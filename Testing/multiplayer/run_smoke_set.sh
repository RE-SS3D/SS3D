#!/usr/bin/env bash
# Run the curated smoke set (smoke-set.txt) through run_smoketest.sh, one scenario after another.
# Every scenario runs even after a failure; the exit code is non-zero if any failed.
#
# Usage: ./Testing/multiplayer/run_smoke_set.sh [--skip-nightly-only]
# Same build environment variables as run_smoketest.sh.

set -uo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
skip_nightly=0
[[ "${1:-}" == "--skip-nightly-only" ]] && skip_nightly=1

failed=()
passed=()
while read -r scenario clients flag; do
    [[ -z "$scenario" || "$scenario" == \#* ]] && continue
    if [[ "$flag" == "nightly" && "$skip_nightly" -eq 1 ]]; then
        echo "skip: $scenario (nightly only)"
        continue
    fi
    start=$(date +%s)
    if "$SCRIPT_DIR/run_smoketest.sh" "$scenario" "$clients"; then
        passed+=("$scenario ($(( $(date +%s) - start ))s)")
    else
        failed+=("$scenario ($(( $(date +%s) - start ))s)")
    fi
done < "$SCRIPT_DIR/smoke-set.txt"

echo "== Smoke set: ${#passed[@]} passed, ${#failed[@]} failed =="
for s in "${passed[@]}"; do echo "PASS $s"; done
for s in "${failed[@]}"; do echo "FAIL $s"; done
[[ ${#failed[@]} -eq 0 ]]
