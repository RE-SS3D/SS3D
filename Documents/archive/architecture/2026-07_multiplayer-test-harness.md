> Implements: Documents/archive/architecture/2026-07_headless-dedicated-server.md#testing-gap-no-multiplayer-test-harness
> Touches systems: networking-session, rounds-lobby, permissions, ingame-console, logging
> Status: shipped

**Progress:** Partial — see Known gaps.

# Multiplayer test harness (Jul 2026)

## Goal

Close the testing gap the headless dedicated-server effort surfaced
([2026-07_headless-dedicated-server.md](2026-07_headless-dedicated-server.md)): every bug found
there (a network session port race, several `[Server]`-path NREs, broken selection outline and
drop interaction against a real remote client) was found by hand — build server, build client,
launch both, play, read two log files, repeat. EditMode tests can't reach any of it (no FishNet
transport, no `UNITY_SERVER` define, no cross-process timing); the PlayMode tests that did exist
ran single-process host mode, exactly the mode that was masking these bugs.

Replace that manual loop, and the brittle multi-process PlayMode harness that partially
automated it, with something that launches real separate server + client processes headlessly,
runs in CI, and is trivially runnable locally.

## Local workflow

**Full loop (agent):** `.cursor/skills/multiplayer-smoke-e2e/SKILL.md` —
`Tools/build_client_and_server.sh` → `run_smoketest.sh` → `triage_run.sh` → fix → rebuild
(max two fix cycles).

Manual / partial:

1. **Build** — `./Tools/build_client_and_server.sh`, or Editor
   `SS3D/Build/Client + Dedicated Server (Linux)` (or the separate Client / Dedicated Server
   items). Outputs: `Builds/Game/SS3D.x86_64`, `Builds/GameServer/SS3D.x86_64`.
2. **Run** — `./Testing/multiplayer/run_smoketest.sh basic-round` (or `late-join 2`).
   Skill: `.cursor/skills/run-multiplayer-smoke/SKILL.md`. Staging hardlinks the player build
   into `Testing/multiplayer/.runs/<id>/` (same filesystem as `Builds/`) and real-copies only
   small writable `Config`/`Data`/`Logs` trees.
3. **Triage** — `./Testing/multiplayer/tools/triage_run.sh latest`.
   Skill: `.cursor/skills/triage-multiplayer-smoke/SKILL.md`.

## Shipped

### In-game automation
- `Assets/Scripts/SS3D/Systems/Testing/AutomationSubSystem.cs` — bootstrapped by
  `SystemsBootstrap.EnsureProcessWideServices` (no `Boot.unity` edit, per [2026-07_agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md)). No-op
  unless the new `-testscript=<path>` CLI arg is set. Drives a headless process through a small
  line-DSL script (`AutomationScript.cs`) using the same client→server broadcast/RPC APIs the
  lobby UI calls (`ChangePlayerReadyMessage`, `ChangeRoundStateMessage`, `EntitySubSystem.CmdSpawnLatePlayer`)
  — never simulated input, never the `#if UNITY_EDITOR`-only stub broadcasts on
  `ReadyPlayersSubSystem`/`RoundSubSystem` (those don't compile into a real built player/server).
  Instructions: `wait_connected`, `ready`, `start_round`, `wait_round <state>`, `embark`,
  `assert_embarked` (waits until this connection owns a spawned `Entity` — unlike `embark`,
  which only fires the spawn/reclaim request without confirming it landed), `reconnect`
  (client-only; redirects DefaultScene offline to `Empty.unity`, then re-joins via
  `ClientManager.StartConnection` with CLI `NetworkSettings` — see networking-session Pitfalls),
  `console <command line>` (routes through `CommandsController.ClientProcessCommand`),
  `wait_seconds <n>`, `disconnect`.
- `TestSignal.cs` — emits `"Test signal {signal} {payload}"` through the existing Serilog
  pipeline (new `Logs.Testing` category) as a deterministic readiness/completion vocabulary
  (`ServerReady`, `ClientConnected`, `RoundStateChanged`, `PlayerEmbarked`, `EmbarkVerified`,
  `ScriptComplete`, `ScriptFailed:<reason>`) — replaces `Thread.Sleep`/`GameObject.Find` polling
  with something a harness can wait on deterministically.
- `-testscript=` plumbed through `CommandLineArgs.cs` → `CommandLineArgsSubSystem.cs` →
  `ApplicationSettings.TestScriptPath`, same pattern as the existing `-skipintro` etc.
- `Assets/Settings/LogSettings.asset`: `UseCompactJsonFormatter` flipped `0` → `1`. It already had
  full code support in `LogManager.cs` but defaulted off, so file-sink logs were plain text —
  the harness's log-based signal detection needs the structured JSON. See the **Pitfall** this
  added to [networking-session.md](../../architecture/systems/networking-session.md).

### Orchestration (`Testing/multiplayer/`)
- `run_smoketest.sh <scenario> [client-count]` — launches one real dedicated-server process and
  N real client processes, all `-batchmode -nographics` (dedicated server is display-free;
  Linux *clients* still need `SDL_VIDEODRIVER=dummy` — see Pitfalls / process.sh),
- `lib/process.sh` — dynamic free-port allocation (no more hardcoded port); stages an isolated
  per-run tree of each build via hardlinks when possible (`cp -a --link` / `cp -al`, full copy
  fallback) so Logs/`Application.dataPath` stay isolated without duplicating player binaries;
  real-copies only `Config`/`Data`/`Logs` (writable). PID-tracked spawn/kill
  (`trap ... EXIT INT TERM`, never a process-name match — the old harness's
  `KillAllBuiltExecutables` could kill unrelated processes on a shared machine).
- `lib/logwait.sh` — polls the structured JSON logs for `Test signal` lines via `jq`; checks
  Unity's own `-logFile` output for uncaught-exception signatures (structured logs only capture
  what goes through the `Log` wrapper — crashes/NREs surface through Unity's own log, not
  Serilog), **known-bad LogWarning/prose patterns** (`tools/known_unity_bad.patterns` — e.g.
  FishNet SyncVar writes on a pure client), and the JSON logs for any `Error`/`Fatal`-level entry.
- `tools/triage_run.sh <run-id|path|latest>` — agent/human-facing summary of a `.runs/<id>/`
  directory: Test signal timeline, `ScriptFailed` payloads, JSON Error/Fatal, unity.log
  denylist hits (`known_unity_bad.patterns`), and exception hits classified against
  `tools/known_unity_noise.patterns` (headless Blitter/shader spam etc.). Does not dump full
  `unity.log`. Cursor skills:
  `.cursor/skills/multiplayer-smoke-e2e/SKILL.md` (build → smoke → triage → fix),
  `.cursor/skills/run-multiplayer-smoke/SKILL.md` (kick off `run_smoketest.sh`),
  `.cursor/skills/triage-multiplayer-smoke/SKILL.md` (summarize a run). The harness fail gate
  uses the **bad** denylist; the **noise** allowlist is triage-only so a `ScriptFailed` root
  cause is not buried under icon-gen stacks.
- `Tools/build_client_and_server.sh` — batchmode Unity build of both Linux binaries via
  `ClientAndServerBuildScript.BuildBothBatch`.
- `scenarios/basic-round{,-client}.txt` — connect, ready, start round (client-side, pre-seeded
  `Administrator` — see below), embark, one real `console playerlist` client↔server↔client RPC
  round trip, disconnect. Ports the intent of the deleted `ServerGameActions`/`ClientGameActions`
  PlayMode tests.
- `scenarios/late-join{,-client-0,-client-1}.txt` — client 0 starts the round and embarks
  immediately; client 1 connects immediately but doesn't ready/embark until the round is already
  `Ongoing` and client 0 has already embarked. Ports
  `KnownIssueReproduction/Issue1002_LateJoinFails_HostPerspective.cs`'s
  `ClientCanEmbarkAfterRoundStartWhenHostHasAlreadyEmbarked` case as a real two-process run.
- `scenarios/reconnect{,-client}.txt` — single client embarks, confirms it controls a spawned
  body (`assert_embarked`), disconnects, reconnects with the same ckey, and confirms it
  regained control of that same body rather than being left with no controllable entity while
  the old one sits ownerless in the world. Regression coverage for the disconnect/reconnect
  ownership fix in `PlayerSubSystem.ProcessPlayerDisconnect`/`ProcessAuthorizePlayer` and
  `EntitySubSystem.TryReclaimEntity` — see [player-control.md](../../architecture/systems/player-control.md).
- `scenarios/atmos-client-sync{,-client}.txt` — regression coverage for
  [2026-07_atmos-client-visualization-sync.md](../../architecture/2026-07_atmos-client-visualization-sync.md): the
  client embarks, runs `console atmosdebug heat 500` (new `AtmosDebugCommand` — headless
  equivalent of `AtmosDebugController`'s GUI buttons, since a `-batchmode` client can't click
  one) at its own tile to force a fresh dirty chunk, waits for the next atmos tick, then runs
  `console atmosclientstatus assert` (new `AtmosClientStatusCommand`, `CommandType.Offline`) —
  it emits a `Test signal AtmosClientSnapshotValid|Invalid` either way, and *throws* when
  `assert` is passed and the snapshot isn't valid, which `AutomationSubSystem`'s existing
  try/catch turns into a `ScriptFailed` the harness already treats as a failure. No new DSL
  instruction or `run_smoketest.sh` changes needed — this reuses the same "distinctive signal /
  exception-on-`console`" idioms every other check in the harness already relies on. Because
  Phase 1 has no late-join bootstrap, the forced heat-add is what makes this deterministic
  regardless of how long the server had been running before this client connected.
- Permissions: `run_smoketest.sh` clears staged `Data/ServerMeta/permissions.json` (Builds often
  ship one), then seeds `Config/permissions.txt` with each client's ckey as `Administrator` —
  a real dedicated server has no Editor session to grant this by hand, and `start_round` is
  server-side gated on it. Without clearing the envelope, Persistence wins and the txt is
  ignored (see [permissions.md](../../architecture/systems/permissions.md) Pitfalls).
- Station map fixtures: CI player builds have no CWD `Data/Tilemaps`. Without seeding,
  `TileSubSystem.Load()` logs "No station templates found", atmos starts with **0 cells**, and
  `atmos-client-sync` fails on `AtmosClientSnapshotInvalid`. `run_smoketest.sh` copies
  tracked `Builds/Game/Data/Tilemaps/` into the staged server `Data/Tilemaps/` (override with
  `SS3D_TILEMAP_FIXTURES`).

### CI
- `.github/workflows/develop-release.yml` — **manual** Windows-default prerelease + **nightly**
  full cut (Windows + Linux client/server → floating `develop-nightly`); TomNAS prefer;
  Linux via `BuildBothForCi`. See
  [2026-07_ci-develop-release-pipeline.md](../../architecture/2026-07_ci-develop-release-pipeline.md).
- `.github/workflows/multiplayer-smoke-test.yml` — opt-in smoke (`workflow_dispatch` or PR
  label `test:multiplayer`) plus nightly 04:00 UTC; same harness as the release smoke stage.
- `.github/workflows/editmodetestrunner.yml` — cheap EditMode on PR/`develop` push
  (TomNAS preferred, GitHub-hosted fallback).
- `.github/workflows/main.yml` — **removed**; Windows cuts use `develop-release.yml`.

### Removed (superseded)
- `Assets/Scripts/Tests/PlayMode/Framework/Helpers/LoadFileHelpers.cs` — `Thread.Sleep`-based
  readiness, Windows-only `user32.dll` window tiling, hardcoded port `1151`.
- `ServerGameActions.cs`, `ClientGameActions.cs`, `ClientLobbyActions.cs`,
  `ClientLateJoinActions.cs`, `InteractionPlayModeTests.cs`,
  `KnownIssueReproduction/Issue1002_LateJoinFails_HostPerspective.cs`,
  `KnownIssueReproduction/Issue0990_PropagatingPocketProblem_ClientPerspective.cs` — all depended
  on `LoadFileHelpers`/a real `NetworkType.Client` process launched from the Editor test-runner.
- `PlayModeTest.KillAllBuiltExecutables`/`ExecutableName`, the `NetworkType.Client` branch of
  `LoadAndSetInLobby`, and `ServerHelpers.CreateClients`/`SetWindowPositions`/
  `WaitUntilClientsLoaded` — the process-spawning parts of the old harness.
  `PlayModeTest`/`ServerHelpers` now only support single-process Host / in-Editor
  DedicatedServer roles, which is what the remaining `RoundLifecycle_*` tests (promoted from
  `KnownIssueReproduction/` to `PlayMode/RegressionTests/` on 2026-10-31) and
  `GeneralTests/Host/HostGameActions.cs` actually use.

## Load/soak extension (2026-11-01)

`Testing/multiplayer/run_loadtest.sh` (PR #91) stages one dedicated server and N headless clients,
waits for all to embark, then holds the round for a configurable duration. Example:
`./Testing/multiplayer/run_loadtest.sh --clients 8 --duration 300 --stagger 2`.
Defaults are 8 clients, 300 seconds and stagger 0. Logs and server metrics are preserved under
`Testing/multiplayer/.runs/load-*`. This is local load tooling; it does not close the deferred
24/7 soak-server work. Metrics and coverage limits: [networking-session §Tests](../../architecture/systems/networking-session.md#tests).

## Known gaps

- **Mouse/screen-space interaction regression coverage was not ported.**
  `InteractionPlayModeTests.PlayerCanDropAndPickUpItem_InteractionPipelineRegression` (deleted)
  drove drop/pickup via `camera.WorldToScreenPoint` + simulated mouse clicks against a rendered
  client — exactly the "drop interaction... not yet investigated" known issue from the headless
  dedicated-server effort. A `-batchmode -nographics` client has no camera/window to click
  against, so this class of bug has **no automated regression coverage** right now. Closing this
  gap needs either a non-headless client harness variant (real display, real OS input
  injection — a materially heavier lift) or a non-UI test hook for interactions specifically
  (the `console` instruction covers server-command-driven actions, not raycast/radial-menu-driven
  ones). Not attempted here.
- **Pocket/container round-trip regression not ported.**
  `KnownIssueReproduction/Issue0990_PropagatingPocketProblem_ClientPerspective.cs` (deleted;
  its `_HostPerspective` sibling stays, single-process) asserted pocket count stays consistent
  across a round stop/start cycle from a real client's perspective. Porting it needs a
  `stop_round` automation instruction (not yet added — only `start_round` exists) and a way to
  assert inventory/container state headlessly (no existing console command exposes pocket count;
  would need a new one or a new `AutomationSubSystem` instruction). Deferred.
- **Single build target.** The harness only builds/runs `StandaloneLinux64`, matching the
  dedicated-server effort's scope; no Windows client coverage.
- **`stage_build` hardlinks.** Default is `cp -a --link` / `cp -al` plus real copies of
  `Config`/`Data`/`Logs`. Falls back to a full copy (with a warning) if source and
  `.runs/` are on different filesystems — watch CI disk if that ever happens.
- **Headless unity.log noise.** `-batchmode -nographics` clients still emit graphics/icon and
  rare missing-script lines; server emits Dedicated Server Optimizations shader messages. Prefer
  source fixes (icon skip already mapped) over growing `known_unity_noise.patterns`. Harness
  still fails on any `Exception:` until an allowlist is wired into `logwait.sh` deliberately.
  Separately, `known_unity_bad.patterns` is a **hard-fail denylist** for non-exception LogWarning
  prose that must never appear (FishNet "Cannot complete operation as server when server is not
  active" — pure-client SyncVar writes). Do not move denylist entries into the noise allowlist.
- **Linux client SIGSEGV without `SDL_VIDEODRIVER`.** On headless hosts with no DISPLAY, Unity 6
  client players pick window backend `(null)` and crash in `PlayerMain`. `lib/process.sh`
  defaults `SDL_VIDEODRIVER=dummy`. Dedicated server builds do not need it.
- **Verification provenance:** the original implementation session used mock binaries. Later local Unity validation paths exist; neither those paths nor the configured CI schedule establish a fresh passing run. See Verification and [TECH_DEBT §1.10](../../architecture/TECH_DEBT.md#110-play-mode--multiplayer-verification-is-optional-in-practice).

## Verification

**Original implementation-session results (historical):**

Built and exercised the orchestration logic (`Testing/multiplayer/run_smoketest.sh` and its
`lib/` helpers) against mock "SS3D.x86_64" shell scripts standing in for the real Unity
binaries (this development environment has no Unity Editor installed, so a real build wasn't
possible here):

- End-to-end pass: 1-client `basic-round` and 2-client `late-join` scenarios both resolve
  per-client scripts, allocate a port, stage isolated build copies, clear ServerMeta permissions
  and seed `permissions.txt`, wait
  on `Test signal` lines, and exit 0.
- Negative control: injecting a `NullReferenceException` line into the mock server's Unity log
  makes the harness exit 1 and dump both processes' logs.
- Two concurrent `run_smoketest.sh` invocations against the same mock build both pass, each with
  its own allocated port and isolated log directory (no `LogServer.json` collision).
- PID-tracked cleanup: an unrelated background process survives a harness run; no harness
  process lingers afterward (verified via PID pattern anchored to the run's own staged path, not
  a global name match).
- `scenarios/reconnect{,-client}.txt` (added later, same technique): a mock binary that
  interprets the DSL opcodes and emits matching `Test signal` lines confirms
  `run_smoketest.sh reconnect` resolves the new scenario files (numbered-then-default fallback),
  stages/ports/permissions all still work, and the client log shows the expected
  `ClientConnected → RoundStateChanged Ongoing → PlayerEmbarked → EmbarkVerified →
  ClientConnected → EmbarkVerified → ScriptComplete` sequence around the `disconnect`/`reconnect`
  pair. This only proves the shell orchestration and DSL plumbing accept the new instructions —
  it does not exercise the real `AutomationSubSystem.Reconnect`/`IsEmbarked` C# or the
  `PlayerSubSystem`/`EntitySubSystem` server logic they drive, which still needs a real build
  (see below).

**Original session limitation: no real `AutomationSubSystem`/`ServerBuildScript`/`ClientBuildScript` build**
(this session had no Unity Editor to build with). Next step for whoever picks this up: run
`SS3D/Build/Dedicated Server (Linux)` and `SS3D/Build/Client (Linux)` from the Editor, then
`./Testing/multiplayer/run_smoketest.sh basic-round` locally, `./Testing/multiplayer/run_smoketest.sh
reconnect` (new — also needs its first real-build run), and a `workflow_dispatch` run of
`multiplayer-smoke-test.yml` in CI, before relying on the nightly/labeled-PR triggers as
a real merge gate.

**Current verification entry point (2026-11-01):** with the Editor open,
`Testing/ui/validate.sh --full` drives builds and the same four smoke scenarios as CI. It also
runs PlayMode and graphical pilots. The separate load runner is manual; CI currently invokes
smoke only. This documentation refresh inspected source/workflows and did not run Unity or query
fresh CI results. Last recorded CI health and remaining merge-gate gaps: [TECH_DEBT §1.10](../../architecture/TECH_DEBT.md#110-play-mode--multiplayer-verification-is-optional-in-practice).

## Related docs

- [2026-07_headless-dedicated-server.md](2026-07_headless-dedicated-server.md) — the effort and
  testing gap this closes
- [2026-07_agent-first-composition.md](../../architecture/2026-07_agent-first-composition.md) — why
  `AutomationSubSystem` is code-bootstrapped instead of living in `Boot.unity`
- [networking-session.md](../../architecture/systems/networking-session.md), [permissions.md](../../architecture/systems/permissions.md),
  [rounds-lobby.md](../../architecture/systems/rounds-lobby.md), [ingame-console.md](../../architecture/systems/ingame-console.md)
