#!/usr/bin/env bash
# Start or stop a GUI editor + MCP server for this checkout on the headless display, for
# unattended runs (weekly-graphical.yml). Interactive work uses the usual local editor script.
#
# Usage: Tools/ci/editor_session.sh start|stop
# Env: DISPLAY (default :0), SS3D_MCP_URL (default http://127.0.0.1:8097).

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")/../.."
export DISPLAY="${DISPLAY:-:0}"
export SS3D_MCP_URL="${SS3D_MCP_URL:-http://127.0.0.1:8097}"
project="$PWD"

editor_pid() { pgrep -f "Editor/Unity -projectPath $project( |\$)" | head -1 || true; }
editor_window() { wmctrl -lp | awk -v pid="$1" '$3 == pid && / Unity - / {print $1; exit}'; }

case "${1:-}" in
start)
    # Two editors on one display fight over real input and the machine-wide MCPForUnity.HttpUrl EditorPref.
    # This includes a stale editor for this checkout (a previous run that did not stop cleanly);
    # hanging on its old MCP connection would be worse than failing fast.
    if others=$(pgrep -af 'Editor/Unity .*-projectPath' | grep -v -e '-batchmode'); then
        echo "error: another Unity editor is open; stop it first (Tools/ci/editor_session.sh stop or ss3d-dev stop):" >&2
        echo "$others" >&2
        exit 1
    fi
    mkdir -p artifacts/ui
    [[ -f artifacts/ui/editor.log ]] && mv artifacts/ui/editor.log artifacts/ui/editor.prev.log
    # A killed editor leaves scene backups and the next start blocks on "Recovering Scene Backups",
    # which nobody can answer in an unattended run. The weekly needs a clean scene anyway.
    rm -rf Temp/__Backupscenes
    nohup Testing/ui/start-mcp.sh > artifacts/ui/mcp.log 2>&1 &
    nohup Testing/ui/open-unity.sh > /dev/null 2>&1 &
    echo "waiting for the editor to connect to MCP (a cold import takes ~25 min)..."
    for i in $(seq 480); do
        grep -q "MCP connected: True" artifacts/ui/editor.log 2>/dev/null && break
        sleep 5
        if [[ -z "$(editor_pid)" ]]; then
            echo "error: the editor exited during start-up; see artifacts/ui/editor.log" >&2
            tail -30 artifacts/ui/editor.log >&2 || true
            exit 1
        fi
        # Fail fast on a modal dialog instead of waiting out the full 40 minutes.
        if (( i % 12 == 0 )); then
            report=$(Testing/ui/watch-dialogs.sh 2>&1) && rc=0 || rc=$?
            if (( rc == 1 )); then
                echo "error: modal dialog during start-up; answer it and retry:" >&2
                echo "$report" >&2
                exit 1
            fi
        fi
    done
    grep -q "MCP connected: True" artifacts/ui/editor.log || { echo "error: no MCP connection after 40 min" >&2; exit 1; }
    # Keep the editor windowed: the remote-client pilot drives the built client with real OS input
    # and needs its window visible; a maximized editor covers it. The known-good size is 1101x533.
    window=$(editor_window "$(editor_pid)")
    if [[ -n "$window" ]]; then
        wmctrl -i -r "$window" -b remove,maximized_vert,maximized_horz || true
        wmctrl -i -r "$window" -e 0,0,0,1101,533 || true
    fi
    Testing/ui/watch-dialogs.sh || echo "warning: dialog(s) open after start-up, see above" >&2
    echo "editor ready"
    ;;
stop)
    # Close the window rather than killing the editor: a kill leaves scene backups that prompt next start.
    pid=$(editor_pid)
    if [[ -n "$pid" ]]; then
        window=$(editor_window "$pid")
        [[ -n "$window" ]] && wmctrl -i -c "$window"
        for _ in $(seq 90); do kill -0 "$pid" 2>/dev/null || break; sleep 1; done
        if kill -0 "$pid" 2>/dev/null; then
            echo "warning: the editor did not quit (open dialog?); killing it" >&2
            kill -9 "$pid"
        fi
    fi
    pkill -f "$project/artifacts/unity-mcp/venv/bin/mcp-for-unity" || true
    pkill -f "mcp-for-unity --transport http --http-url $SS3D_MCP_URL" || true
    rm -f mono_crash.*.blob
    echo "stopped"
    ;;
*)
    echo "usage: editor_session.sh start|stop" >&2
    exit 2
    ;;
esac
