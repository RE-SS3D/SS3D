#!/usr/bin/env bash
# Keep one open GitHub issue per failing scheduled run family (D9 in
# Documents/architecture/2026-11_testing-philosophy.md).
#
#   red:   open an issue labelled <label>, or comment on the one already open.
#   green: close the open issue with a link to the passing run.
#
# Usage: Tools/ci/nightly_issue.sh <label> <title> <success|failure> <details>
# Needs GH_TOKEN with issues: write, GITHUB_REPOSITORY, GITHUB_SERVER_URL and GITHUB_RUN_ID.

set -euo pipefail

label="${1:?usage: nightly_issue.sh <label> <title> <success|failure> <details>}"
title="${2:?title required}"
outcome="${3:?outcome required}"
details="${4:-}"

repo="${GITHUB_REPOSITORY:?}"
run_url="${GITHUB_SERVER_URL:-https://github.com}/${repo}/actions/runs/${GITHUB_RUN_ID:?}"
today="$(date -u +%Y-%m-%d)"

gh label create "$label" --repo "$repo" --color B60205 \
    --description "Opened and closed by the scheduled test runs" >/dev/null 2>&1 || true

open_issue="$(gh issue list --repo "$repo" --label "$label" --state open --limit 1 --json number --jq '.[0].number // empty')"

if [[ "$outcome" == "success" ]]; then
    if [[ -n "$open_issue" ]]; then
        gh issue close "$open_issue" --repo "$repo" --comment "Green again on $today: $run_url"
        echo "Closed #$open_issue"
    else
        echo "Green; no open $label issue"
    fi
    exit 0
fi

body="$(printf '**%s** failed on %s: %s\n\n%s\n' "$title" "$today" "$run_url" "$details")"

if [[ -n "$open_issue" ]]; then
    gh issue comment "$open_issue" --repo "$repo" --body "$body"
    echo "Commented on #$open_issue"
else
    footer=$'\n\n---\nOpened by the scheduled run. It closes itself on the next green run; agents check it at session start (AGENTS.md).'
    gh issue create --repo "$repo" --label "$label" --title "$title ($today)" --body "$body$footer"
fi
