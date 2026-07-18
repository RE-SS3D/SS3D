#!/usr/bin/env bash
# Run SS3D EditMode tests headlessly via the Unity Editor CLI.
#
# Usage:
#   ./Tools/run_editmode_tests.sh
#   ./Tools/run_editmode_tests.sh --filter InteractionPipeline
#   ./Tools/run_editmode_tests.sh --assembly SS3D.Tests.EditMode
#
# Writes (matching CI layout from .github/workflows/editmodetestrunner.yml):
#   artifacts/editmode-results.xml
#   artifacts/editmode.log
#
# Requires Unity 6000.3.16f1 (project version) on PATH via UNITY_EDITOR_PATH, or the
# default Hub install under ~/Unity/Hub/Editor/6000.3.16f1/Editor/Unity.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_VERSION="${SS3D_UNITY_VERSION:-6000.3.16f1}"
UNITY_EDITOR="${UNITY_EDITOR_PATH:-$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity}"

FILTER=""
ASSEMBLY=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --filter|-f)
            FILTER="${2:?--filter requires a value}"
            shift 2
            ;;
        --assembly|-a)
            ASSEMBLY="${2:?--assembly requires a value}"
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

if [[ ! -x "$UNITY_EDITOR" ]]; then
    echo "error: Unity Editor not found/executable at: $UNITY_EDITOR" >&2
    echo "Set UNITY_EDITOR_PATH or install ${UNITY_VERSION} via Unity Hub." >&2
    exit 1
fi

mkdir -p "$REPO_ROOT/artifacts"
RESULTS="$REPO_ROOT/artifacts/editmode-results.xml"
LOG="$REPO_ROOT/artifacts/editmode.log"
rm -f "$RESULTS"

ARGS=(
    -batchmode
    -nographics
    -projectPath "$REPO_ROOT"
    -runTests
    -testPlatform EditMode
    -testResults "$RESULTS"
    -logFile "$LOG"
)

if [[ -n "$ASSEMBLY" ]]; then
    ARGS+=(-assemblyNames "$ASSEMBLY")
fi
if [[ -n "$FILTER" ]]; then
    ARGS+=(-testFilter "$FILTER")
fi

echo "== EditMode tests (Unity ${UNITY_VERSION}) =="
echo "editor: $UNITY_EDITOR"
echo "results: $RESULTS"
echo "log: $LOG"
[[ -n "$ASSEMBLY" ]] && echo "assembly: $ASSEMBLY"
[[ -n "$FILTER" ]] && echo "filter: $FILTER"

set +e
"$UNITY_EDITOR" "${ARGS[@]}"
UNITY_EXIT=$?
set -e

if [[ ! -f "$RESULTS" ]]; then
    echo "error: no results XML at $RESULTS (Unity exit $UNITY_EXIT) — see $LOG" >&2
    exit 1
fi

"$REPO_ROOT/Tools/summarize_editmode_results.py" "$RESULTS"
SUMMARY_EXIT=$?

# Prefer the summarizer's pass/fail; Unity can also exit non-zero for compile errors.
if (( SUMMARY_EXIT != 0 )); then
    exit "$SUMMARY_EXIT"
fi
if (( UNITY_EXIT != 0 )); then
    echo "warning: Unity exited $UNITY_EXIT but XML reports all tests passed — check $LOG" >&2
    exit "$UNITY_EXIT"
fi
exit 0
