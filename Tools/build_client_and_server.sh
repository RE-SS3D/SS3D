#!/usr/bin/env bash
# Build Linux dedicated server + client for the multiplayer smoke harness.
#
# Usage:
#   ./Tools/build_client_and_server.sh
#
# Invokes SS3D.Editor.ClientAndServerBuildScript.BuildBothBatch via Unity batchmode.
# Outputs (same as Editor menu "SS3D/Build/Client + Dedicated Server (Linux)"):
#   Builds/GameServer/SS3D.x86_64
#   Builds/Game/SS3D.x86_64
#
# Requires Unity 6000.3.16f1 (override with UNITY_EDITOR_PATH / SS3D_UNITY_VERSION).
# Close the Editor on this project first — batchmode cannot lock an open project.

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_VERSION="${SS3D_UNITY_VERSION:-6000.3.16f1}"
UNITY_EDITOR="${UNITY_EDITOR_PATH:-$HOME/Unity/Hub/Editor/${UNITY_VERSION}/Editor/Unity}"
METHOD="SS3D.Editor.ClientAndServerBuildScript.BuildBothBatch"

if [[ ! -x "$UNITY_EDITOR" ]]; then
    echo "error: Unity Editor not found/executable at: $UNITY_EDITOR" >&2
    echo "Set UNITY_EDITOR_PATH or install ${UNITY_VERSION} via Unity Hub." >&2
    echo "Fallback: Editor menu SS3D/Build/Client + Dedicated Server (Linux)." >&2
    exit 1
fi

mkdir -p "$REPO_ROOT/artifacts"
LOG="$REPO_ROOT/artifacts/build-client-server.log"

echo "== Build client + dedicated server (Unity ${UNITY_VERSION}) =="
echo "editor: $UNITY_EDITOR"
echo "method: $METHOD"
echo "log: $LOG"

set +e
"$UNITY_EDITOR" \
    -batchmode \
    -nographics \
    -projectPath "$REPO_ROOT" \
    -executeMethod "$METHOD" \
    -quit \
    -logFile "$LOG"
UNITY_EXIT=$?
set -e

SERVER_BIN="$REPO_ROOT/Builds/GameServer/SS3D.x86_64"
CLIENT_BIN="$REPO_ROOT/Builds/Game/SS3D.x86_64"

if [[ ! -x "$SERVER_BIN" || ! -x "$CLIENT_BIN" ]]; then
    echo "error: build finished without both binaries (Unity exit $UNITY_EXIT)" >&2
    echo "  expected: $SERVER_BIN" >&2
    echo "  expected: $CLIENT_BIN" >&2
    echo "  see: $LOG" >&2
    exit 1
fi

if (( UNITY_EXIT != 0 )); then
    echo "error: Unity exited $UNITY_EXIT — see $LOG" >&2
    exit "$UNITY_EXIT"
fi

echo "OK: $SERVER_BIN"
echo "OK: $CLIENT_BIN"
exit 0
