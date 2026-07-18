---
name: run-multiplayer-smoke
description: >-
  Runs the headless multiplayer smoke harness (run_smoketest.sh) after a local
  client/server build. Use when the user asks to run smoke, kick off
  basic-round or late-join, or start Testing/multiplayer after building via the
  Editor menu.
---

# Run multiplayer smoke

Kick off `run_smoketest.sh`, report the run id / exit code, then hand off to
**triage-multiplayer-smoke**.

For **build → smoke → triage → fix** in one loop, use **multiplayer-smoke-e2e** instead.

## Checklist

```
- [ ] Confirm Builds/GameServer and Builds/Game binaries exist (or env overrides)
- [ ] Run run_smoketest.sh with scenario + client count
- [ ] Capture RUN_ID from the banner / "Logs preserved at" line
- [ ] Tell the user exit code + run id; next step is triage (do not cat unity.log)
```

## Prerequisites (user builds in Editor)

Expected outputs (unless overridden):

| Role | Default path | Editor menu |
|------|--------------|-------------|
| Dedicated server | `Builds/GameServer/SS3D.x86_64` | `SS3D/Build/Dedicated Server (Linux)` |
| Client | `Builds/Game/SS3D.x86_64` | `SS3D/Build/Client (Linux)` |

Or combined: `SS3D/Build/Client + Dedicated Server (Linux)`.

Overrides: `SS3D_SERVER_BUILD_DIR`, `SS3D_CLIENT_BUILD_DIR`, `SS3D_SERVER_BIN_NAME`,
`SS3D_CLIENT_BIN_NAME`.

## Step 1: Choose scenario

| Scenario | Command | Notes |
|----------|---------|-------|
| `basic-round` (default) | `./Testing/multiplayer/run_smoketest.sh basic-round` | 1 client |
| `late-join` | `./Testing/multiplayer/run_smoketest.sh late-join 2` | needs 2 clients |

Ask the user only if they did not name a scenario; otherwise default to `basic-round`.

## Step 2: Run the harness

```bash
./Testing/multiplayer/run_smoketest.sh <scenario> [client-count]
```

- Block until it finishes (can take a couple of minutes for scenario waits; staging is cheap
  when hardlinks work).
- Staging hardlinks player builds into `.runs/<id>/` and real-copies only `Config`/`Data`/`Logs`.
  `du` of a single run dir can still look large; actual free-space growth across many runs should
  stay small. A "falling back to full copy" warning means disk will grow ~180–300 MB per process.
- Exit non-zero on ScriptFailed, JSON Error/Fatal, or unity.log exception signatures is
  **success for the harness** when investigating bugs: the run dir is the artifact.

Banner line looks like:

```text
== Multiplayer smoke test: basic-round, 1 client(s) (run <RUN_ID>) ==
```

End line:

```text
Logs preserved at: .../Testing/multiplayer/.runs/<RUN_ID>
```

## Step 3: Hand off

Report: scenario, exit code, `RUN_ID`, path under `Testing/multiplayer/.runs/`.

Next: run **triage-multiplayer-smoke** (`./Testing/multiplayer/tools/triage_run.sh <RUN_ID>`
or `latest`). Do not dump `unity.log`.

## Do not

- Rebuild the game from this skill — use **multiplayer-smoke-e2e** or
  `Tools/build_client_and_server.sh` / the Editor menu.
- Treat a failing scenario as “harness broken” without triage — many fails are real game bugs.
- Treat `total_size` from triage as "apparent" tree size — hardlinked player binaries are shared
  with `Builds/` and sibling runs; do not delete `Builds/` while old hardlinked `.runs/` exist
  if you still need those runs.
- Edit `Documents/design/*`.
- Commit Unity mat/font/URP/Addressables dirt from opening the Editor.
