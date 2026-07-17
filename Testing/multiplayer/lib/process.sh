#!/usr/bin/env bash
# Process orchestration helpers for the multiplayer smoke test harness.
# Sourced by run_smoketest.sh - not meant to be executed directly.

SS3D_TRACKED_PIDS=()

# Prints a free UDP port on 127.0.0.1. Avoids the hardcoded port the old PlayMode harness used,
# which broke under concurrent/parallel runs.
alloc_port() {
    python3 - <<'PY'
import socket
s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
s.bind(("127.0.0.1", 0))
print(s.getsockname()[1])
s.close()
PY
}

# stage_build <source_build_dir> <dest_dir>
# Copies a Unity build output directory into an isolated per-run directory. Unity resolves
# Application.dataPath (and therefore the Serilog Logs/ folder) from the executable's own
# location, not the process's working directory - without this, concurrent runs sharing one
# build would all write LogServer.json/LogClient<ckey>.json to the same shared Logs/ folder.
# Also clears any Logs/ carried over from the source build (e.g. earlier manual dogfooding
# runs) so a stale "ServerReady" line can't be mistaken for this run's own signal.
stage_build() {
    local source_dir="$1"
    local dest_dir="$2"

    if [[ ! -d "$source_dir" ]]; then
        echo "error: build directory not found: $source_dir" >&2
        return 1
    fi

    mkdir -p "$(dirname "$dest_dir")"
    cp -r "$source_dir" "$dest_dir"
    rm -rf "${dest_dir:?}/Logs"
}

# spawn_process <workdir> <executable> <logfile> <args...>
# Launches a process with its working directory set to <workdir> (Config/permissions.txt is
# resolved relative to CWD, see Assets/Scripts/SS3D/Data/Paths.cs), Unity's own -logFile
# pointed at <logfile> for crash/exception detection, and tracks the PID in
# SS3D_TRACKED_PIDS for cleanup - never killed by name match, only by the exact PID this run
# started.
spawn_process() {
    local workdir="$1"
    local executable="$2"
    local logfile="$3"
    shift 3

    mkdir -p "$(dirname "$logfile")"

    (
        cd "$workdir" || exit 1
        exec "$executable" "$@" -logFile "$logfile"
    ) &

    local pid=$!
    SS3D_TRACKED_PIDS+=("$pid")
}

last_tracked_pid() {
    echo "${SS3D_TRACKED_PIDS[-1]}"
}

kill_tracked_pids() {
    for pid in "${SS3D_TRACKED_PIDS[@]:-}"; do
        if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
            kill "$pid" 2>/dev/null
        fi
    done
}

# wait_for_pid_exit <pid> <timeout_seconds>
wait_for_pid_exit() {
    local pid="$1"
    local timeout="$2"
    local waited=0

    while kill -0 "$pid" 2>/dev/null; do
        if (( waited >= timeout )); then
            return 1
        fi
        sleep 1
        waited=$((waited + 1))
    done

    return 0
}
