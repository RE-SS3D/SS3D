#!/usr/bin/env bash
set -euo pipefail

# =============================================================================
# setup-unity-wsl.sh — Install Unity Editor on WSL2 Ubuntu for EditMode testing
# =============================================================================
# Project: SS3D
# Unity version: 2021.3.15f1 (changeset e8e88683f834)
#
# Usage:  sudo ./setup-unity-wsl.sh
#
# After running this script, activate your license then run tests with:
#   /opt/unity/2021.3.15f1/Editor/Unity \
#     -batchmode -nographics \
#     -runTests -testPlatform EditMode \
#     -projectPath /home/jakub/matrix/SS3D \
#     -testResults test-results.xml \
#     -logFile unity-test-run.log
# =============================================================================

if [[ $EUID -ne 0 ]]; then
    echo "ERROR: This script must be run as root (sudo ./setup-unity-wsl.sh)"
    exit 1
fi

UNITY_VERSION="2021.3.15f1"
UNITY_CHANGESET="e8e88683f834"
UNITY_INSTALL_DIR="/opt/unity/${UNITY_VERSION}"
UNITY_GPG_KEY="/usr/share/keyrings/unity.gpg"
UNITY_REPO_FILE="/etc/apt/sources.list.d/unityhub.list"
UNITY_KEY_URL="https://hub.unity3d.com/linux/keys/public"
UNITY_REPO_URL="https://hub.unity3d.com/linux/repos/deb"

echo "============================================"
echo " SS3D Unity Test Runner — WSL2 Setup"
echo " Unity ${UNITY_VERSION} (${UNITY_CHANGESET})"
echo "============================================"
echo ""

# ---- Step 0: Nuke any broken Unity repo, then install prerequisites -------
echo "[0/5] Installing system prerequisites..."

# Remove any stale/broken Unity repo files first — if a repo was added
# without a valid GPG key, apt-get update will fail before we can fix it.
for f in /etc/apt/sources.list.d/unity*.list /etc/apt/sources.list.d/unityhub*.list; do
    if [[ -f "$f" ]]; then
        echo "  Removing stale repo file: $f"
        rm -f "$f"
    fi
done

# Also remove any stale GPG key so we start clean
rm -f "${UNITY_GPG_KEY}"

apt-get update -qq
apt-get install -y -qq \
    wget \
    gnupg \
    ca-certificates \
    libasound2 \
    libatomic1 \
    libc++1 \
    libgconf-2-4 \
    libgtk2.0-0 \
    libnotify4 \
    libnss3 \
    libxss1 \
    libxtst6 \
    xvfb \
    unzip \
    curl

echo "  Done."

# ---- Step 1: Add Unity Hub repository (key first, then repo) --------------
echo "[1/5] Adding Unity Hub repository..."

# Download and install the GPG key
wget -qO - "${UNITY_KEY_URL}" | gpg --dearmor --batch --yes -o "${UNITY_GPG_KEY}"

# Add the repository
echo "deb [signed-by=${UNITY_GPG_KEY}] ${UNITY_REPO_URL} stable main" \
    > "${UNITY_REPO_FILE}"

apt-get update -qq

echo "  Done."

# ---- Step 2: Install Unity Hub --------------------------------------------
echo "[2/5] Installing Unity Hub..."

if command -v unityhub &>/dev/null; then
    echo "  Unity Hub already installed — skipping."
else
    apt-get install -y -qq unityhub 2>/dev/null || {
        # Fallback: download AppImage for headless install capability
        echo "  .deb install failed — falling back to AppImage..."
        TMP_DIR=$(mktemp -d)
        curl -sSfL -o "${TMP_DIR}/unityhub.AppImage" \
            "https://public-cdn.cloud.unity3d.com/hub/prod/UnityHub.AppImage"
        chmod +x "${TMP_DIR}/unityhub.AppImage"
        cp "${TMP_DIR}/unityhub.AppImage" /usr/local/bin/unityhub
        chmod +x /usr/local/bin/unityhub
        rm -rf "$TMP_DIR"
        echo "  Unity Hub AppImage installed to /usr/local/bin/unityhub"
    }
fi

echo "  Done."

# ---- Step 3: Install Unity Editor (headless) ------------------------------
echo "[3/5] Installing Unity Editor ${UNITY_VERSION}..."
echo "  (This downloads ~3-4 GB and may take several minutes)"

if [[ -x "${UNITY_INSTALL_DIR}/Editor/Unity" ]]; then
    echo "  Unity ${UNITY_VERSION} already installed at ${UNITY_INSTALL_DIR} — skipping."
else
    unityhub --headless install \
        --version "${UNITY_VERSION}" \
        --changeset "${UNITY_CHANGESET}"

    if [[ -x "${UNITY_INSTALL_DIR}/Editor/Unity" ]]; then
        echo "  Unity Editor installed successfully."
    else
        echo "  ERROR: Unity Editor binary not found at expected path."
        echo "  Expected: ${UNITY_INSTALL_DIR}/Editor/Unity"
        echo "  Installed editors:"
        unityhub --headless editors --installed 2>&1 || true
        exit 1
    fi
fi

echo "  Done."

# ---- Step 4: Install Linux build support module ---------------------------
echo "[4/5] Installing Linux build support..."

if [[ -d "${UNITY_INSTALL_DIR}/Editor/Data/PlaybackEngines/LinuxStandaloneSupport" ]]; then
    echo "  Linux build support already installed — skipping."
else
    unityhub --headless install-modules \
        --version "${UNITY_VERSION}" \
        --modules linux-server 2>&1 || {
        echo "  WARNING: Could not install linux-server module via Hub."
        echo "  This is non-fatal — EditMode tests may still work without it."
        echo "  If tests fail about missing LinuxStandaloneSupport, re-run:"
        echo "    unityhub --headless install-modules --version ${UNITY_VERSION} --modules linux-server"
    }
fi

echo "  Done."

# ---- Step 5: Verify and write convenience wrapper --------------------------
echo "[5/5] Verifying setup and creating test-runner wrapper..."

if [[ ! -x "${UNITY_INSTALL_DIR}/Editor/Unity" ]]; then
    echo "  ERROR: Unity binary not found. Installation may have failed."
    exit 1
fi

UNITY_BIN="${UNITY_INSTALL_DIR}/Editor/Unity"

# Create convenience script for running EditMode tests
WRAPPER_PATH="/usr/local/bin/run-ss3d-tests"
cat > "${WRAPPER_PATH}" << 'WRAPPER_EOF'
#!/usr/bin/env bash
# run-ss3d-tests — Run SS3D EditMode tests in batch mode
set -euo pipefail

PROJECT_PATH="${SS3D_PATH:-/home/jakub/matrix/SS3D}"
RESULTS_DIR="${PROJECT_PATH}/TestResults"
mkdir -p "${RESULTS_DIR}"

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RESULTS_FILE="${RESULTS_DIR}/editmode-${TIMESTAMP}.xml"
LOG_FILE="${RESULTS_DIR}/unity-log-${TIMESTAMP}.log"

echo "Running EditMode tests..."
echo "  Project:  ${PROJECT_PATH}"
echo "  Results:  ${RESULTS_FILE}"
echo "  Log:      ${LOG_FILE}"
echo ""

/opt/unity/2021.3.15f1/Editor/Unity \
    -batchmode \
    -nographics \
    -runTests \
    -testPlatform EditMode \
    -projectPath "${PROJECT_PATH}" \
    -testResults "${RESULTS_FILE}" \
    -logFile "${LOG_FILE}"

EXIT_CODE=$?

echo ""
echo "============================================"
if [[ $EXIT_CODE -eq 0 ]]; then
    echo " ALL TESTS PASSED"
else
    echo " TESTS FAILED (exit code: ${EXIT_CODE})"
fi
echo " Results: ${RESULTS_FILE}"
echo " Log:     ${LOG_FILE}"
echo "============================================"

exit $EXIT_CODE
WRAPPER_EOF

chmod +x "${WRAPPER_PATH}"

echo "  Convenience wrapper written to: ${WRAPPER_PATH}"
echo "  Usage: SS3D_PATH=/path/to/project run-ss3d-tests"
echo ""

# ---- Summary --------------------------------------------------------------
echo ""
echo "============================================"
echo " SETUP COMPLETE"
echo "============================================"
echo ""
echo " Unity Editor: ${UNITY_INSTALL_DIR}/Editor/Unity"
echo " Test runner:  run-ss3d-tests"
echo ""
echo "----------------------------------------"
echo " NEXT STEP — Activate your license"
echo "----------------------------------------"
echo ""
echo " You MUST activate a Unity license before tests will run."
echo ""
echo " Option A — Interactive (requires WSLg / X server):"
echo "   ${UNITY_BIN} -projectPath /home/jakub/matrix/SS3D"
echo "   # Then: Help → Manage License → Activate"
echo ""
echo " Option B — Manual activation file:"
echo "   1. Get your license file from https://license.unity3d.com/manual"
echo "   2. Place it at ~/.config/unity3d/Unity/Unity_lic.ulf"
echo "   # Or use the alf (manual activation file) flow"
echo ""
echo " Option C — Username/password (batch mode):"
echo "   ${UNITY_BIN} \\"
echo "     -batchmode -nographics -quit \\"
echo "     -username YOUR_EMAIL \\"
echo "     -password YOUR_PASSWORD \\"
echo "     -logFile activate.log"
echo "   cat activate.log  # Check for success/failure"
echo ""
echo " Option D — Serial number:"
echo "   ${UNITY_BIN} \\"
echo "     -batchmode -nographics -quit \\"
echo "     -serial YOUR_SERIAL \\"
echo "     -username YOUR_EMAIL \\"
echo "     -password YOUR_PASSWORD \\"
echo "     -logFile activate.log"
echo ""
echo "----------------------------------------"
echo " ONCE LICENSED — Run tests"
echo "----------------------------------------"
echo ""
echo "   run-ss3d-tests"
echo ""
echo " Or manually:"
echo "   ${UNITY_BIN} \\"
echo "     -batchmode -nographics \\"
echo "     -runTests -testPlatform EditMode \\"
echo "     -projectPath /home/jakub/matrix/SS3D \\"
echo "     -testResults test-results.xml \\"
echo "     -logFile unity-test-run.log"
echo ""
echo "============================================"
