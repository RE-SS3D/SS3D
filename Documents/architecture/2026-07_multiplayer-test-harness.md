> Implements: Documents/architecture/2026-07_headless-dedicated-server.md#testing-gap-no-multiplayer-test-harness
> Touches systems: networking-session, rounds-lobby, permissions, ingame-console, logging
> Status: shipped (partial — see Known gaps)

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
- `Assets/Scripts/SS3D/Systems/Testing/AutomationSubSystem.cs` — self-bootstraps via
  `[RuntimeInitializeOnLoadMethod]` (same pattern as `ScreenEffectsSubSystem`; no `Boot.unity`
  edit, per [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md)). No-op
  unless the new `-testscript=<path>` CLI arg is set. Drives a headless process through a small
  line-DSL script (`AutomationScript.cs`) using the same client→server broadcast/RPC APIs the
  lobby UI calls (`LobbyReadyView`, `ChangeRoundStateView`, `EntitySubSystem.CmdSpawnLatePlayer`)
  — never simulated input, never the `#if UNITY_EDITOR`-only stub broadcasts on
  `ReadyPlayersSubSystem`/`RoundSubSystem` (those don't compile into a real built player/server).
  Instructions: `wait_connected`, `ready`, `start_round`, `wait_round <state>`, `embark`,
  `console <command line>` (routes through `CommandsController.ClientProcessCommand`),
  `wait_seconds <n>`, `disconnect`.
- `TestSignal.cs` — emits `"Test signal {signal} {payload}"` through the existing Serilog
  pipeline (new `Logs.Testing` category) as a deterministic readiness/completion vocabulary
  (`ServerReady`, `ClientConnected`, `RoundStateChanged`, `PlayerEmbarked`, `ScriptComplete`,
  `ScriptFailed:<reason>`) — replaces `Thread.Sleep`/`GameObject.Find` polling with something a
  harness can wait on deterministically.
- `-testscript=` plumbed through `CommandLineArgs.cs` → `CommandLineArgsSubSystem.cs` →
  `ApplicationSettings.TestScriptPath`, same pattern as the existing `-skipintro` etc.
- `Assets/Settings/LogSettings.asset`: `UseCompactJsonFormatter` flipped `0` → `1`. It already had
  full code support in `LogManager.cs` but defaulted off, so file-sink logs were plain text —
  the harness's log-based signal detection needs the structured JSON. See the **Pitfall** this
  added to [networking-session.md](systems/networking-session.md).

### Orchestration (`Testing/multiplayer/`)
- `run_smoketest.sh <scenario> [client-count]` — launches one real dedicated-server process and
  N real client processes, all `-batchmode -nographics` (no display dependency, runs unmodified
  on headless Linux CI or a dev machine).
- `lib/process.sh` — dynamic free-port allocation (no more hardcoded port); stages an isolated
  per-run tree of each build via hardlinks when possible (`cp -a --link` / `cp -al`, full copy
  fallback) so Logs/`Application.dataPath` stay isolated without duplicating player binaries;
  real-copies only `Config`/`Data`/`Logs` (writable). PID-tracked spawn/kill
  (`trap ... EXIT INT TERM`, never a process-name match — the old harness's
  `KillAllBuiltExecutables` could kill unrelated processes on a shared machine).
- `lib/logwait.sh` — polls the structured JSON logs for `Test signal` lines via `jq`; checks
  Unity's own `-logFile` output for uncaught-exception signatures (structured logs only capture
  what goes through the `Log` wrapper — crashes/NREs surface through Unity's own log, not
  Serilog) and the JSON logs for any `Error`/`Fatal`-level entry.
- `tools/triage_run.sh <run-id|path|latest>` — agent/human-facing summary of a `.runs/<id>/`
  directory: Test signal timeline, `ScriptFailed` payloads, JSON Error/Fatal, and unity.log
  exception hits classified against `tools/known_unity_noise.patterns` (headless Blitter/shader
  spam etc.). Does not dump full `unity.log`. Cursor skills:
  `.cursor/skills/multiplayer-smoke-e2e/SKILL.md` (build → smoke → triage → fix),
  `.cursor/skills/run-multiplayer-smoke/SKILL.md` (kick off `run_smoketest.sh`),
  `.cursor/skills/triage-multiplayer-smoke/SKILL.md` (summarize a run). The harness fail gate
  does **not** yet use the noise allowlist — triage reports noise separately so a
  `ScriptFailed` root cause is not buried under icon-gen stacks.
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
- Permissions: `run_smoketest.sh` clears staged `Data/ServerMeta/permissions.json` (Builds often
  ship one), then seeds `Config/permissions.txt` with each client's ckey as `Administrator` —
  a real dedicated server has no Editor session to grant this by hand, and `start_round` is
  server-side gated on it. Without clearing the envelope, Persistence wins and the txt is
  ignored (see [permissions.md](systems/permissions.md) Pitfalls).

### CI
- `.github/workflows/develop-release.yml` — **manual** gated path: EditMode → Linux
  server+client builds (separate `buildsPath` dirs, `versioning: None`) → `basic-round` +
  `late-join 2` → Windows client zip with `Builds/Start_SS3D_*.bat` → GitHub prerelease.
  See [2026-07_ci-develop-release-pipeline.md](2026-07_ci-develop-release-pipeline.md).
- `.github/workflows/multiplayer-smoke-test.yml` — opt-in smoke only (`workflow_dispatch` or PR
  label `test:multiplayer`); no longer runs on every `develop` push. Same build scripts and
  harness as the release workflow’s smoke stage.
- `.github/workflows/editmodetestrunner.yml` — cheap EditMode on PR/`develop` push (unchanged).
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
  DedicatedServer roles, which is what the remaining `KnownIssueReproduction/RoundLifecycle_*`
  and `GeneralTests/Host/HostGameActions.cs` tests actually use.

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
- **Not yet verified against a real Unity build in this environment** — see Verification below.

## Verification

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

**Not yet verified: a real `AutomationSubSystem`/`ServerBuildScript`/`ClientBuildScript` build**
(this session had no Unity Editor to build with). Next step for whoever picks this up: run
`SS3D/Build/Dedicated Server (Linux)` and `SS3D/Build/Client (Linux)` from the Editor, then
`./Testing/multiplayer/run_smoketest.sh basic-round` locally, and a `workflow_dispatch` run of
`multiplayer-smoke-test.yml` in CI, before relying on the `push: develop`/labeled-PR triggers as
a real merge gate.

## Related docs

- [2026-07_headless-dedicated-server.md](2026-07_headless-dedicated-server.md) — the effort and
  testing gap this closes
- [2026-07_agent-first-composition.md](2026-07_agent-first-composition.md) — why
  `AutomationSubSystem` self-bootstraps instead of living in `Boot.unity`
- [networking-session.md](systems/networking-session.md), [permissions.md](systems/permissions.md),
  [rounds-lobby.md](systems/rounds-lobby.md), [ingame-console.md](systems/ingame-console.md)
