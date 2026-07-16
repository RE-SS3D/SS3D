#!/usr/bin/env bash
# Launches the headless SS3D dedicated server build (StandaloneLinux64, Server subtarget).
# Usage: ./start_ss3d_server.sh [extra args passed through to the binary]
set -euo pipefail

cd "$(dirname "$0")/GameServer"

./SS3D.x86_64 -batchmode -nographics -serveronly -port=2222 -logFile server.log "$@"
