#!/usr/bin/env bash
# Print a nightly/weekly phase's wall-clock time against its budget (D7 in
# Documents/architecture/2026-11_testing-philosophy.md). Over budget is a warning, not a
# failure: it means "cull before this tier gets anything new".
#
# Usage: Tools/ci/budget.sh <label> <budget-minutes> <start-epoch-seconds>
# In GitHub Actions it also writes a ::warning:: annotation and a line to the job summary.

set -euo pipefail

label="${1:?usage: budget.sh <label> <budget-minutes> <start-epoch>}"
budget_min="${2:?budget minutes required}"
start="${3:?start epoch required}"

elapsed=$(( $(date +%s) - start ))
budget=$(( budget_min * 60 ))
human=$(printf '%dm%02ds' $(( elapsed / 60 )) $(( elapsed % 60 )))

if (( elapsed > budget )); then
    line="$label: $human of ${budget_min}m budget (OVER by $(( (elapsed - budget + 59) / 60 ))m; cull before adding to this tier)"
    echo "::warning title=Over budget::$line"
    status=":warning:"
else
    line="$label: $human of ${budget_min}m budget ($(( elapsed * 100 / budget ))%)"
    status=":white_check_mark:"
fi
echo "$line"

if [[ -n "${GITHUB_STEP_SUMMARY:-}" ]]; then
    echo "- $status $line" >> "$GITHUB_STEP_SUMMARY"
fi
