#!/usr/bin/env bash
# Headless script-compile check (no EditMode tests, no player build).
#
# Usage:
#   ./Tools/check_compile.sh
#
# Writes:
#   artifacts/compile.log
#
# Exit 0 if scripts compile; non-zero on compiler errors or Unity failure.
# Close the Editor on this project first — batchmode cannot lock an open project.
#
# Requires Unity 6000.3.16f1 (override with UNITY_EDITOR_PATH / SS3D_UNITY_VERSION).

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_VERSION="${SS3D_UNITY_VERSION:-6000.3.16f1}"
UNITY_EDITOR="${UNITY_EDITOR_PATH:-$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity}"
METHOD="SS3D.Editor.CompileCheck.RunBatch"

while [[ $# -gt 0 ]]; do
    case "$1" in
        -h|--help)
            sed -n '2,14p' "$0"
            exit 0
            ;;
        *)
            echo "error: unknown argument: $1" >&2
            exit 1
            ;;
    esac
done

if [[ ! -x "$UNITY_EDITOR" ]]; then
    echo "error: Unity Editor not found/executable at: $UNITY_EDITOR" >&2
    echo "Set UNITY_EDITOR_PATH or install ${UNITY_VERSION} via Unity Hub." >&2
    exit 1
fi

mkdir -p "$REPO_ROOT/artifacts"
LOG="$REPO_ROOT/artifacts/compile.log"
rm -f "$LOG"

echo "== Compile check (Unity ${UNITY_VERSION}) =="
echo "editor: $UNITY_EDITOR"
echo "method: $METHOD"
echo "log: $LOG"

# No -quit: CompileCheck.RunBatch calls EditorApplication.Exit after compilation settles.
set +e
"$UNITY_EDITOR" \
    -batchmode \
    -nographics \
    -projectPath "$REPO_ROOT" \
    -executeMethod "$METHOD" \
    -logFile "$LOG"
UNITY_EXIT=$?
set -e

if [[ ! -f "$LOG" ]]; then
    echo "error: no compile log at $LOG (Unity exit $UNITY_EXIT)" >&2
    exit 1
fi

# Authoritative signal: diagnostic errors in the log (Unity exit codes are unreliable
# when scripts fail before -executeMethod can run). Matches CSC (CS*) and analyzers
# treated as errors (UNT*, IDE*, RCS*, …): "path(line,col): error CODE: message".
mapfile -t DIAG_ERRORS < <(grep -E ':\s*error\s+[A-Z]+[0-9]+:' "$LOG" | sort -u || true)
OTHER_FAIL=0
if grep -qE 'Scripts have compiler errors\.|CompileCheck: FAILED' "$LOG"; then
    OTHER_FAIL=1
fi

if (( ${#DIAG_ERRORS[@]} > 0 )) || (( OTHER_FAIL != 0 )); then
    echo "Compile FAILED"
    if (( ${#DIAG_ERRORS[@]} > 0 )); then
        printf '%s\n' "${DIAG_ERRORS[@]}"
        echo "(${#DIAG_ERRORS[@]} unique error line(s); full log: $LOG)"
    else
        echo "(see $LOG for details)"
    fi
    exit 1
fi

if (( UNITY_EXIT != 0 )); then
    echo "error: Unity exited $UNITY_EXIT with no diagnostic error lines — see $LOG" >&2
    # Common: project lock while Editor is open, missing executeMethod, license.
    if grep -qiE 'Another Unity instance|Could not find method|Failed to resolve|No valid Unity|license' "$LOG"; then
        grep -iE 'Another Unity instance|Could not find method|Failed to resolve|No valid Unity|license' "$LOG" | sort -u >&2 || true
    fi
    exit "$UNITY_EXIT"
fi

if ! grep -q 'CompileCheck: OK' "$LOG"; then
    echo "warning: Unity exited 0 but CompileCheck: OK not found — treating as pass if no diagnostic errors" >&2
fi

echo "Compile OK"
exit 0
