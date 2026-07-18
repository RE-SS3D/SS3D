---
name: check-compile
description: >-
  Runs a headless Unity script compile check (no EditMode tests, no player
  build) via Tools/check_compile.sh. Use after implementing a plan or code
  change to verify the project compiles, when the user asks for a compile
  check, or before claiming implementation done without running the full suite.
---

# Check compile

Fast gate: open the project in batchmode, wait for script compilation, report
pass/fail. Prefer this over EditMode or smoke when you only need “does it
compile?”

## Checklist

```
- [ ] Close Editor on this project if batchmode lock fails (ask user)
- [ ] Run ./Tools/check_compile.sh
- [ ] On failure: fix error CS lines from the summary; re-run
- [ ] Do not claim green without a successful run (or user-confirmed Editor compile)
```

## Step 1: Run

```bash
./Tools/check_compile.sh
```

- Unity: `6000.3.16f1` (override with `UNITY_EDITOR_PATH` / `SS3D_UNITY_VERSION`).
- Writes `artifacts/compile.log`.
- Invokes `-executeMethod SS3D.Editor.CompileCheck.RunBatch` (no tests / no build).
- Typically much faster than `./Tools/run_editmode_tests.sh`, but still a Unity
  startup — set a high enough shell block time (often 1–3+ minutes with a warm
  `Library/`).
- If the Editor already has this project open, batchmode may fail to lock —
  ask the user to close it.

## Step 2: Triage failures

From the script stdout (unique `: error CODE:` lines — `CS*`, analyzer `UNT*`, etc.):

1. Open the cited file/line; fix minimally.
2. Docs-first if the error spans a domain: [INDEX](../../../Documents/architecture/INDEX.md)
   → system map **Pitfalls**.
3. Do **not** dump the whole log — `rg ':\s*error\s+[A-Z]+[0-9]+:' artifacts/compile.log` if needed.
4. Re-run `./Tools/check_compile.sh` until OK.

## When to escalate

| Need | Use instead |
|------|-------------|
| Behavior / unit regressions | **fix-editmode-tests** / `./Tools/run_editmode_tests.sh` |
| Multiplayer runtime | **multiplayer-smoke-e2e** or **run-multiplayer-smoke** |

## Fallbacks

| Situation | Action |
|-----------|--------|
| No Unity CLI | Ask user to open the project in the Editor and confirm Console has no script errors |
| Project lock | User closes Editor, then re-run |
| `executeMethod` missing but `: error …:` present | Trust those diagnostic lines; fix those first |

## Do not

- Treat a compile pass as EditMode or smoke coverage.
- Commit `artifacts/` or Editor dirt from opening the project.
- Edit `Documents/design/*` or `Documents/FORK_STATUS.md` unless the owner asks.
- Loop forever — after two unsuccessful fix attempts, stop and report hypothesis + evidence.
