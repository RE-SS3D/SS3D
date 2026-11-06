#!/usr/bin/env bash
# Multiplayer test harness entry point.
#
# Launches a real headless dedicated-server process and N real headless client processes (all
# -batchmode -nographics, no display dependency), drives each through a scripted scenario via
# the in-game AutomationSubSystem (-testscript=), and fails on any uncaught exception,
# known-bad unity.log pattern (tools/known_unity_bad.patterns), or Error/Fatal log entry
# in any process's logs - replacing the old brittle PlayMode harness
# (Thread.Sleep readiness, Windows-only window tiling, hardcoded port, global process-name
# kill). See Documents/architecture/systems/networking-session-testing.md.
#
# Usage: ./Testing/multiplayer/run_smoketest.sh <scenario-name> [client-count]
#
# For client index N, looks for scenarios/<scenario-name>-client-N.txt first, falling back to
# scenarios/<scenario-name>-client.txt (used by every client index that has no numbered
# variant) - see scenarios/late-join*.txt for an example of per-client scripts.
#
# Requires SS3D built to:
#   Builds/GameServer/SS3D.x86_64  (dedicated server - "SS3D/Build/Dedicated Server (Linux)")
#   Builds/Game/SS3D.x86_64        (client            - "SS3D/Build/Client (Linux)")
# or override the directories/filenames with SS3D_SERVER_BUILD_DIR / SS3D_CLIENT_BUILD_DIR /
# SS3D_SERVER_BIN_NAME / SS3D_CLIENT_BIN_NAME (CI builds both into one directory with
# buildName-derived filenames - see .github/workflows/multiplayer-smoke-test.yml).

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# shellcheck source=lib/process.sh
source "$SCRIPT_DIR/lib/process.sh"
# shellcheck source=lib/logwait.sh
source "$SCRIPT_DIR/lib/logwait.sh"

SCENARIO="${1:?Usage: run_smoketest.sh <scenario-name> [client-count]}"
CLIENT_COUNT="${2:-1}"

SERVER_BUILD_DIR="${SS3D_SERVER_BUILD_DIR:-$REPO_ROOT/Builds/GameServer}"
CLIENT_BUILD_DIR="${SS3D_CLIENT_BUILD_DIR:-$REPO_ROOT/Builds/Game}"
SERVER_BIN_NAME="${SS3D_SERVER_BIN_NAME:-SS3D.x86_64}"
CLIENT_BIN_NAME="${SS3D_CLIENT_BIN_NAME:-SS3D.x86_64}"

RUN_ID="$(date +%s)-$$"
RUN_DIR="$REPO_ROOT/Testing/multiplayer/.runs/$RUN_ID"

SERVER_SCRIPT="$SCRIPT_DIR/scenarios/${SCENARIO}.txt"
# Optional per-scenario settings, sourced from scenarios/<scenario>.conf: SERVER_EXTRA_ARGS (array of
# extra server arguments, e.g. -sandbox), CLIENT_TIMEOUT (seconds each client gets to finish) and
# CLIENT_NO_TOKEN=1 (clients start without their -token, so an admin name is refused until set_token).
SERVER_EXTRA_ARGS=()
CLIENT_TIMEOUT=90
CLIENT_NO_TOKEN=0
if [[ -f "$SCRIPT_DIR/scenarios/${SCENARIO}.conf" ]]; then
    # shellcheck source=/dev/null
    source "$SCRIPT_DIR/scenarios/${SCENARIO}.conf"
fi

trap kill_tracked_pids EXIT INT TERM

echo "== Multiplayer smoke test: $SCENARIO, $CLIENT_COUNT client(s) (run $RUN_ID) =="

if [[ ! -f "$SERVER_BUILD_DIR/$SERVER_BIN_NAME" ]]; then
    echo "error: no server build at $SERVER_BUILD_DIR/$SERVER_BIN_NAME - build it first (SS3D/Build/Dedicated Server (Linux))." >&2
    exit 1
fi

if [[ ! -f "$CLIENT_BUILD_DIR/$CLIENT_BIN_NAME" ]]; then
    echo "error: no client build at $CLIENT_BUILD_DIR/$CLIENT_BIN_NAME - build it first (SS3D/Build/Client (Linux))." >&2
    exit 1
fi

if [[ ! -f "$SERVER_SCRIPT" ]]; then
    echo "error: no server scenario script at $SERVER_SCRIPT" >&2
    exit 1
fi

if ! command -v jq >/dev/null 2>&1; then
    echo "error: jq is required (structured JSON log parsing) but not found on PATH." >&2
    exit 1
fi

# Previous runs may leave players alive (Unity often ignores SIGTERM after ScriptComplete).
# Kill only the exact binaries this run is about to launch — not arbitrary SS3D processes.
for orphan_bin in "$SERVER_BUILD_DIR/$SERVER_BIN_NAME" "$CLIENT_BUILD_DIR/$CLIENT_BIN_NAME"; do
    while read -r orphan_pid; do
        [[ -n "$orphan_pid" ]] || continue
        echo "warning: killing leftover PID $orphan_pid using $orphan_bin" >&2
        kill -KILL "$orphan_pid" 2>/dev/null || true
    done < <(pgrep -f "^${orphan_bin}( |$)" 2>/dev/null || true)
done

# Resolve each client's scenario script and ckey up front so a missing script fails fast,
# before any process is started.
CLIENT_SCRIPTS=()
CLIENT_CKEYS=()
for ((i = 0; i < CLIENT_COUNT; i++)); do
    numbered_script="$SCRIPT_DIR/scenarios/${SCENARIO}-client-${i}.txt"
    default_script="$SCRIPT_DIR/scenarios/${SCENARIO}-client.txt"

    if [[ -f "$numbered_script" ]]; then
        CLIENT_SCRIPTS+=("$numbered_script")
    elif [[ -f "$default_script" ]]; then
        CLIENT_SCRIPTS+=("$default_script")
    else
        echo "error: no client scenario script at $numbered_script or $default_script" >&2
        exit 1
    fi

    CLIENT_CKEYS+=("harness_${RUN_ID}_${i}")
done

mkdir -p "$RUN_DIR"

echo "Staging isolated build trees (hardlink when possible)..."
stage_build "$SERVER_BUILD_DIR" "$RUN_DIR/server" || exit 1
chmod +x "$RUN_DIR/server/$SERVER_BIN_NAME"

# Ensure CWD Data/Tilemaps exists for this run. develop-release seeds the same fixtures into
# published zips; local/CI player trees and GameServer still often lack them. Without maps,
# TileSubSystem.Load() logs "No station templates found" and observer-sync cannot pass.
TILEMAP_FIXTURES="${SS3D_TILEMAP_FIXTURES:-$REPO_ROOT/Builds/Game/Data/Tilemaps}"
if [[ -d "$TILEMAP_FIXTURES" ]]; then
    mkdir -p "$RUN_DIR/server/Data/Tilemaps"
    cp -a "$TILEMAP_FIXTURES"/. "$RUN_DIR/server/Data/Tilemaps/"
else
    echo "warning: no tilemap fixtures at $TILEMAP_FIXTURES — scenarios that need a station map may fail" >&2
fi

# Round start loads Data/StationTemplates/<map> (default classic-ministation) and refuses a map that fails
# Map Check (missing job/antag spawns). Always overwrite with the committed templates: a GameServer
# tree can carry a stale copy from before the spawn markers were authored, and the round then
# drops from Preparing straight back to Stopped.
STATION_FIXTURES="${SS3D_STATION_FIXTURES:-$REPO_ROOT/Builds/Game/Data/StationTemplates}"
if [[ -d "$STATION_FIXTURES" ]]; then
    mkdir -p "$RUN_DIR/server/Data/StationTemplates"
    cp -a "$STATION_FIXTURES"/. "$RUN_DIR/server/Data/StationTemplates/"
else
    echo "warning: no station template fixtures at $STATION_FIXTURES — round start will fail" >&2
fi

# Every client's ckey needs Administrator to be allowed to start the round (see
# ChangeRoundStateView.HandleEmbarkButtonPress / PermissionSubSystem) - seeded here since a
# real headless dedicated server has no Editor session to grant it by hand. Harmless for
# clients whose script never calls start_round.
#
# Builds often ship Data/ServerMeta/permissions.json from prior Editor/play sessions.
# PermissionsPersistenceContributor prefers that envelope over Config/permissions.txt
# (see Documents/architecture/systems/permissions.md Pitfalls), so clear it from the
# staged tree first — Data is a real copy, not a hardlink to Builds/.
rm -f "$RUN_DIR/server/Data/ServerMeta/permissions.json" \
    "$RUN_DIR/server/Data/ServerMeta/permissions"
mkdir -p "$RUN_DIR/server/Config"
: > "$RUN_DIR/server/Config/permissions.txt"
# Dedicated servers only admit admin names with a matching -token (AdminTokens).
: > "$RUN_DIR/server/Config/admin-tokens.txt"
for ckey in "${CLIENT_CKEYS[@]}"; do
    echo "${ckey} Administrator" >> "$RUN_DIR/server/Config/permissions.txt"
    echo "${ckey} smoke-${ckey}" >> "$RUN_DIR/server/Config/admin-tokens.txt"
done

PORT="$(alloc_port)"
echo "Allocated port $PORT"

SERVER_JSON_LOG="$RUN_DIR/server/Logs/LogServer.json"
SERVER_UNITY_LOG="$RUN_DIR/server/unity.log"

echo "Starting dedicated server on port $PORT..."
spawn_process "$RUN_DIR/server" "$RUN_DIR/server/$SERVER_BIN_NAME" "$SERVER_UNITY_LOG" \
    -batchmode -nographics -serveronly -port="$PORT" -skipintro -testscript="$SERVER_SCRIPT" "${SERVER_EXTRA_ARGS[@]}"
SERVER_PID="$(last_tracked_pid)"

if ! wait_for_signal "$SERVER_JSON_LOG" "ServerReady" 60; then
    echo "Server never reported ready."
    dump_logs "server" "$SERVER_UNITY_LOG" "$SERVER_JSON_LOG"
    echo "FAIL: $SCENARIO"
    exit 1
fi

CLIENT_PIDS=()
CLIENT_JSON_LOGS=()
CLIENT_UNITY_LOGS=()

for ((i = 0; i < CLIENT_COUNT; i++)); do
    ckey="${CLIENT_CKEYS[$i]}"
    workdir="$RUN_DIR/client-$i"

    stage_build "$CLIENT_BUILD_DIR" "$workdir" || exit 1
    chmod +x "$workdir/$CLIENT_BIN_NAME"

    unity_log="$workdir/unity.log"
    json_log="$workdir/Logs/LogClient${ckey}.json"

    token_args=(-token="smoke-${ckey}")
    [[ "$CLIENT_NO_TOKEN" -eq 1 ]] && token_args=()

    echo "Starting client $i (ckey=$ckey)..."
    spawn_process "$workdir" "$workdir/$CLIENT_BIN_NAME" "$unity_log" \
        -batchmode -nographics -ip=127.0.0.1 -port="$PORT" -ckey="$ckey" "${token_args[@]}" -skipintro -testscript="${CLIENT_SCRIPTS[$i]}"

    CLIENT_PIDS+=("$(last_tracked_pid)")
    CLIENT_JSON_LOGS+=("$json_log")
    CLIENT_UNITY_LOGS+=("$unity_log")
done

FAILED=0

for ((i = 0; i < CLIENT_COUNT; i++)); do
    if ! wait_for_signal "${CLIENT_JSON_LOGS[$i]}" "ScriptComplete" "$CLIENT_TIMEOUT"; then
        echo "Client $i script did not complete."
        FAILED=1
    fi
done

if [[ "$FAILED" -eq 0 ]] && ! wait_for_signal "$SERVER_JSON_LOG" "ScriptComplete" 60; then
    echo "Server script did not complete."
    FAILED=1
fi

wait_for_pid_exit "$SERVER_PID" 30 || true
for pid in "${CLIENT_PIDS[@]}"; do
    wait_for_pid_exit "$pid" 30 || true
done

# Guarantee no leftover players (Environment.Exit / SIGTERM are not reliable for Unity 6).
kill_tracked_pids

CHECK_LABELS=("server")
CHECK_UNITY_LOGS=("$SERVER_UNITY_LOG")
CHECK_JSON_LOGS=("$SERVER_JSON_LOG")
for ((i = 0; i < CLIENT_COUNT; i++)); do
    CHECK_LABELS+=("client-$i")
    CHECK_UNITY_LOGS+=("${CLIENT_UNITY_LOGS[$i]}")
    CHECK_JSON_LOGS+=("${CLIENT_JSON_LOGS[$i]}")
done

for ((i = 0; i < ${#CHECK_LABELS[@]}; i++)); do
    label="${CHECK_LABELS[$i]}"
    unitylog="${CHECK_UNITY_LOGS[$i]}"
    jsonlog="${CHECK_JSON_LOGS[$i]}"

    if ! check_unity_log_for_exceptions "$unitylog"; then
        echo "error: exception found in $label's Unity log ($unitylog)" >&2
        FAILED=1
    fi

    if ! check_unity_log_for_bad_patterns "$unitylog"; then
        echo "error: known-bad pattern found in $label's Unity log ($unitylog)" >&2
        FAILED=1
    fi

    if ! check_for_json_errors "$jsonlog"; then
        echo "error: Error/Fatal log entry found in $label's structured log ($jsonlog)" >&2
        FAILED=1
    fi

    if ! check_for_error_signal "$jsonlog"; then
        echo "error: $label's script reported ScriptFailed" >&2
        FAILED=1
    fi
done

if [[ "$FAILED" -ne 0 ]]; then
    for ((i = 0; i < ${#CHECK_LABELS[@]}; i++)); do
        dump_logs "${CHECK_LABELS[$i]}" "${CHECK_UNITY_LOGS[$i]}" "${CHECK_JSON_LOGS[$i]}"
    done
    echo "FAIL: $SCENARIO"
    exit 1
fi

echo "PASS: $SCENARIO"
echo "Logs preserved at: $RUN_DIR"
exit 0
