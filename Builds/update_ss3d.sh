#!/usr/bin/env bash
# Update an extracted Linux client release. Requires Python 3. Launches the game only with --restart
# (the in-game Update button passes --wait-pid and --restart).
set -euo pipefail
SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
exec python3 "$SCRIPT_DIR/update_ss3d.py" "$@"
