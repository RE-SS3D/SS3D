---
name: fix-editmode-tests
description: >-
  Runs SS3D EditMode tests headlessly, summarizes failures from NUnit XML, and
  fixes failing tests. Use when the user asks to run EditMode tests, fix failing
  unit tests, or triage editmode-results.xml / Test Runner failures.
---

# Fix EditMode tests

Run EditMode tests → read the summary → fix root causes → re-run focused filters.
Prefer the CLI scripts over asking the user to paste Test Runner output.

## Checklist

```
- [ ] Run Tools/run_editmode_tests.sh (full or --filter)
- [ ] Read summarizer output; open only the failing test + SUT files
- [ ] Docs-first: INDEX → system map for the failing domain (AGENTS.md)
- [ ] Fix minimally; do not edit Documents/design/*
- [ ] Re-run with --filter until green; then full suite if the change is broad
```

## Step 1: Run tests

```bash
./Tools/run_editmode_tests.sh
# focused:
./Tools/run_editmode_tests.sh --filter InteractionPipeline
./Tools/run_editmode_tests.sh --assembly SS3D.Tests.EditMode
```

- Unity: `6000.3.16f1` (override with `UNITY_EDITOR_PATH` / `SS3D_UNITY_VERSION`).
- Writes `artifacts/editmode-results.xml` + `artifacts/editmode.log` (same layout as CI
  `.github/workflows/editmodetestrunner.yml`).
- Full suite can take several minutes (~300+ `[Test]`s). Set a high shell block time.
- If the Editor is already open on this project, batchmode may fail to lock the project —
  ask the user to close it, or use pasted Test Runner failures instead.

Already have XML (CI artifact / prior run)?

```bash
./Tools/summarize_editmode_results.py artifacts/editmode-results.xml
```

## Step 2: Triage failures

From the summarizer, for each failure:

1. Note **classname** + **method** + assertion/exception message.
2. Open the test under `Assets/Scripts/Tests/EditMode/` (also `AssetAudit/`).
3. Open the system under test; navigate via
   [Documents/architecture/INDEX.md](../../../Documents/architecture/INDEX.md) → system map
   **Pitfalls** before broad grepping.
4. Do **not** dump entire `artifacts/editmode.log` — `rg` the failing method name, read ~40 lines.

## Step 3: Fix

- Prefer fixing production code when the test correctly encodes intended behavior.
- Update the test only when the contract intentionally changed (and note divergence in the
  system map / plan — never in `Documents/design/*`).
- Match existing test style (`EditModeTest` base in `Assets/Scripts/Tests/Common/`).
- Keep changes scoped; no drive-by refactors.

## Step 4: Verify

```bash
./Tools/run_editmode_tests.sh --filter '<method or | joined from summarizer>'
```

If the fix touches shared infrastructure, run the full suite once more before claiming done.

## Fallbacks

| Situation | Action |
|-----------|--------|
| No Unity CLI | Ask user to run **Window → General → Test Runner** (EditMode), export/paste failures, then fix from that list |
| CI only | `gh run download` the “Test results” artifact from `editmodetestrunner.yml`, summarize the XML |
| Compile error, no XML | Prefer **check-compile** / `./Tools/check_compile.sh`; or rg `artifacts/editmode.log` for `: error ` |

## Do not

- Treat PlayMode / multiplayer smoke as part of this skill (use **run-multiplayer-smoke** /
  **triage-multiplayer-smoke**).
- Edit `Documents/design/*` or `Documents/FORK_STATUS.md` unless the owner asks.
- Commit `artifacts/` or Unity mat/font/URP dirt from opening the Editor.
- Loop forever on the same failure — after two unsuccessful fix attempts, stop and report
  hypothesis + evidence.
