> Implements: none (infrastructure — CI/test tooling, not a gameplay domain)
> Touches systems: networking-session, electricity, area, ingame-console, logging
> Status: in-progress

**Progress:** Phase 0 runner online; EditMode and develop-release prefer TomNAS with GitHub fallback. Todos (folded in from the archived [self_hosted_ci_smoke plan](../archive/plans/self_hosted_ci_smoke.plan.md)):

- [x] Register the runner, Library stash, pull the Unity image
- [x] `multiplayer-smoke-test.yml` on self-hosted + Library stash + nightly
- [ ] `workflow_dispatch` smoke on TomNAS; confirm a warm second run
- [ ] Phase 1: harness SIGKILL + DSL + server-kill scenarios
- [x] Phase 2: folded into the `observer-sync` smoke scenario (2026-11): `lightdebug` switches the area lights, the observing client checks with `observesync lights-off` ([testing philosophy](2026-11_testing-philosophy.md#harness-contracts) D5)

# Multiplayer testing on self-hosted CI (Jul 2026)

## Goal

Take multiplayer play-testing off the owner's dev machine and off GitHub's metered
minutes, and grow the harness to catch the classes of desync it currently can't.

Today's loop blocks the dev box for ~45 min per cold build and burns rate-limited
`unity_tests` minutes; the `Testing/multiplayer/` harness
([Multiplayer testing](systems/networking-session-testing.md)) is solid but
runs **entirely headless** (`-batchmode -nographics`), so it structurally cannot observe
rendered or UI-presented state. The three bugs that motivated this effort each land in a
different bucket:

| Bug | Bucket | Where it's caught |
|---|---|---|
| Server close → client freeze | Lifecycle/robustness | **Phase 1** — headless, existing harness + a server-kill scenario |
| Lights not synced client↔server | State desync | **Phase 2** — headless, new console assert command (atmos-client-sync pattern) |
| Diegetic machine screen leaks to host | Visual/UI presence | **Phase 3 (deferred)** — needs a rendered client |

## Decisions

- **Orchestration: GitHub self-hosted runner**, not Jenkins. The owner's local x64 box
  (Docker + a persistent Unity `Library/` cache) registers as a runner; the existing
  workflows move onto it with a `runs-on` change and near-zero other edits. Keeps results in
  the GitHub UI already in use, off metered minutes, off the dev box. Jenkins is reserved for
  the later always-on soak server (Phase 4), not for replacing CI runs.
- **Coverage: Phases 1–2 (headless) first.** They reuse the current harness and its proven
  idioms and catch two of the three motivating bugs cheaply. The rendered-client harness
  (Phase 3) is the only path to the UI-leak bug and to the still-deferred drop/pickup +
  selection-outline regressions, but it's a materially heavier lift and gets its own effort.

## Phase 0 — Self-hosted runner + warm build cache

The single biggest lever on build time is a **persistent `Library/` cache**, not the CI
tool. CI pays ~45 min because every run re-imports assets and does a full IL2CPP compile from
an empty `Library`; on a persistent box with a cached Library, server+client rebuilds drop to
minutes.

### Ops (TomNAS — provisioned Jul 2026)

| Item | Value |
|------|--------|
| SSH | `ssh tomnas` (user `cu6e`) |
| Runner name | `TomNAS-unity` |
| Labels | `self-hosted`, `linux`/`Linux`, `x64`/`X64`, `unity` |
| Install dir | `/home/cu6e/actions-runner` |
| Service | user systemd `github-actions-runner.service` (`systemctl --user …`) |
| Library stash (smoke) | `/home/cu6e/.cache/ss3d/Library` (restore copies it into the workspace, ~20 s; only a green job moves its Library back, so a failed or cancelled run keeps the last green stash) |
| Library stash (EditMode) | `/home/cu6e/.cache/ss3d/Library-editmode` (separate so EditMode and builds don't overwrite each other's cache) |
| Manual clone slot | `/home/cu6e/Dev/` (optional; CI checks out into runner `_work`) |

Still required once was: docker group + linger + image pull (done Jul 2026). Pre-pull:

```bash
ssh tomnas 'docker pull unityci/editor:ubuntu-6000.3.16f1-linux-il2cpp-3'
```

Without the docker group, `game-ci/unity-builder` fails on the daemon socket. Without linger,
the user systemd unit dies when all `cu6e` sessions end.

### Workflow

- [`.github/workflows/multiplayer-smoke-test.yml`](../../.github/workflows/multiplayer-smoke-test.yml):
  `runs-on: [self-hosted, linux, x64, unity]`; restore the Library stash with `cp -a`, save it with `mv` after a green job;
  **one** `unity-builder` step with `ClientAndServerBuildScript.BuildBothForCi`;
  `runAsHostUser: true`; nightly `cron: "0 4 * * *"`.
- [`.github/workflows/editmodetestrunner.yml`](../../.github/workflows/editmodetestrunner.yml):
  `run_tests` runs on `[self-hosted, linux, x64, unity]` + `Library-editmode` stash +
  `runAsHostUser: true`; `workflow_dispatch` input `runner: tomnas|github` can pick
  `ubuntu-latest` instead. There is no automatic fallback: `github.token` cannot list repo
  runners (HTTP 403), so the old online probe always fell back. While TomNAS is offline, jobs
  queue until it returns.
- [`.github/workflows/develop-release.yml`](../../.github/workflows/develop-release.yml):
  same TomNAS default in `prepare`; Linux via **one** `BuildBothForCi` step; shares
  smoke’s `/home/cu6e/.cache/ss3d/Library` stash on self-hosted; nightly `cron: "0 5 * * *"`
  (Windows + Linux client/server → floating `develop-nightly`). Manual dispatch stays
  Windows-default with opt-in Linux.
- Build methods for isolated server/client still exist for local/menu use; smoke / release
  Linux CI must not split them across two Editor sessions.

Net effect: the exact 45-min / metered-minutes pain the CI-pipeline doc
([2026-07_ci-develop-release-pipeline.md](2026-07_ci-develop-release-pipeline.md)) records
goes away, without changing what the smoke tests *do* yet.

## Phase 1 — Lifecycle / robustness scenarios (headless, high value)

Covers the **server-close → client-freeze** bug directly, and generalizes to ungraceful
disconnect handling.

- **Harness orchestration (`run_smoketest.sh` + `lib/process.sh`):** add a way for a scenario
  to have the harness kill the *server* PID mid-run (ungraceful `SIGKILL`, distinct from the
  client-initiated `disconnect` DSL instruction and from the existing `trap` cleanup that
  tears everything down at EXIT). The client scenario then continues and must prove it noticed.
- **Client watchdog assertion:** after the server dies, the client must emit a disconnect
  `Test signal` (e.g. `ServerLost`) and complete its script within N seconds. A client that
  hangs never emits `ScriptComplete`, which the harness already treats as a failure
  (`wait_for_signal ... ScriptComplete 90`) — so "freeze" surfaces as a timeout without new
  gate logic. Add an explicit signal so a *clean* detected-disconnect is distinguishable from
  a hang in triage.
- New scenario `scenarios/server-kill{,-client}.txt`; may need a small DSL addition
  (`wait_disconnected` / `expect_server_loss`) in `AutomationScript.cs` +
  `AutomationSubSystem.cs`, mirroring the existing `wait_connected` / `disconnect` verbs.
- Follow-ups in the same shape once the mechanism exists: mid-round server kill, client kill
  from the server's perspective, transport timeout.

## Phase 2 — State-sync assert commands (headless, proven pattern)

> Done differently (2026-11): no separate light-sync scenario. `observer-sync` checks lights, doors,
> held items, damage and atmos from a second client; see
> [networking-session-testing](systems/networking-session-testing.md#tests).

Covers the **light-sync** desync. This is a near-verbatim reuse of the `atmos-client-sync`
scenario: a console command forces a change, then a `...status assert` command **throws** when
the client's replicated state disagrees with server truth, which `AutomationSubSystem`'s
try/catch turns into a `ScriptFailed` the harness already fails on — no new DSL or
`run_smoketest.sh` changes needed.

- Add lighting/electricity debug + assert console commands paralleling `AtmosDebugCommand` /
  `AtmosClientStatusCommand` (see the atmos-client-sync entry in
  [Multiplayer testing](systems/networking-session-testing.md)): force a known
  power/light state server-side, then have the client assert its `LightPower` SyncVar / area
  lighting snapshot matches. See [electricity](systems/electricity.md) (LightPower SyncVar) and
  [area](systems/area.md) (client lighting snapshot).
- New scenario `scenarios/light-sync{,-client}.txt` on the same idiom.
- The same "distinctive signal / exception-on-`console`" technique extends to any other
  replicated numeric/boolean state whose desync is otherwise invisible headlessly (area power,
  door state, machine on/off).

## Deferred (own efforts)

- **Phase 3 — Rendered-client harness.** Run the client **non-headless under Xvfb** on the
  box, screenshot on a `Test signal`, and assert UI-presence / pixel state. Only path to the
  **machine-UI-leak-to-host** bug as a visual fact, and it simultaneously reopens the three
  gaps the harness doc lists as unported: mouse/screen-space **drop-pickup**, **selection
  outline** ([2026-07_headless-dedicated-server.md](../archive/architecture/2026-07_headless-dedicated-server.md)
  Known issues), and *visual* light sync. Materially heavier (real display, OS input
  injection, screenshot diffing) — its own architecture effort.
- **Phase 4 — 24/7 soak server.** Long-lived dedicated server + scripted bot clients running
  continuous connect/round/interact loops, triaged nightly. This is the Jenkins-shaped piece;
  build it on the persistent box once Phases 1–3 exist.

## Pitfalls (anticipated)

- **Warm `Library/` cache is the whole win — protect it.** A step that clears it, or a
  container that doesn't mount the volume, silently drops you back to 45-min cold builds with
  no error. Verify cache reuse in run logs. **Never stash a failed build's Library** — a
  half-written `PackageCache` poisons the next warm run. Wipe leftover workspace `Library/`
  before restore.
- **One Editor session for server+client.** Two sequential `unity-builder` steps (server then
  client) restart Docker/Unity against a shared Library and race `PackageCache` on this host
  (missing localization sources → CS2001; missing `Unity.Cecil.Awesome.dll` → CS0006). Smoke
  uses `ClientAndServerBuildScript.BuildBothForCi` in a single step.
- **Headless Linux client needs `SDL_VIDEODRIVER=dummy`.** Without DISPLAY, Unity 6 client
  players select window backend `(null)` and SIGSEGV in `PlayerMain` before any game code.
  Dedicated server builds are fine. Harness defaults the env var in `lib/process.sh`.
- **Server-kill vs. harness cleanup.** The harness already `trap`s EXIT/INT/TERM to
  `kill_tracked_pids`; a deliberate mid-run server kill must not confuse that PID bookkeeping
  or trip the "server script did not complete" path as a false failure. The server dying is
  expected in this scenario — gate on the *client's* signal, not the server's `ScriptComplete`.
- **Don't let a hang look like a pass.** A frozen client that emits nothing must fail via the
  `ScriptComplete` timeout; make sure no scenario accidentally lets an empty/absent log read as
  success.
- **Self-hosted runner secrets.** License activation still needs the `unity_tests` Environment
  secrets ([2026-07_ci-develop-release-pipeline.md](2026-07_ci-develop-release-pipeline.md)
  Pitfalls) — a self-hosted runner does not change that a job without `environment: unity_tests`
  sees empty `UNITY_LICENSE`/`UNITY_SERIAL`.
- **EditMode fallback needs an online probe, not a second `runs-on`.** A job pinned only to
  TomNAS queues forever when the runner is offline (`timeout-minutes` starts after assign).
  `editmodetestrunner.yml` therefore has a cheap `pick_runner` on `ubuntu-latest` that reads
  `TomNAS-unity`'s Actions status and chooses self-hosted vs hosted. If GitHub marks the
  runner offline while the systemd unit is still up, force `runner=tomnas` via
  `workflow_dispatch` or restart `github-actions-runner.service` on TomNAS.
- **Separate EditMode Library stash.** EditMode uses `Library-editmode`; smoke uses `Library`.
  Sharing would let a failed EditMode discard wipe the warm build cache.

## Out of scope

- Windows client coverage (harness stays `StandaloneLinux64`, matching current scope).
- The rendered-client and soak-server efforts themselves (Phases 3–4, deferred above).
- Pocket/container round-trip regression (needs a `stop_round` DSL verb + headless
  inventory-state assertion; tracked as a Known gap in
  [Multiplayer testing](systems/networking-session-testing.md)).

## Related docs

- [Multiplayer testing](systems/networking-session-testing.md) — the harness Phases 1–2 extend
- [2026-07_ci-develop-release-pipeline.md](2026-07_ci-develop-release-pipeline.md) — the workflows Phase 0 moves onto the runner
- [2026-07_headless-dedicated-server.md](../archive/architecture/2026-07_headless-dedicated-server.md) — the deferred visual regressions Phase 3 would reopen
- [networking-session.md](systems/networking-session.md), [electricity.md](systems/electricity.md), [area.md](systems/area.md)
