#!/usr/bin/env bash
# Multiplayer load/soak harness: one real dedicated server + N real headless clients,
# holding a round for a configurable duration while sampling server RSS/CPU/threads.
#
# Usage: ./run_loadtest.sh [--clients N] [--duration SECONDS]
#
# Defaults: 8 clients, 300 seconds. Run this when the host is otherwise idle; eight Unity
# clients plus the server are deliberately heavy.
#
# --stagger SECONDS delays each client launch. 0 starts all clients at once (thundering-herd
# stress; currently exposes a connect-time NRE). 2-3 gives a stable baseline.
#
# Build dirs / binary names can be overridden like the smoke harness:
#   SS3D_SERVER_BUILD_DIR, SS3D_CLIENT_BUILD_DIR,
#   SS3D_SERVER_BIN_NAME, SS3D_CLIENT_BIN_NAME.
set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

# shellcheck source=lib/process.sh
source "$SCRIPT_DIR/lib/process.sh"
# shellcheck source=lib/logwait.sh
source "$SCRIPT_DIR/lib/logwait.sh"

CLIENTS=8
DURATION=300
STAGGER=0
SCENARIO="loadtest"

while (($#)); do
    case "$1" in
        --clients) CLIENTS="${2:?--clients needs a value}"; shift 2 ;;
        --duration) DURATION="${2:?--duration needs a value}"; shift 2 ;;
        --stagger) STAGGER="${2:?--stagger needs a value}"; shift 2 ;;
        --scenario) SCENARIO="${2:?--scenario needs a value}"; shift 2 ;;
        -h|--help)
            sed -n '2,16p' "$0"; exit 0 ;;
        *) echo "usage: $0 [--clients N] [--duration SECONDS] [--stagger SECONDS] [--scenario NAME]" >&2; exit 2 ;;
    esac
done

[[ "$CLIENTS" =~ ^[1-9][0-9]*$ ]] || { echo "error: --clients must be a positive integer" >&2; exit 2; }
[[ "$DURATION" =~ ^[1-9][0-9]*$ ]] || { echo "error: --duration must be a positive integer" >&2; exit 2; }
[[ "$STAGGER" =~ ^[0-9]+$ ]] || { echo "error: --stagger must be a non-negative integer" >&2; exit 2; }
command -v jq >/dev/null || { echo "error: jq is required" >&2; exit 1; }

SERVER_BUILD_DIR="${SS3D_SERVER_BUILD_DIR:-$REPO_ROOT/Builds/GameServer}"
CLIENT_BUILD_DIR="${SS3D_CLIENT_BUILD_DIR:-$REPO_ROOT/Builds/Game}"
SERVER_BIN_NAME="${SS3D_SERVER_BIN_NAME:-SS3D.x86_64}"
CLIENT_BIN_NAME="${SS3D_CLIENT_BIN_NAME:-SS3D.x86_64}"

SERVER_SRC="$SCRIPT_DIR/scenarios/${SCENARIO}-server.txt"
CLIENT0_SRC="$SCRIPT_DIR/scenarios/${SCENARIO}-client-0.txt"
CLIENTN_SRC="$SCRIPT_DIR/scenarios/${SCENARIO}-client.txt"
for f in "$SERVER_SRC" "$CLIENT0_SRC" "$CLIENTN_SRC"; do
    [[ -f "$f" ]] || { echo "error: missing scenario file $f" >&2; exit 1; }
done
[[ -x "$SERVER_BUILD_DIR/$SERVER_BIN_NAME" ]] || {
    echo "error: no server build at $SERVER_BUILD_DIR/$SERVER_BIN_NAME" >&2; exit 1; }
[[ -x "$CLIENT_BUILD_DIR/$CLIENT_BIN_NAME" ]] || {
    echo "error: no client build at $CLIENT_BUILD_DIR/$CLIENT_BIN_NAME" >&2; exit 1; }

RUN_ID="$(date +%s)-$$"
RUN_DIR="$REPO_ROOT/Testing/multiplayer/.runs/load-$RUN_ID"
mkdir -p "$RUN_DIR"

PORT="$(alloc_port)"
echo "== Load test: $CLIENTS client(s), ${DURATION}s, stagger=${STAGGER}s (run $RUN_ID, port $PORT) =="
echo "run dir: $RUN_DIR"

SERVER_UNITY_LOG="$RUN_DIR/server/unity.log"
SERVER_JSON_LOG="$RUN_DIR/server/Logs/LogServer.json"
METRICS="$RUN_DIR/metrics.csv"
METRICS_PID=""
CLIENT_PIDS=()
CLIENT_JSON_LOGS=()
CLIENT_UNITY_LOGS=()
CLIENT_CKEYS=()

cleanup() {
    if [[ -n "$METRICS_PID" ]]; then
        kill "$METRICS_PID" 2>/dev/null || true
        wait "$METRICS_PID" 2>/dev/null || true
        METRICS_PID=""
    fi
    kill_tracked_pids
}
trap cleanup EXIT INT TERM

fail() {
    echo "FAIL: loadtest: $*" >&2
    for f in "$SERVER_UNITY_LOG" "$SERVER_JSON_LOG"; do
        [[ -f "$f" ]] && { echo "--- $f"; tail -n 30 "$f"; }
    done
    for i in "${!CLIENT_UNITY_LOGS[@]}"; do
        for f in "${CLIENT_UNITY_LOGS[$i]}" "${CLIENT_JSON_LOGS[$i]}"; do
            [[ -f "$f" ]] && { echo "--- $f"; tail -n 20 "$f"; }
        done
    done
    echo "logs preserved at: $RUN_DIR" >&2
    exit 1
}

stage_build "$SERVER_BUILD_DIR" "$RUN_DIR/server" || fail "could not stage server build"
chmod +x "$RUN_DIR/server/$SERVER_BIN_NAME" || fail "server binary is not executable"

TILEMAP_FIXTURES="${SS3D_TILEMAP_FIXTURES:-$REPO_ROOT/Builds/Game/Data/Tilemaps}"
STATION_FIXTURES="${SS3D_STATION_FIXTURES:-$REPO_ROOT/Builds/Game/Data/StationTemplates}"
if [[ -d "$TILEMAP_FIXTURES" ]]; then
    mkdir -p "$RUN_DIR/server/Data/Tilemaps"
    cp -a "$TILEMAP_FIXTURES"/. "$RUN_DIR/server/Data/Tilemaps/"
fi
if [[ -d "$STATION_FIXTURES" ]]; then
    mkdir -p "$RUN_DIR/server/Data/StationTemplates"
    cp -a "$STATION_FIXTURES"/. "$RUN_DIR/server/Data/StationTemplates/"
fi

rm -f "$RUN_DIR/server/Data/ServerMeta/permissions.json" \
    "$RUN_DIR/server/Data/ServerMeta/permissions"
mkdir -p "$RUN_DIR/server/Config"
: > "$RUN_DIR/server/Config/permissions.txt"
# Dedicated servers only admit admin names with a matching -token (AdminTokens).
: > "$RUN_DIR/server/Config/admin-tokens.txt"
for ((i = 0; i < CLIENTS; i++)); do
    ckey="load_${RUN_ID}_${i}"
    CLIENT_CKEYS+=("$ckey")
    echo "$ckey Administrator" >> "$RUN_DIR/server/Config/permissions.txt"
    echo "$ckey token-$ckey" >> "$RUN_DIR/server/Config/admin-tokens.txt"
done

sed "s/__DURATION__/$((DURATION + 60))/g" "$SERVER_SRC" > "$RUN_DIR/server-script.txt"
sed "s/__DURATION__/${DURATION}/g" "$CLIENT0_SRC" > "$RUN_DIR/client-0-script.txt"
sed "s/__DURATION__/${DURATION}/g" "$CLIENTN_SRC" > "$RUN_DIR/client-script.txt"

echo "Starting dedicated server..."
spawn_process "$RUN_DIR/server" "$RUN_DIR/server/$SERVER_BIN_NAME" "$SERVER_UNITY_LOG" \
    -batchmode -nographics -serveronly -port="$PORT" -skipintro \
    -testscript="$RUN_DIR/server-script.txt"
SERVER_PID="$(last_tracked_pid)"

wait_for_signal "$SERVER_JSON_LOG" ServerReady 90 || fail "server did not report ready"

echo "Starting $CLIENTS client(s)..."
for ((i = 0; i < CLIENTS; i++)); do
    ckey="${CLIENT_CKEYS[$i]}"
    workdir="$RUN_DIR/client-$i"
    stage_build "$CLIENT_BUILD_DIR" "$workdir" || fail "could not stage client $i"
    chmod +x "$workdir/$CLIENT_BIN_NAME" || fail "client $i binary is not executable"

    unity_log="$workdir/unity.log"
    json_log="$workdir/Logs/LogClient${ckey}.json"
    script="$RUN_DIR/client-script.txt"
    ((i == 0)) && script="$RUN_DIR/client-0-script.txt"

    CLIENT_UNITY_LOGS+=("$unity_log")
    CLIENT_JSON_LOGS+=("$json_log")
    spawn_process "$workdir" "$workdir/$CLIENT_BIN_NAME" "$unity_log" \
        -batchmode -nographics -ip=127.0.0.1 -port="$PORT" -ckey="$ckey" \
        -token="token-$ckey" -skipintro -testscript="$script"
    CLIENT_PIDS+=("$(last_tracked_pid)")
    if ((STAGGER > 0 && i < CLIENTS - 1)); then
        sleep "$STAGGER"
    fi
done

echo "Waiting for clients to connect..."
for ((i = 0; i < CLIENTS; i++)); do
    wait_for_signal "${CLIENT_JSON_LOGS[$i]}" ClientConnected 120 || fail "client $i did not connect"
done

echo "Waiting for clients to embark..."
for ((i = 0; i < CLIENTS; i++)); do
    wait_for_signal "${CLIENT_JSON_LOGS[$i]}" EmbarkVerified 240 || fail "client $i did not embark"
done

echo "Load reached: $CLIENTS clients embarked; soaking for ${DURATION}s"

printf 'utc,elapsed_s,rss_kb,cpu_pct,threads,embarked,lo_rx_bytes,lo_tx_bytes,lo_rx_mbps,lo_tx_mbps\n' > "$METRICS"
(
    started="$(date +%s)"
    prev_rx=""
    prev_tx=""
    prev_net_time=""
    while true; do
        now="$(date +%s)"
        elapsed=$((now - started))
        read -r rss pcpu threads <<< "$(ps -o rss=,pcpu=,nlwp= -p "$SERVER_PID" 2>/dev/null | awk '{$1=$1; print}')"
        [[ -n "${rss:-}" && -n "${pcpu:-}" && -n "${threads:-}" ]] || break

        read -r lo_rx lo_tx <<< "$(awk '/^ *lo:/ {gsub(/^ *lo: */, \"\"); print $1, $9}' /proc/net/dev 2>/dev/null)"
        lo_rx="${lo_rx:-0}"
        lo_tx="${lo_tx:-0}"
        rx_mbps=0
        tx_mbps=0
        if [[ -n "$prev_rx" && -n "$prev_net_time" ]]; then
            dt=$((now - prev_net_time))
            ((dt < 1)) && dt=1
            rx_mbps="$(awk -v d=$((lo_rx - prev_rx)) -v t="$dt" 'BEGIN { printf "%.3f", (d * 8) / t / 1000000 }')"
            tx_mbps="$(awk -v d=$((lo_tx - prev_tx)) -v t="$dt" 'BEGIN { printf "%.3f", (d * 8) / t / 1000000 }')"
        fi
        prev_rx="$lo_rx"
        prev_tx="$lo_tx"
        prev_net_time="$now"

        embarked=0
        for log in "${CLIENT_JSON_LOGS[@]}"; do
            if [[ -f "$log" ]] && jq -e 'select(.signal == "EmbarkVerified")' "$log" >/dev/null 2>&1; then
                embarked=$((embarked + 1))
            fi
        done
        printf '%s,%s,%s,%s,%s,%s,%s,%s,%s,%s\n' \
            "$(date -u +%FT%TZ)" "$elapsed" "$rss" "$pcpu" "$threads" "$embarked" \
            "$lo_rx" "$lo_tx" "$rx_mbps" "$tx_mbps" >> "$METRICS"
        sleep 10
    done
) &
METRICS_PID=$!

for ((i = 0; i < CLIENTS; i++)); do
    wait_for_signal "${CLIENT_JSON_LOGS[$i]}" ScriptComplete $((DURATION + 180)) || fail "client $i did not complete"
done

wait_for_signal "$SERVER_JSON_LOG" ScriptComplete 120 || true

if [[ -n "$METRICS_PID" ]]; then
    kill "$METRICS_PID" 2>/dev/null || true
    wait "$METRICS_PID" 2>/dev/null || true
    METRICS_PID=""
fi

wait_for_pid_exit "$SERVER_PID" 30 || true
for pid in "${CLIENT_PIDS[@]}"; do
    wait_for_pid_exit "$pid" 30 || true
done
kill_tracked_pids

FAILED=0
LABELS=("server")
UNITY_LOGS=("$SERVER_UNITY_LOG")
JSON_LOGS=("$SERVER_JSON_LOG")
for ((i = 0; i < CLIENTS; i++)); do
    LABELS+=("client-$i")
    UNITY_LOGS+=("${CLIENT_UNITY_LOGS[$i]}")
    JSON_LOGS+=("${CLIENT_JSON_LOGS[$i]}")
done

for ((i = 0; i < ${#LABELS[@]}; i++)); do
    if ! check_unity_log_for_exceptions "${UNITY_LOGS[$i]}"; then
        echo "error: exception in ${LABELS[$i]} unity.log" >&2
        FAILED=1
    fi
    if ! check_unity_log_for_bad_patterns "${UNITY_LOGS[$i]}"; then
        echo "error: known-bad pattern in ${LABELS[$i]} unity.log" >&2
        FAILED=1
    fi
    if ! check_for_json_errors "${JSON_LOGS[$i]}"; then
        echo "error: Error/Fatal in ${LABELS[$i]} structured log" >&2
        FAILED=1
    fi
    if ! check_for_error_signal "${JSON_LOGS[$i]}"; then
        echo "error: ScriptFailed in ${LABELS[$i]} structured log" >&2
        FAILED=1
    fi
done

echo "=== server metrics ==="
awk -F, 'NR > 1 {
    rss += $3; if ($3 > maxrss) maxrss = $3;
    cpu += $4; if ($4 > maxcpu) maxcpu = $4;
    if ($6 > maxembarked) maxembarked = $6;
    rx_mbps += $9; if ($9 > max_rx_mbps) max_rx_mbps = $9;
    tx_mbps += $10; if ($10 > max_tx_mbps) max_tx_mbps = $10;
    rx_bytes = $7; tx_bytes = $8;
    n++;
} END {
    if (n == 0) { print "no samples"; exit }
    printf "samples=%d avg_rss=%.1f MiB max_rss=%.1f MiB avg_cpu=%.1f%% max_cpu=%.1f%% max_embarked=%d\n",
        n, rss / n / 1024, maxrss / 1024, cpu / n, maxcpu, maxembarked;
    printf "loopback avg_rx=%.3f Mbps max_rx=%.3f Mbps avg_tx=%.3f Mbps max_tx=%.3f Mbps total_rx=%.1f MiB total_tx=%.1f MiB\n",
        rx_mbps / n, max_rx_mbps, tx_mbps / n, max_tx_mbps, rx_bytes / 1048576, tx_bytes / 1048576;
}' "$METRICS"

if ((FAILED)); then
    echo "FAIL: loadtest (see $RUN_DIR)" >&2
    exit 1
fi

echo "PASS: loadtest clients=$CLIENTS duration=${DURATION}s"
echo "Logs preserved at: $RUN_DIR"
