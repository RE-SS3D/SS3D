#!/usr/bin/env bash
# Script-compile check (no EditMode tests, no player build).
#
# Usage:
#   ./Tools/check_compile.sh           # auto: fast if interactive Editor holds project
#   ./Tools/check_compile.sh --fast    # parse ~/.config/unity3d/Editor.log (Editor open)
#   ./Tools/check_compile.sh --batch   # headless Unity batchmode (Editor must be closed)
#   ./Tools/check_compile.sh --wait 60 # fast mode: wait up to N seconds for a new compile
#
# Writes a slice/copy summary context to artifacts/compile.log when useful.
# Exit 0 if scripts compile; non-zero on compiler/analyzer errors.
#
# Requires Unity 6000.3.16f1 for --batch (UNITY_EDITOR_PATH / SS3D_UNITY_VERSION).

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_VERSION="${SS3D_UNITY_VERSION:-6000.3.16f1}"
UNITY_EDITOR="${UNITY_EDITOR_PATH:-$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity}"
METHOD="SS3D.Editor.CompileCheck.RunBatch"
EDITOR_LOG="${UNITY_EDITOR_LOG:-$HOME/.config/unity3d/Editor.log}"
MODE="auto" # auto | fast | batch
WAIT_SECS=0

while [[ $# -gt 0 ]]; do
    case "$1" in
        --fast)
            MODE="fast"
            shift
            ;;
        --batch)
            MODE="batch"
            shift
            ;;
        --wait)
            WAIT_SECS="${2:?--wait requires seconds}"
            shift 2
            ;;
        -h|--help)
            sed -n '2,16p' "$0"
            exit 0
            ;;
        *)
            echo "error: unknown argument: $1" >&2
            exit 1
            ;;
    esac
done

mkdir -p "$REPO_ROOT/artifacts"
OUT_LOG="$REPO_ROOT/artifacts/compile.log"
SUMMARY="$REPO_ROOT/Tools/summarize_compile_log.py"
chmod +x "$SUMMARY" 2>/dev/null || true

# --- detection helpers -------------------------------------------------------

# Match real Editor binaries via /proc (avoid pgrep matching this script's argv).
unity_pids_for_project() {
    local want_batch="$1" # "yes" | "no"
    local pid exe cmd
    for pid in /proc/[0-9]*; do
        pid="${pid##*/}"
        exe=$(readlink "/proc/$pid/exe" 2>/dev/null || true)
        [[ "$exe" == *"/Editor/Unity" ]] || continue
        cmd=$(tr '\0' ' ' <"/proc/$pid/cmdline" 2>/dev/null || true)
        [[ "$cmd" == *"-projectPath ${REPO_ROOT}"* ]] || continue
        if [[ "$want_batch" == "yes" ]]; then
            [[ "$cmd" == *"-batchmode"* ]] || continue
        else
            [[ "$cmd" == *"-batchmode"* ]] && continue
        fi
        echo "$pid"
    done
}

interactive_editor_pids() { unity_pids_for_project no; }
batchmode_pids() { unity_pids_for_project yes; }

resolve_mode() {
    if [[ "$MODE" != "auto" ]]; then
        echo "$MODE"
        return
    fi
    if interactive_editor_pids | grep -q .; then
        echo "fast"
        return
    fi
    echo "batch"
}

# --- fast path: Editor.log ---------------------------------------------------

wait_for_new_tundra() {
    local log="$1"
    local deadline=$((SECONDS + WAIT_SECS))
    local start_lines
    start_lines=$(wc -l <"$log" 2>/dev/null || echo 0)
    echo "waiting up to ${WAIT_SECS}s for a new Tundra build in $log (from line $start_lines)..."
    while (( SECONDS < deadline )); do
        if [[ -f "$log" ]]; then
            # New completed compile after our start offset?
            if tail -n +"$((start_lines + 1))" "$log" 2>/dev/null | grep -qE '^\*\*\* Tundra build (failed|success)'; then
                echo "new Tundra build observed"
                return 0
            fi
        fi
        sleep 0.5
    done
    echo "warning: timed out waiting for new Tundra — summarizing latest window anyway" >&2
    return 0
}

run_fast() {
    if [[ ! -f "$EDITOR_LOG" ]]; then
        echo "error: Editor log not found: $EDITOR_LOG" >&2
        echo "Set UNITY_EDITOR_LOG or open the project once in the Editor." >&2
        exit 1
    fi

    local pids
    pids=$(interactive_editor_pids | tr '\n' ' ' || true)
    echo "== Compile check (fast / Editor.log) =="
    echo "editor-log: $EDITOR_LOG"
    if [[ -n "${pids// /}" ]]; then
        echo "interactive Unity pid(s): $pids"
    else
        echo "note: no interactive Unity for this project — summarizing latest log window (--fast forced or stale log)"
    fi

    if (( WAIT_SECS > 0 )); then
        wait_for_new_tundra "$EDITOR_LOG"
    fi

    # Copy a trailing slice for artifacts (full Editor.log can be huge).
    tail -n 5000 "$EDITOR_LOG" >"$OUT_LOG"

    set +e
    "$SUMMARY" "$EDITOR_LOG"
    local rc=$?
    set -e
    exit "$rc"
}

# --- batch path --------------------------------------------------------------

run_batch() {
    local holders
    holders=$(interactive_editor_pids | tr '\n' ' ' || true)
    if [[ -n "${holders// /}" ]]; then
        echo "error: interactive Editor holds this project (pid(s): $holders)" >&2
        echo "Use ./Tools/check_compile.sh --fast  (or close the Editor for --batch)." >&2
        exit 1
    fi

    holders=$(batchmode_pids | tr '\n' ' ' || true)
    if [[ -n "${holders// /}" ]]; then
        echo "error: another batchmode Unity already holds this project (pid(s): $holders)" >&2
        echo "Wait for it to finish, or kill it, then re-run." >&2
        exit 1
    fi

    if [[ ! -x "$UNITY_EDITOR" ]]; then
        echo "error: Unity Editor not found/executable at: $UNITY_EDITOR" >&2
        echo "Set UNITY_EDITOR_PATH or install ${UNITY_VERSION} via Unity Hub." >&2
        exit 1
    fi

    rm -f "$OUT_LOG"

    echo "== Compile check (batchmode / Unity ${UNITY_VERSION}) =="
    echo "editor: $UNITY_EDITOR"
    echo "method: $METHOD"
    echo "log: $OUT_LOG"

    set +e
    "$UNITY_EDITOR" \
        -batchmode \
        -nographics \
        -projectPath "$REPO_ROOT" \
        -executeMethod "$METHOD" \
        -logFile "$OUT_LOG"
    local unity_exit=$?
    set -e

    if [[ ! -f "$OUT_LOG" ]]; then
        echo "error: no compile log at $OUT_LOG (Unity exit $unity_exit)" >&2
        exit 1
    fi

    set +e
    "$SUMMARY" "$OUT_LOG"
    local summary_exit=$?
    set -e

    if (( summary_exit != 0 )); then
        exit "$summary_exit"
    fi
    if (( unity_exit != 0 )); then
        echo "error: Unity exited $unity_exit but summarizer reported OK — see $OUT_LOG" >&2
        if grep -qiE 'Another Unity instance|Could not find method|Failed to resolve|No valid Unity|license' "$OUT_LOG"; then
            grep -iE 'Another Unity instance|Could not find method|Failed to resolve|No valid Unity|license' "$OUT_LOG" | sort -u >&2 || true
        fi
        exit "$unity_exit"
    fi
    exit 0
}

# --- main --------------------------------------------------------------------

RESOLVED="$(resolve_mode)"
case "$RESOLVED" in
    fast) run_fast ;;
    batch) run_batch ;;
    *)
        echo "error: unknown mode $RESOLVED" >&2
        exit 1
        ;;
esac
