#!/usr/bin/env bash
# SS3D Linux bootstrap/updater. Downloads the newest stable v* release, verifies GitHub's
# SHA-256 asset digest, installs it under ~/.local/share/ss3d/versions/<tag>, and launches it.
#
# Requires: curl, jq, unzip, sha256sum.
# Usage: ./ss3d-launcher.sh [--server HOST] [--port PORT] [--ckey CKEY]
#        [--install-dir DIR] [--tag TAG] [--no-launch] [-- extra game args]
set -euo pipefail

REPO="RE-SS3D/SS3D"
API="https://api.github.com/repos/$REPO"
INSTALL_DIR="${SS3D_INSTALL_DIR:-$HOME/.local/share/ss3d}"
SERVER="${SS3D_SERVER:-127.0.0.1}"
PORT="${SS3D_PORT:-1151}"
CKEY="${SS3D_CKEY:-player}"
TAG_OVERRIDE=""
NO_LAUNCH=0
EXTRA=()

while (($#)); do
    case "$1" in
        --server) SERVER="$2"; shift 2 ;;
        --port) PORT="$2"; shift 2 ;;
        --ckey) CKEY="$2"; shift 2 ;;
        --install-dir) INSTALL_DIR="$2"; shift 2 ;;
        --tag) TAG_OVERRIDE="$2"; shift 2 ;;
        --no-launch) NO_LAUNCH=1; shift ;;
        --) shift; EXTRA=("$@"); break ;;
        -h|--help)
            sed -n '2,10p' "$0"; exit 0 ;;
        *) echo "unknown argument: $1" >&2; exit 2 ;;
    esac
done

for tool in curl jq unzip sha256sum; do
    command -v "$tool" >/dev/null 2>&1 || {
        echo "error: $tool is required" >&2
        exit 1
    }
done

VERSIONS="$INSTALL_DIR/versions"
VERSION_FILE="$INSTALL_DIR/.version"
mkdir -p "$VERSIONS"
LOCAL="$(cat "$VERSION_FILE" 2>/dev/null || echo "0.0.0")"

find_exe() {
    find "$1" -type f \( -name 'SS3D-Client' -o -name 'SS3D.x86_64' \) 2>/dev/null | head -1
}

launch() {
    local exe="$1"
    chmod +x "$exe"
    if (( NO_LAUNCH )); then
        echo "installed: $exe"
        return 0
    fi
    cd "$(dirname "$exe")"
    exec "$exe" -ip="$SERVER" -port="$PORT" -ckey="$CKEY" -skipintro "${EXTRA[@]}"
}

# API failure should not block an already-installed build.
api_get() {
    curl -fsSL --retry 2 --max-time 30 \
        -H 'Accept: application/vnd.github+json' \
        -H 'User-Agent: SS3D-Launcher' \
        "$1"
}

if [[ -n "$TAG_OVERRIDE" ]]; then
    TAG="$TAG_OVERRIDE"
else
    RELEASES="$(api_get "$API/releases?per_page=50" 2>/dev/null || true)"
    TAG="$(printf '%s' "$RELEASES" | jq -r '.[].tag_name' 2>/dev/null \
        | grep -E '^v[0-9]+\.[0-9]+\.[0-9]+$' | sort -V | tail -1 || true)"
fi

if [[ -z "$TAG" ]]; then
    LOCAL_EXE="$(find_exe "$VERSIONS/$LOCAL" || true)"
    if [[ -n "$LOCAL_EXE" ]]; then
        echo "warning: no stable v* release found; launching local $LOCAL" >&2
        launch "$LOCAL_EXE"
        exit 0
    fi
    echo "error: no stable v* release found" >&2
    exit 1
fi

LOCAL_EXE="$(find_exe "$VERSIONS/$LOCAL" || true)"
if [[ "$TAG" == "$LOCAL" && -n "$LOCAL_EXE" ]]; then
    echo "already on $TAG"
    launch "$LOCAL_EXE"
    exit 0
fi

RELEASE_JSON="$(api_get "$API/releases/tags/$TAG")"
ASSET_NAME="$(printf '%s' "$RELEASE_JSON" \
    | jq -r '.assets[] | select(.name | startswith("SS3D-Linux-Client-")) | .name' | head -1)"
if [[ -z "$ASSET_NAME" ]]; then
    echo "error: no Linux client asset on $TAG" >&2
    exit 1
fi
DIGEST="$(printf '%s' "$RELEASE_JSON" \
    | jq -r --arg n "$ASSET_NAME" '.assets[] | select(.name == $n) | .digest')"
URL="$(printf '%s' "$RELEASE_JSON" \
    | jq -r --arg n "$ASSET_NAME" '.assets[] | select(.name == $n) | .browser_download_url')"
if [[ -z "$DIGEST" || "$DIGEST" == "null" ]]; then
    echo "error: $ASSET_NAME has no SHA-256 digest; refusing to update" >&2
    exit 1
fi

TMP="$(mktemp -d /tmp/ss3d-launcher.XXXXXX)"
trap 'rm -rf "$TMP"' EXIT
echo "downloading $TAG ($ASSET_NAME)"
curl -fL --retry 3 --progress-bar -o "$TMP/$ASSET_NAME" "$URL"

ACTUAL="$(sha256sum "$TMP/$ASSET_NAME" | awk '{print $1}')"
EXPECTED="${DIGEST#sha256:}"
if [[ "$ACTUAL" != "$EXPECTED" ]]; then
    echo "error: SHA-256 mismatch (expected $EXPECTED, got $ACTUAL)" >&2
    exit 1
fi

unzip -q "$TMP/$ASSET_NAME" -d "$TMP/extract"
NEW_EXE="$(find_exe "$TMP/extract")"
if [[ -z "$NEW_EXE" ]]; then
    echo "error: no SS3D client executable in $ASSET_NAME" >&2
    exit 1
fi

rm -rf "$VERSIONS/$TAG.new"
mv "$TMP/extract" "$VERSIONS/$TAG.new"
rm -rf "$VERSIONS/$TAG"
mv "$VERSIONS/$TAG.new" "$VERSIONS/$TAG"
printf '%s\n' "$TAG" > "$VERSION_FILE"

# Keep the current and one previous version.
ls -1dt "$VERSIONS"/*/ 2>/dev/null | tail -n +3 | xargs -r rm -rf

EXE="$(find_exe "$VERSIONS/$TAG")"
[[ -n "$EXE" ]] || { echo "error: installed build has no executable" >&2; exit 1; }
launch "$EXE"
