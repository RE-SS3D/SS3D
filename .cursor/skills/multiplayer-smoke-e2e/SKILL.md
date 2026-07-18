---
name: multiplayer-smoke-e2e
description: >-
  End-to-end multiplayer smoke loop: build Linux client+server, run
  run_smoketest.sh, triage .runs logs, and fix failures then rebuild/re-run.
  Use when the user asks for a full smoke cycle, build-and-smoke, E2E multiplayer
  test, or to fix smoke failures after a local rebuild.
---

# Multiplayer smoke end-to-end

Orchestrates build → smoke → triage → fix → rebuild/re-run. Prefer this over
calling the smaller skills separately when the user wants a full cycle.

Underlying pieces (do not restate their full docs — open them when needed):

| Step | Skill / tool |
|------|----------------|
| Build | `Tools/build_client_and_server.sh` (or Editor menu) |
| Run | **run-multiplayer-smoke** / `Testing/multiplayer/run_smoketest.sh` |
| Triage | **triage-multiplayer-smoke** / `Testing/multiplayer/tools/triage_run.sh` |
| Fix | This skill’s fix loop (docs-first) |

## Checklist

```
- [ ] Step 1: Build client + server (or --skip-build if bins are fresh)
- [ ] Step 2: Run smoke scenario; capture RUN_ID + exit code
- [ ] Step 3: Triage the run (never cat full unity.log)
- [ ] Step 4: If failed — fix root cause, then rebuild + re-run (max 2 fix cycles)
- [ ] Report final verdict + run ids
```

## Defaults

| Knob | Default |
|------|---------|
| Scenario | `basic-round` (1 client) |
| Alt scenario | `late-join 2` if user asks |
| Unity | `6000.3.16f1` (`UNITY_EDITOR_PATH` / `SS3D_UNITY_VERSION`) |
| Max fix cycles | **2** (build is expensive) |

Skip build only when the user says builds are current, or passes `--skip-build`.

## Step 1: Build

```bash
./Tools/build_client_and_server.sh
```

- Log: `artifacts/build-client-server.log` — on failure, `rg` for `error CS` / `Build failed`, do not dump the whole log.
- **Project lock:** if batchmode cannot open the project, ask the user to close the Editor
  *or* run `SS3D/Build/Client + Dedicated Server (Linux)`, then continue from Step 2.
- Outputs: `Builds/GameServer/SS3D.x86_64`, `Builds/Game/SS3D.x86_64`.

## Step 2: Run smoke

```bash
./Testing/multiplayer/run_smoketest.sh basic-round
# or: ./Testing/multiplayer/run_smoketest.sh late-join 2
```

- Block until finished (minutes). Staging uses hardlinks when possible.
- Non-zero exit with a populated `.runs/<id>/` is normal when the game is buggy — continue to triage.
- Capture `RUN_ID` from the banner / `Logs preserved at:` line.

## Step 3: Triage

```bash
./Testing/multiplayer/tools/triage_run.sh <RUN_ID|latest>
```

Follow **triage-multiplayer-smoke** order: `script_failed` → signals → json errors → real
unity exceptions → known_noise. Dig with `jq` / small `rg` windows only.

If the run **PASSED**: stop — report PASS + run id. No fix loop.

## Step 4: Fix (only on failure)

1. Identify root cause from triage (not noise buckets).
2. Docs-first: [INDEX.md](../../../Documents/architecture/INDEX.md) → system map **Pitfalls**
   (often [networking-session.md](../../../Documents/architecture/systems/networking-session.md),
   [rounds-lobby.md](../../../Documents/architecture/systems/rounds-lobby.md),
   [permissions.md](../../../Documents/architecture/systems/permissions.md)).
3. Minimal code fix. Never edit `Documents/design/*`. Update system-map Pitfalls if you hit a
   silent headless failure.
4. **Rebuild** (Step 1) — player builds do not pick up C# changes otherwise.
5. Re-run smoke (Step 2) + triage (Step 3).

Stop after **2** unsuccessful fix cycles (same or shifting failure). Report hypothesis,
evidence (signals / ScriptFailed), and what to try next. Do not thrash builds.

## Scope boundaries

- **In scope:** harness wiring, automation waits, headless guards, permissions seeding,
  log/signal issues, clear NREs the scenario hits.
- **Out of scope for endless looping:** large gameplay redesigns, mouse/screen-space
  interaction coverage, Windows builds. Surface those and stop.
- **Not this skill:** EditMode unit tests → **fix-editmode-tests**.

## Do not

- `cat` full `unity.log` or `artifacts/build-client-server.log`.
- Commit Unity mat/font/URP/Addressables dirt or `artifacts/`.
- Edit `Documents/design/*` / `Documents/FORK_STATUS.md` unless the owner asks.
- Claim PASS without a green `run_smoketest.sh` exit after the latest rebuild.
