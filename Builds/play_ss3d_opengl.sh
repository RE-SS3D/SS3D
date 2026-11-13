#!/usr/bin/env bash
# Start the Linux client on OpenGL instead of Vulkan. Much faster on some machines, but bullet
# holes and blood decals don't show: URP has no DBuffer decals on OpenGL.
# With no arguments it opens the main menu; any arguments (e.g. -ip=... -ckey=...) are passed on.
set -euo pipefail
SCRIPT_DIR="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
if [ "$#" -eq 0 ]; then set -- -forceLauncher; fi
exec "$SCRIPT_DIR/Game/SS3D-Client" -force-glcore "$@"
