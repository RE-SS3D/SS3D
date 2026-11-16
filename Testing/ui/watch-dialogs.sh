#!/usr/bin/env bash
# Report Unity editor windows that may be blocking it (modal dialogs) and capture each one.
# Exit 0: none. Exit 1: dialog(s) found, PNGs under artifacts/ui/dialogs/. Exit 2: no editor / no X.
# Needs X11 access (DISPLAY=:0 under XWayland): run outside the agent's Bash sandbox.
set -uo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/../.."
repo=$PWD
out=artifacts/ui/dialogs
export DISPLAY=${DISPLAY:-:0}

# Match the editor binary only, not shells whose command line mentions the path.
mapfile -t pids < <(pgrep -f "Editor/Unity -projectPath $repo( |\$)")
if (( ${#pids[@]} == 0 )); then
    echo "No Unity editor running for $repo" >&2
    exit 2
fi
if ! windows=$(wmctrl -l -p 2>&1); then
    echo "wmctrl failed (sandboxed or no X display?): $windows" >&2
    exit 2
fi

# The main window, and progress windows that come and go during compiles, imports and builds
# (Unity titles some progress bars just "Unity", e.g. "Compiling Scripts").
main='^Unity - '
progress='^(Unity$|Compiling|Building|Hold on|Importing|Reloading|Refresh|Domain Reload|Preparing|Packaging|Baking|Loading)'

found=0
while read -r id _desk pid _host title; do
    [[ " ${pids[*]} " == *" $pid "* ]] || continue
    [[ $title =~ $main ]] && continue
    if [[ ${title,,} =~ ${progress,,} ]]; then
        echo "busy: $title"
        continue
    fi
    found=1
    mkdir -p "$out"
    slug=$(tr -c 'A-Za-z0-9' '-' <<<"$title" | tr -s '-' | sed 's/^-//; s/-$//' | cut -c1-60)
    png="$out/$(date -u +%Y%m%d-%H%M%S)-${slug:-untitled}.png"
    if import -window "$id" "$png" 2>/dev/null; then
        echo "DIALOG: $title -> $png"
    else
        echo "DIALOG: $title (capture failed)"
    fi
done <<<"$windows"
exit $found
