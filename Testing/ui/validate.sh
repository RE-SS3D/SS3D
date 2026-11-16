#!/usr/bin/env bash
# Validation in the open editor: preflight → compile → EditMode by default; add focused steps with --steps.
# Usage: Testing/ui/validate.sh [--steps a,b|--nightly|--weekly]. Run outside the agent Bash sandbox (X11 watchdog).
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
exec artifacts/unity-mcp/venv/bin/python Testing/ui/validate.py "$@"
