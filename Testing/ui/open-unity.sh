#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
version=$(sed -n 's/^m_EditorVersion: //p' ProjectSettings/ProjectVersion.txt)
editor=${SS3D_UNITY_EDITOR:-"$HOME/Unity/Hub/Editor/$version/Editor/Unity"}
if [[ ! -x "$editor" ]]; then
    echo "Unity $version not found at $editor. Set SS3D_UNITY_EDITOR to its executable." >&2
    exit 1
fi
mkdir -p artifacts/ui
exec "$editor" -projectPath "$PWD" \
    -executeMethod SS3D.Editor.Automation.UnityMcpBootstrap.Connect \
    -logFile "$PWD/artifacts/ui/editor.log"
