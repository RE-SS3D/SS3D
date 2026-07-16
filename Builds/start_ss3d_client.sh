#!/usr/bin/env bash
# Launches the SS3D client build and connects it to a dedicated server.
# Usage: ./start_ss3d_client.sh [server_ip] [port] [ckey] [extra args...]
set -euo pipefail

SERVER_IP="${1:-127.0.0.1}"
PORT="${2:-2222}"
CKEY="${3:-john}"
EXTRA_ARGS=("${@:4}")

cd "$(dirname "$0")/Game"

./SS3D.x86_64 -ip="$SERVER_IP" -port="$PORT" -ckey="$CKEY" -skipintro "${EXTRA_ARGS[@]}"
