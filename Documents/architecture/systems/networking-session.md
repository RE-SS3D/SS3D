> Code paths: Assets/Scripts/SS3D/Networking/, Assets/Scripts/SS3D/Editor/ServerBuildScript.cs, Assets/Scripts/SS3D/Editor/ClientBuildScript.cs, Assets/Scripts/SS3D/Systems/Testing/, Testing/multiplayer/
> Entry points: NetworkSessionSubSystem, SS3D.Systems.Testing.AutomationSubSystem
> Status: partial
> Verified: c74429e16 — 2026-07-18

# Networking (session)

## Overview

FishNet session management — host/join, network type and port settings. Distinct from tile AOI helpers under `Systems/Networking/`. Includes a genuine headless dedicated-server build (`UNITY_SERVER` subtarget), not just a client build launched with `-serveronly` — see [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md). A real multi-process test harness now exercises this end to end — see [2026-07_multiplayer-test-harness](../2026-07_multiplayer-test-harness.md). Manual CI prerelease (EditMode/smoke opt-in): [2026-07_ci-develop-release-pipeline](../2026-07_ci-develop-release-pipeline.md).

## Start here

- `Assets/Scripts/SS3D/Networking/NetworkSessionSubSystem.cs` — session host/join subsystem; on `UNITY_SERVER` also self-starts on `ApplicationInitializing` since `IntroUIHelper` (the only other caller) lives in a scene the server skips
- `Assets/Scripts/SS3D/Editor/ServerBuildScript.cs` — `SS3D/Build/Dedicated Server (Linux)` menu item and CI build method
- `Assets/Scripts/SS3D/Editor/ClientBuildScript.cs` — `SS3D/Build/Client (Linux)` menu item; now also `-buildMethod`/`-customBuildPath`-invocable from CI, mirroring `ServerBuildScript`
- `Assets/Scripts/SS3D/Editor/ClientAndServerBuildScript.cs` — `SS3D/Build/Client + Dedicated Server (Linux)` menu item; batchmode entry `BuildBothBatch` used by `Tools/build_client_and_server.sh`
- `Assets/Scripts/SS3D/Systems/Testing/AutomationSubSystem.cs` — self-bootstrapping (no scene edit), drives a headless process through a `-testscript=` script using the same client→server broadcast/RPC APIs the lobby UI calls; no-op unless that flag is set. Client `wait_connected` also waits until `PlayerSubSystem` exists (FishNet connects before Game finishes loading additively).
- `Testing/multiplayer/run_smoketest.sh` — the actual multiplayer test harness: launches a real server + N real client processes and asserts on their logs; see [2026-07_multiplayer-test-harness](../2026-07_multiplayer-test-harness.md)
- `Builds/start_ss3d_server.sh`, `Builds/start_ss3d_client.sh`, `Builds/Start_SS3D_*.bat` — local / Windows-prerelease launch scripts
- `Dockerfile`, `docker-compose.yml` — containerized server deployment
- `.github/workflows/develop-release.yml` — manual Windows+bats prerelease by default; Linux/EditMode/smoke opt-in ([2026-07_ci-develop-release-pipeline](../2026-07_ci-develop-release-pipeline.md))
- `.github/workflows/multiplayer-smoke-test.yml` — opt-in multi-process smoke (`workflow_dispatch` / PR label `test:multiplayer`)
- `.github/workflows/editmodetestrunner.yml` — cheap EditMode on PR/`develop` push

## Extension points

- Boot.unity's `ServerManager._startOnHeadless` must stay `0` — FishNet's own headless
  auto-start races with `NetworkSessionSubSystem.StartNetworkSession`, see
  [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md).
- New `AutomationSubSystem` instructions (the line-DSL used by `Testing/multiplayer/scenarios/*.txt`) go in `AutomationScript.cs`/`AutomationSubSystem.RunInstruction` - keep using real client→server broadcast/RPC calls, never the `#if UNITY_EDITOR`-only stub broadcasts on `ReadyPlayersSubSystem`/`RoundSubSystem` (those don't compile into a real built player/server).

## Pitfalls

- **`UseCompactJsonFormatter` / JSON file logs.** Player builds always write compact JSON (`.json`) regardless of the asset — the multiplayer harness (`Testing/multiplayer/lib/logwait.sh`) requires `LogServer.json`/`LogClient<ckey>.json`. The LogSettings toggle only affects Editor Play Mode. A `false` C# default plus Coimbra `OnValidate` dirtying the asset used to bake plain-text `.log` into Builds and make every scenario time out; the field default is now `true` and player builds ignore the toggle.
- **Log file path is chosen in `CommandLineArgsSubSystem` after CLI parsing, not from a peer `ApplicationPreInitializing` listener.** `NetworkType`/`Ckey` come from `-serveronly`/`-ip=`/`-ckey=`; initializing Serilog from `NetworkSessionSubSystem` on the same event raced and could open `LogHost.json` while the harness waited on `LogServer.json`. Path helper: `NetworkSessionSubSystem.GetFileLogName`.
- **Client `wait_connected` must wait for an authorized Player, not just `PlayerSubSystem`.** FishNet reports Connected before Game loads additively; `UnauthorizedPlayer` auth (and server `Player` spawn with ckey) finishes later still. Calling `ready`/`start_round` as soon as `PlayerSubSystem` exists sends broadcasts while `GetCkey(conn)` is still null → server logs `Ckey null while trying to get user role` / `User null doesn't have Administrator` and `wait_round Ongoing` times out. `AutomationSubSystem` waits until `PlayerSubSystem.GetCkey(LocalConnection)` is non-empty.
- **Harness admin seed needs a clear ServerMeta permissions envelope.** Seeding only `Config/permissions.txt` is ignored when staged `Data/ServerMeta/permissions.json` exists. `run_smoketest.sh` removes that file before seeding; see [permissions.md](permissions.md).
- **Server must not `disconnect` immediately after `wait_round Ongoing`.** The dedicated-server scenario used to tear down as soon as Ongoing flipped locally; the client's SyncVar often never received Ongoing (~80ms race) → client `wait_round` timeout while server already `ScriptComplete`. Keep the server up (`wait_seconds`) until clients finish embark/console/disconnect.
- **After `ScriptComplete`, hard-exit — do not `Application.Quit`.** Quit still unloads scenes and re-enters `ApplicationInitializing`, so NetworkSession re-joins and (without a guard) automation re-runs → harness Error/Fatal + RoleSubSystem duplicate-key. `AutomationSubSystem` runs the script once and `Environment.Exit(0)` after emitting the final signal.
- **`TileResourceLoader` / `Item.GenerateIcon` preview cameras break `-nographics` clients.** `RuntimePreviewGenerator` recreates URP on NullGfxDevice → GraphicsBuffer/Blitter spam that fails the harness exception check. Skip icon generation when `Application.isBatchMode` or `GraphicsDeviceType.Null` (dedicated server already skipped via `UNITY_SERVER`).
- **Destroyed `BasicElectricDevice` throws on `TileObject` during server teardown.** Accessing `.gameObject` on a destroyed component NREs inside electricity FixedUpdate after `StopConnection`; `TileObject` now returns null when `this` is Unity-destroyed so area/APC lookups bail cleanly.

## Depends on / Used by

- **Used by:** All `NetworkSubSystem` domains, [tile](tile.md) AOI

## Related docs

- [INDEX.md](../INDEX.md)
- [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md) — headless
  server build, runtime guards, known issues, testing-harness gap
- [2026-07_multiplayer-test-harness](../2026-07_multiplayer-test-harness.md) — the harness that closes that gap
- [2026-07_ci-develop-release-pipeline](../2026-07_ci-develop-release-pipeline.md) — manual CI prerelease path (EditMode/smoke opt-in)
