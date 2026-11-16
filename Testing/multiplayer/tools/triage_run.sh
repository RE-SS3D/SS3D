#!/usr/bin/env bash
# Summarize a multiplayer smoke-test run without dumping full unity.log files.
#
# Usage:
#   ./Testing/multiplayer/tools/triage_run.sh <run-id-or-path>
#   ./Testing/multiplayer/tools/triage_run.sh latest
#
# Prints: process inventory + sizes, Test signal timeline, ScriptFailed payloads,
# JSON Error/Fatal lines, unity.log denylist hits (known_unity_bad.patterns — harness
# hard-fail), and unity.log exception hits classified as real vs known noise
# (known_unity_noise.patterns). Exit 0 always when the run dir exists — this is a
# reporter, not a pass/fail gate (run_smoketest.sh owns that).

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../.." && pwd)"
RUNS_ROOT="$REPO_ROOT/Testing/multiplayer/.runs"
NOISE_FILE="$SCRIPT_DIR/known_unity_noise.patterns"
BAD_FILE="$SCRIPT_DIR/known_unity_bad.patterns"

TARGET="${1:?Usage: triage_run.sh <run-id|path|latest>}"

if ! command -v jq >/dev/null 2>&1; then
    echo "error: jq is required" >&2
    exit 1
fi

resolve_run_dir() {
    local t="$1"
    if [[ -d "$t" ]]; then
        printf '%s\n' "$(cd "$t" && pwd)"
        return 0
    fi
    if [[ -d "$RUNS_ROOT/$t" ]]; then
        printf '%s\n' "$RUNS_ROOT/$t"
        return 0
    fi
    if [[ "$t" == "latest" ]]; then
        local latest
        latest="$(ls -1dt "$RUNS_ROOT"/*/ 2>/dev/null | head -1 || true)"
        if [[ -z "$latest" ]]; then
            echo "error: no runs under $RUNS_ROOT" >&2
            return 1
        fi
        printf '%s\n' "$(cd "$latest" && pwd)"
        return 0
    fi
    echo "error: run not found: $t (looked under $RUNS_ROOT)" >&2
    return 1
}

load_noise_patterns() {
    NOISE_PATTERNS=()
    if [[ ! -f "$NOISE_FILE" ]]; then
        return 0
    fi
    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
        NOISE_PATTERNS+=("$line")
    done < "$NOISE_FILE"
}

load_bad_patterns() {
    BAD_PATTERNS=()
    if [[ ! -f "$BAD_FILE" ]]; then
        return 0
    fi
    while IFS= read -r line || [[ -n "$line" ]]; do
        [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
        BAD_PATTERNS+=("$line")
    done < "$BAD_FILE"
}

is_known_noise() {
    local line="$1"
    local pat
    for pat in "${NOISE_PATTERNS[@]}"; do
        if echo "$line" | grep -qE -- "$pat"; then
            return 0
        fi
    done
    return 1
}

# Match the same signatures as lib/logwait.sh check_unity_log_for_exceptions.
EXCEPTION_RE='NullReferenceException|Unhandled Exception|Exception:|Assertion failed|Fatal error'

human_bytes() {
    local n="$1"
    if (( n >= 1048576 )); then
        awk -v n="$n" 'BEGIN { printf "%.1fM", n/1048576 }'
    elif (( n >= 1024 )); then
        awk -v n="$n" 'BEGIN { printf "%.0fK", n/1024 }'
    else
        printf '%sB' "$n"
    fi
}

triage_process() {
    local proc_dir="$1"
    local label
    label="$(basename "$proc_dir")"

    local unity_log="$proc_dir/unity.log"
    local json_glob=()
    # Prefer Serilog file sinks; fall back to any .json under Logs/
    if [[ -d "$proc_dir/Logs" ]]; then
        while IFS= read -r -d '' f; do
            json_glob+=("$f")
        done < <(find "$proc_dir/Logs" -maxdepth 1 -type f \( -name 'Log*.json' -o -name '*.json' \) -print0 2>/dev/null)
    fi

    echo
    echo "── $label ──"
    echo "dir: $proc_dir"

    local unity_bytes=0 json_bytes=0 unity_lines=0
    if [[ -f "$unity_log" ]]; then
        unity_bytes=$(wc -c < "$unity_log")
        unity_lines=$(wc -l < "$unity_log")
        echo "unity.log: $(human_bytes "$unity_bytes") ($unity_lines lines)"
    else
        echo "unity.log: (missing)"
    fi

    if ((${#json_glob[@]} == 0)); then
        echo "json: (missing — look for LogServer.json / LogClient*.json under Logs/)"
    else
        for jf in "${json_glob[@]}"; do
            json_bytes=$(wc -c < "$jf")
            echo "json: $(basename "$jf") $(human_bytes "$json_bytes") ($(wc -l < "$jf") lines)"
        done
    fi

    # Signal timeline (order preserved by file order + @t within each file)
    echo "signals:"
    local any_signal=0
    for jf in "${json_glob[@]}"; do
        while IFS= read -r row; do
            any_signal=1
            echo "  $row"
        done < <(jq -r 'select(.signal != null) | "\(.["@t"] // "?")  \(.signal)\(if .payload then "  payload=\(.payload)" else "" end)"' "$jf" 2>/dev/null)
    done
    if (( any_signal == 0 )); then
        echo "  (none)"
    fi

    echo "script_failed:"
    local any_fail=0
    for jf in "${json_glob[@]}"; do
        while IFS= read -r row; do
            any_fail=1
            echo "  $row"
        done < <(jq -r 'select(.signal == "ScriptFailed") | .payload // "(no payload)"' "$jf" 2>/dev/null)
    done
    if (( any_fail == 0 )); then
        echo "  (none)"
    fi

    echo "json_error_fatal:"
    local any_err=0
    for jf in "${json_glob[@]}"; do
        while IFS= read -r row; do
            any_err=1
            echo "  $row"
        done < <(jq -r 'select(.["@l"] == "Error" or .["@l"] == "Fatal") | "\(.["@t"] // "?")  \(.["@l"])  \(.["@mt"] // .message // .)"' "$jf" 2>/dev/null)
    done
    if (( any_err == 0 )); then
        echo "  (none)"
    fi

    echo "unity_bad_patterns:"
    if [[ ! -f "$unity_log" ]]; then
        echo "  (no unity.log)"
    elif ((${#BAD_PATTERNS[@]} == 0)); then
        echo "  (no denylist loaded)"
    else
        local any_bad=0
        local pat count
        for pat in "${BAD_PATTERNS[@]}"; do
            count="$(grep -cE -- "$pat" "$unity_log" 2>/dev/null || true)"
            if [[ "${count:-0}" -gt 0 ]]; then
                any_bad=1
                echo "  ${count}×  $pat  (would fail harness)"
            fi
        done
        if (( any_bad == 0 )); then
            echo "  (none)"
        fi
    fi

    echo "unity_exceptions:"
    if [[ ! -f "$unity_log" ]]; then
        echo "  (no unity.log)"
        return 0
    fi

    local real=()
    local noise_counts=()
    declare -A noise_map=()
    local line matched_noise

    while IFS= read -r line; do
        matched_noise=0
        if is_known_noise "$line"; then
            matched_noise=1
            # Bucket by first matching pattern key = truncated line head
            local key="${line:0:120}"
            noise_map["$key"]=$(( ${noise_map["$key"]:-0} + 1 ))
        fi
        if (( matched_noise == 0 )); then
            real+=("$line")
        fi
    done < <(grep -E "$EXCEPTION_RE" "$unity_log" 2>/dev/null || true)

    if ((${#noise_map[@]} > 0)); then
        echo "  known_noise (counts):"
        for key in "${!noise_map[@]}"; do
            echo "    ${noise_map[$key]}×  $key"
        done | sort -t'×' -k1 -nr
    else
        echo "  known_noise: (none)"
    fi

    if ((${#real[@]} > 0)); then
        echo "  real (would fail harness; showing up to 20):"
        local i=0
        for line in "${real[@]}"; do
            echo "    $line"
            i=$((i + 1))
            if (( i >= 20 )); then
                echo "    … ($(( ${#real[@]} - 20 )) more omitted)"
                break
            fi
        done
    else
        echo "  real: (none)"
    fi

    # Size warning for agent context
    if (( unity_bytes > 100000 )); then
        echo "  note: unity.log >100K — do not cat the whole file; dig around ScriptFailed/@t only"
    fi
}

main() {
    local run_dir
    run_dir="$(resolve_run_dir "$TARGET")" || exit 1
    load_noise_patterns
    load_bad_patterns

    local run_id
    run_id="$(basename "$run_dir")"
    local total_bytes
    total_bytes=$(du -sb "$run_dir" 2>/dev/null | awk '{print $1}')

    echo "=== Multiplayer smoke triage: $run_id ==="
    echo "path: $run_dir"
    echo "total_size: $(human_bytes "${total_bytes:-0}") (apparent; player binaries usually hardlinked to Builds/)"
    echo "noise_allowlist: $NOISE_FILE (${#NOISE_PATTERNS[@]} patterns)"
    echo "bad_denylist: $BAD_FILE (${#BAD_PATTERNS[@]} patterns)"

    local procs=()
    if [[ -d "$run_dir/server" ]]; then
        procs+=("$run_dir/server")
    fi
    while IFS= read -r -d '' d; do
        procs+=("$d")
    done < <(find "$run_dir" -maxdepth 1 -type d -name 'client-*' -print0 2>/dev/null | sort -z)

    if ((${#procs[@]} == 0)); then
        echo "error: no server/ or client-* under $run_dir" >&2
        exit 1
    fi

    local p
    for p in "${procs[@]}"; do
        triage_process "$p"
    done

    echo
    echo "── verdict hints ──"
    echo "1. Prefer ScriptFailed payload + last Test signal over unity.log prose."
    echo "2. unity_bad_patterns hits fail the harness (known_unity_bad.patterns) — fix source, do not allowlist."
    echo "3. If only known_noise exceptions remain, fix source or (after rebuild) consider harness allowlist — do not grow patterns casually."
    echo "4. Agent: never cat full unity.log; open JSON around failing signal, then ±40 lines of unity.log."
}

main
