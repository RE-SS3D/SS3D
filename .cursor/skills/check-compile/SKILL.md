---
name: check-compile
description: >-
  Runs a Unity script compile check (no EditMode tests, no player build) via
  Tools/check_compile.sh. Prefers a fast Editor.log parse when the interactive
  Editor holds the project; otherwise uses headless batchmode. Use after
  implementing a plan or code change, when the user asks for a compile check,
  or before claiming implementation done without running the full suite.
---

# Check compile

Fast gate for “does it compile?” Prefer this over EditMode or smoke.

## Checklist

```
- [ ] Run ./Tools/check_compile.sh (auto picks fast vs batch)
- [ ] On failure: fix listed diagnostics; re-run (keep Editor open for fast loops)
- [ ] Do not claim green without a successful run (or user-confirmed Console)
```

## Step 1: Run

```bash
./Tools/check_compile.sh           # auto
./Tools/check_compile.sh --fast    # force Editor.log path
./Tools/check_compile.sh --batch   # force headless Unity
./Tools/check_compile.sh --fast --wait 60  # after edits: wait for a new Tundra line
```

| Mode | When | Cost |
|------|------|------|
| **fast** | Interactive Editor has this project open (auto), or `--fast` | Seconds — parses `~/.config/unity3d/Editor.log` (override `UNITY_EDITOR_LOG`) |
| **batch** | No interactive Editor (auto), or `--batch` | Minutes — Unity startup + `CompileCheck.RunBatch` |

- Writes a trailing slice to `artifacts/compile.log`; summarizer reads the real Editor log in fast mode.
- Unity version for batch: `6000.3.16f1` (`UNITY_EDITOR_PATH` / `SS3D_UNITY_VERSION`).
- **Fix loops:** keep the Editor open and use auto/`--fast` (optionally `--wait N` after saving edits). Do not close/reopen Unity between iterations.
- If batchmode is already running on this project, the script refuses to start another — wait or kill it.

## Step 2: Triage failures

From stdout (unique `: error CODE:` lines in the **latest** Tundra window — `CS*`, `UNT*`, etc.):

1. Open the cited file/line; fix minimally.
2. Docs-first if the error spans a domain: [INDEX](../../../Documents/architecture/INDEX.md)
   → system map **Pitfalls**.
3. Do **not** dump the whole Editor.log — re-run the script or
   `./Tools/summarize_compile_log.py ~/.config/unity3d/Editor.log`.
4. Re-run until OK.

## When to escalate

| Need | Use instead |
|------|-------------|
| Behavior / unit regressions | **fix-editmode-tests** / `./Tools/run_editmode_tests.sh` |
| Multiplayer runtime | **multiplayer-smoke-e2e** or **run-multiplayer-smoke** |

## Fallbacks

| Situation | Action |
|-----------|--------|
| No Unity CLI and no Editor | Ask user to open the project and confirm Console has no script errors |
| Stale Editor.log / Editor closed | Use `--batch` (close interactive Editor first) |
| Want only latest window dump | `./Tools/summarize_compile_log.py <log>` |

## Do not

- Launch `--batch` in a tight fix loop while the Editor could stay open — use **fast**.
- Treat a compile pass as EditMode or smoke coverage.
- Commit `artifacts/` or Editor dirt from opening the project.
- Edit `Documents/design/*` or `Documents/FORK_STATUS.md` unless the owner asks.
- Loop forever — after two unsuccessful fix attempts, stop and report hypothesis + evidence.
