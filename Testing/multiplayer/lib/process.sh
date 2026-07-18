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
# Stages a Unity build into an isolated per-run directory. Unity resolves Application.dataPath
# (and therefore the Serilog Logs/ folder) from the executable's own location, not CWD — without
# a per-run tree, concurrent runs sharing one build would collide on LogServer.json /
# LogClient<ckey>.json. Config/ and Data/ are CWD-relative (see Paths.cs), so the process
# workdir is this staged dir too.
#
# Prefer hardlinks (cp -a --link / cp -al) so each run does not duplicate ~180–300 MB of
# player binaries on disk. Writable overlays (Config, Data, Logs) are replaced with real
# copies so runtime writes cannot mutate Builds/ or sibling runs sharing hardlinked files.
# Falls back to a full copy with a warning when hardlinks are unavailable (cross-filesystem).
stage_build() {
    local source_dir="$1"
    local dest_dir="$2"

    if [[ ! -d "$source_dir" ]]; then
        echo "error: build directory not found: $source_dir" >&2
        return 1
    fi

    mkdir -p "$(dirname "$dest_dir")"
    rm -rf "$dest_dir"

    if cp -a --link "$source_dir" "$dest_dir" 2>/dev/null \
        || cp -al "$source_dir" "$dest_dir" 2>/dev/null; then
        :
    else
        echo "warning: hardlink staging unavailable for $dest_dir; falling back to full copy (high disk use)" >&2
        cp -a "$source_dir" "$dest_dir" || return 1
    fi

    # Real copies of small writable trees (CWD Config/Data; Logs next to the binary).
    rm -rf "${dest_dir:?}/Config" "${dest_dir:?}/Data" "${dest_dir:?}/Logs"
    if [[ -d "$source_dir/Config" ]]; then
        cp -a "$source_dir/Config" "$dest_dir/Config"
    else
        mkdir -p "$dest_dir/Config"
    fi
    if [[ -d "$source_dir/Data" ]]; then
        cp -a "$source_dir/Data" "$dest_dir/Data"
    else
        mkdir -p "$dest_dir/Data"
    fi
    mkdir -p "$dest_dir/Logs"
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
