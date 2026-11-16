#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
venv="artifacts/unity-mcp/venv"
if [[ ! -x "$venv/bin/mcp-for-unity" ]]; then
    python3 -m venv "$venv"
    "$venv/bin/pip" install -r Testing/ui/requirements-mcp.txt
fi
export UNITY_MCP_DISABLE_TELEMETRY=true
exec "$venv/bin/mcp-for-unity" --transport http --http-url "${SS3D_MCP_URL:-http://127.0.0.1:8080}"
