#!/usr/bin/env bash
# Structured-log tailing and assertion helpers for the multiplayer smoke test harness.
# Sourced by run_smoketest.sh - not meant to be executed directly.
#
# Requires jq. Reads the compact-JSON Serilog file sink (Assets/Settings/LogSettings.asset ->
# UseCompactJsonFormatter) rather than scraping human-readable log prose.

# wait_for_signal <json_log_path> <signal_name> <timeout_seconds>
# Polls for a "Test signal <signal_name>" line (see SS3D.Systems.Testing.TestSignal) instead of
# a fixed Thread.Sleep or a scene GameObject.Find poll loop.
wait_for_signal() {
    local log_path="$1"
    local signal="$2"
    local timeout="$3"
    local waited=0

    while true; do
        if [[ -f "$log_path" ]] && jq -e --arg signal "$signal" 'select(.signal == $signal)' "$log_path" >/dev/null 2>&1; then
            return 0
        fi

        if (( waited >= timeout )); then
            echo "error: timed out after ${timeout}s waiting for signal '$signal' in $log_path" >&2
            return 1
        fi

        sleep 1
        waited=$((waited + 1))
    done
}

# check_for_error_signal <json_log_path>
# ScriptFailed:<reason> is emitted by AutomationSubSystem when a scripted instruction throws -
# treat it as a hard failure even if no exception separately reached Unity's own log.
check_for_error_signal() {
    local log_path="$1"

    if [[ ! -f "$log_path" ]]; then
        return 0
    fi

    if jq -e 'select(.signal == "ScriptFailed")' "$log_path" >/dev/null 2>&1; then
        return 1
    fi

    return 0
}

# check_for_json_errors <json_log_path>
# CompactJsonFormatter omits "@l" entirely at the default Information level, so its presence at
# all (Error/Fatal) is the signal - see Assets/Scripts/SS3D/Logging/LogManager.cs.
check_for_json_errors() {
    local log_path="$1"

    if [[ ! -f "$log_path" ]]; then
        return 0
    fi

    if jq -e 'select(.["@l"] == "Error" or .["@l"] == "Fatal")' "$log_path" >/dev/null 2>&1; then
        return 1
    fi

    return 0
}

# check_unity_log_for_exceptions <unity_logfile_path>
# Uncaught exceptions/NREs surface through Unity's own -logFile, not the Serilog pipeline - see
# Testing/multiplayer notes in Documents/architecture/2026-07_multiplayer-test-harness.md.
check_unity_log_for_exceptions() {
    local log_path="$1"

    if [[ ! -f "$log_path" ]]; then
        return 0
    fi

    if grep -qE "NullReferenceException|Unhandled Exception|Exception:|Assertion failed|Fatal error" "$log_path"; then
        return 1
    fi

    return 0
}

dump_logs() {
    local label="$1"
    shift

    echo "===== $label ====="
    for f in "$@"; do
        if [[ -f "$f" ]]; then
            echo "--- $f ---"
            cat "$f"
        fi
    done
}
