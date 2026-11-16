#!/usr/bin/env bash
# Run SS3D PlayMode tests (tier 2 scenarios + nightly-only PlayMode tests) in a batchmode editor.
#
# Usage:
#   ./Tools/run_playmode_tests.sh                       # every PlayMode test
#   ./Tools/run_playmode_tests.sh --category Scenarios  # NUnit category (';' separates several)
#   ./Tools/run_playmode_tests.sh --filter AccessScenarios
#
# Writes artifacts/playmode-results.xml and artifacts/playmode.log.
#
# Not -nographics: scenarios spawn rendered prefabs, and a null graphics device breaks
# Shader.Find-based presentation code. Needs a display; on the runner that is the headless GPU
# Xorg on :0 (DISPLAY defaults to it).
#
# Forces the Player subtarget: a server build leaves the Library's active subtarget on Server,
# and every later editor then compiles its own assemblies with UNITY_SERVER (AudioSubSystem NRE,
# headless code paths; see networking-session.md Pitfalls).

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_VERSION="$(sed -n 's/^m_EditorVersion: //p' "$REPO_ROOT/ProjectSettings/ProjectVersion.txt")"
UNITY_EDITOR="${UNITY_EDITOR_PATH:-$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity}"
export DISPLAY="${DISPLAY:-:0}"

CATEGORY=""
FILTER=""

while [[ $# -gt 0 ]]; do
    case "$1" in
        --category|-c)
            CATEGORY="${2:?--category requires a value}"
            shift 2
            ;;
        --filter|-f)
            FILTER="${2:?--filter requires a value}"
            shift 2
            ;;
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
RESULTS="$REPO_ROOT/artifacts/playmode-results.xml"
LOG="$REPO_ROOT/artifacts/playmode.log"
rm -f "$RESULTS"

ARGS=(
    -batchmode
    -projectPath "$REPO_ROOT"
    -buildTarget Linux64
    -standaloneBuildSubtarget Player
    -runTests
    -testPlatform PlayMode
    -testResults "$RESULTS"
    -logFile "$LOG"
)

if [[ -n "$CATEGORY" ]]; then
    ARGS+=(-testCategory "$CATEGORY")
fi
if [[ -n "$FILTER" ]]; then
    ARGS+=(-testFilter "$FILTER")
fi

echo "== PlayMode tests (Unity ${UNITY_VERSION}, DISPLAY=${DISPLAY}) =="
[[ -n "$CATEGORY" ]] && echo "category: $CATEGORY"
[[ -n "$FILTER" ]] && echo "filter: $FILTER"

set +e
"$UNITY_EDITOR" "${ARGS[@]}"
UNITY_EXIT=$?
set -e

if [[ ! -f "$RESULTS" ]]; then
    echo "error: no results XML at $RESULTS (Unity exit $UNITY_EXIT); see $LOG" >&2
    grep -m 20 -E "error CS|Scripts have compiler errors|Aborting batchmode" "$LOG" >&2 || true
    exit 1
fi

set +e
"$REPO_ROOT/Tools/summarize_editmode_results.py" "$RESULTS" ./Tools/run_playmode_tests.sh
SUMMARY_EXIT=$?
set -e

if (( SUMMARY_EXIT != 0 )); then
    exit "$SUMMARY_EXIT"
fi
if (( UNITY_EXIT != 0 )); then
    echo "warning: Unity exited $UNITY_EXIT but the XML reports every test passed; check $LOG" >&2
    exit "$UNITY_EXIT"
fi
exit 0
