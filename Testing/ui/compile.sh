#!/usr/bin/env bash
# Refresh, recompile and list every compiler/analyzer error in the open editor; nonzero on failure.
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
exec artifacts/unity-mcp/venv/bin/python Testing/ui/validate.py --steps compile "$@"
