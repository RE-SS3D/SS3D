#!/usr/bin/env bash
# Runs the headless SS3D dedicated server (StandaloneLinux64, Server subtarget).
# Usage: ./start_ss3d_server.sh [port] [extra args passed through to the binary]
# Logs go to GameServer/server.log.
set -euo pipefail

PORT="${1:-1151}"
EXTRA_ARGS=("${@:2}")

cd "$(dirname "$0")/GameServer"
# Local builds are SS3D.x86_64; release zips ship SS3D-Server.
BIN=./SS3D.x86_64
[ -e "$BIN" ] || BIN=./SS3D-Server
chmod +x "$BIN" 2>/dev/null || true

exec "$BIN" -batchmode -nographics -serveronly -port="$PORT" -logFile server.log "${EXTRA_ARGS[@]}"
