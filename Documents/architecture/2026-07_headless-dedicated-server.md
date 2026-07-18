> Implements: none (infrastructure — server build tooling, not a gameplay domain)
> Touches systems: networking-session, scene-management, application, core-subsystems, tile, health (roles), machine-interface, audio, electricity
> Status: shipped (partial — see Known issues)

# Headless dedicated server (Jul 2026)

## Goal

Replace the fork's fake "dedicated server" — a normal Standalone client build launched
with `-serveronly`, still loading Intro/Launcher and carrying full rendering/audio — with
a genuine Unity Server-subtarget build (`UNITY_SERVER`), buildable in one click, deployable
headless on Linux, and validated by actually running a client against it end to end.

## Shipped

### Build pipeline
- `Assets/Scripts/SS3D/Editor/ServerBuildScript.cs` — forces `StandaloneBuildSubtarget.Server`,
  buildable from CI (`-buildMethod`) or the Editor menu `SS3D/Build/Dedicated Server (Linux)`.
- `Assets/Scripts/SS3D/Editor/ClientBuildScript.cs` — `SS3D/Build/Client (Linux)`, sits next to
  the server menu item so both builds are one click each (toggling "Server Build" by hand in
  Build Settings is easy to forget and silently produces a client build that never compiles
  with `UNITY_SERVER`).
- `.github/workflows/develop-release.yml` / `multiplayer-smoke-test.yml` — build the dedicated
  server (and client), then exercise it via the multiplayer harness rather than a
  boot-and-grep-only job.
- `Builds/start_ss3d_server.sh`, `Builds/start_ss3d_client.sh` — local launch scripts.
- `Dockerfile`, `docker-compose.yml` — containerized deployment.

### Runtime guards (`#if UNITY_SERVER` / `#if !UNITY_SERVER`)
- `CameraSubSystem`, `PlayerCameraSubSystem` — disable the scene's one live camera/audio
  listener and skip DOTween FOV tweening.
- `NetworkSettings.ResetOnBuiltApplication` — defaults to `NetworkType.DedicatedServer`
  instead of `Client` on a Server-subtarget build, so the process boots correctly even with
  no CLI args.
- `SceneSubSystem.LoadMainScene` — skips Intro/Launcher, loads `Game` directly.
- `NetworkSessionSubSystem` — starts its own network session on `ApplicationInitializing`
  (the only other caller, `IntroUIHelper`, lives in the now-skipped Intro scene).
- `PlacedTileObject.Create`, `PlacedItemObject.Create`, `DoorAdjacencyConnector.CreateWallCap`
  — disable `Renderer`/`Light`/`ParticleSystem` on every spawned tile/item/wall-cap via the new
  `ServerVisualsUtility.DisableRenderingComponents`. "Dedicated Server Optimizations" strips
  shaders from the build, so any enabled renderer left touching the render pipeline spams
  "Trying to access a shader..." — this was the dominant source of log growth (14MB/minute).
- `TileResourceLoader.LoadAssets`, `Item.GenerateIcon` — skip UI icon generation
  (`RuntimePreviewGenerator` renders a camera to produce a preview texture; with shaders
  stripped this crashed URP's pipeline init).
- `EmissiveMaterialController`, `ConstructionHologramManager` — stop per-frame
  material/mouse-position updates that have no meaning without a client camera.
- `MachineInterfaceSubSystem.Open` — skipped on the server. Every machine controller's
  "open UI" RPC is `[TargetRpc(RunLocally = true)]` (so a host player sees the UI without a
  network round trip); on a dedicated server that also ran the client-only UI Toolkit code
  with no shaders available and crashed on every machine interaction.

### Scene/data fixes
- `Assets/Content/Systems/Lobby/StaticWorldObjects.prefab` — removed two orphaned components
  (a dead Post-Processing Stack v2 setup, a deleted "soda can spawner" script) that logged
  "referenced script is missing" on every scene load.
- Boot.unity `ServerManager._startOnHeadless` set to `0` — FishNet's own headless auto-start
  raced with `NetworkSessionSubSystem`, binding the transport's scene-configured port (4538)
  before our code ran on its configured port, making every subsequent `StartConnection` call
  fail as "already started." Manifested as a false "port already in use."

### General robustness fixes (surfaced by, but not specific to, headless testing)
Several of these are pre-existing bugs in code paths that had never actually executed before
a dedicated server + real remote client made the server's `[Server]`-authoritative logic run
for the first time (previously only host mode — server and client in one process — was
exercised, which masks a different set of ordering/null bugs):
- `RoleSubSystem.RemovePlayerFromCounters` — wrapped a `Dictionary.FirstOrDefault()` struct
  result in a nullable (never actually null), then later choked on a null `Player` key from
  FishNet's `SyncDictionary` remove callback. Rewritten with `TryGetValue` + null guard.
- `GamemodeSubSystem.SendObjectiveToClients` — NRE on an objective with an unresolved
  assignee ckey.
- `BasicElectricDevice.OnDestroyed`, `SmesBattery.OnDestroyed` — null-derefed
  `SubSystems.Get<ElectricitySubSystem>()` during teardown ordering; `SubSystems.Get<T>()`
  also now suppresses its "not found" log once `Application.quitting` has fired, since
  teardown order is inherently arbitrary.
- `NetworkSessionSubSystem` — logs an error if `Transport.StartConnection()` returns `false`
  instead of failing completely silently (it previously logged "Hosting a new server..."
  unconditionally *before* knowing whether the connection actually started).
- `MachineInterfaceHost.EnsureRuntimeAssets` — widened to check every template/style field,
  not just vending/gas-pump/tokens. `_ventTemplate`, `_scrubberTemplate`, `_airAlarmTemplate`
  were never assigned on the `Game.unity` scene host (only auto-populated by an Editor-only
  `AssetDatabase` fallback, so they were silently null in **every** compiled build — client
  included, not just the server). Scene data fixed by assigning those fields in the Editor.

## Known issues (found during manual dogfooding, not yet root-caused)

Found by playing a real session against the dedicated server with a separate client build.
This list is almost certainly incomplete — see Testing gap below for why.

- **Selection outline not working** against a real remote client (client-authored, only
  ever manually tested in host mode before). Not yet investigated — see
  [selection.md](systems/selection.md).
- **Drop interaction** behaves incorrectly against a real remote client. Not yet
  investigated — see [inventory.md](systems/inventory.md).
- Likely more: this fork has never had a real client ↔ dedicated-server session run this
  thoroughly before. Anything that only worked because host mode collapses server and
  client into one process (shared memory, no real RTT, no real desync window) is a
  candidate.

## Testing gap: no multiplayer test harness

Every bug in this effort — the network session race, the `[Server]`-path NREs, the
selection/drop issues above — was found by hand: build server, build client, launch both,
play, read two log files, repeat. EditMode tests can't catch any of it (no FishNet transport,
no `UNITY_SERVER` define, no cross-process timing), and the existing PlayMode tests run
single-process (host mode), which is exactly the mode that was masking these bugs.

What's missing is a way to exercise a real server + real client(s) as separate processes (or
at minimum separate `NetworkManager` instances with an actual loopback transport) and assert
on outcomes automatically. Two directions worth evaluating, not mutually exclusive:

1. **In-process PlayMode multiplayer test**: boot two FishNet `NetworkManager` instances in
   one Test Runner process — one `ServerManager.StartConnection`, one `ClientManager.StartConnection`
   over Tugboat loopback — drive scripted interactions against the client, assert server-side
   state and absence of logged exceptions (`LogAssert`/`Application.logMessageReceived`). Fast,
   runs in CI, but still single-process so it won't catch everything a real second process would
   (it's a step up from host mode, not equivalent to true separate processes).
2. **Built-binary smoke test, extended**: `multiplayer-smoke-test.yml` / `develop-release.yml`
   (`run_smoke`) already build server+client and run the multi-process harness. Extend that
   path further if you need scripted interactions beyond connect/round/late-join — fail the
   job on any `[ERR]` / uncaught-exception line in either log.

Recommendation: prefer extending the existing harness (`Testing/multiplayer/`) over a
boot-and-grep-only job. An in-process PlayMode dual-NetworkManager path (1) is more valuable
long-term (faster iteration, no real network needed) but is a bigger lift and deserves its
own plan.

## Deferred / out of scope

- Fixing selection outline and drop interaction (tracked above as known issues, not
  root-caused).
- Building either multiplayer test harness described above.
- FishNet code stripping (Pro-only feature, disabled in the vendored free copy;
  `CameraSubSystem` etc. cover the actual client-only-instantiation surface instead).
- Windows dedicated server build (Linux-only per this effort's scope).
- Publishing the server build as a release artifact — superseded by
  [2026-07_ci-develop-release-pipeline.md](2026-07_ci-develop-release-pipeline.md)
  (manual Windows+bats prerelease after EditMode + Linux multiplayer smoke; Linux zips secondary).

## Success criteria

- [x] `UNITY_SERVER` build target buildable from CI and from the Editor in one click
- [x] Server boots headless, loads `Game` directly, no Intro/Launcher
- [x] Log stays flat during normal operation (no per-frame or per-instantiation shader spam)
- [x] A separately-built client can connect, join a round, and interact with machines,
      lockers, items, and doors without crashing the server
- [x] Round start/end and player disconnect handled without server-side exceptions
- [ ] Selection outline and drop interaction work correctly against a real client (deferred)
- [x] Automated multiplayer test coverage exists — see
      [2026-07_multiplayer-test-harness.md](2026-07_multiplayer-test-harness.md) (partial: mouse/
      screen-space interaction and pocket/container round-trip regressions not yet covered)
