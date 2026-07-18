> Code paths: Assets/Scripts/SS3D/Networking/, Assets/Scripts/SS3D/Editor/ServerBuildScript.cs, Assets/Scripts/SS3D/Editor/ClientBuildScript.cs, Assets/Scripts/SS3D/Systems/Testing/, Testing/multiplayer/
> Entry points: NetworkSessionSubSystem, SS3D.Systems.Testing.AutomationSubSystem
> Status: partial
> Verified: 21e83b853 — 2026-07-18

# Networking (session)

## Overview

FishNet session management — host/join, network type and port settings. Distinct from tile AOI helpers under `Systems/Networking/`. Includes a genuine headless dedicated-server build (`UNITY_SERVER` subtarget), not just a client build launched with `-serveronly` — see [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md). A real multi-process test harness now exercises this end to end — see [2026-07_multiplayer-test-harness](../2026-07_multiplayer-test-harness.md).

## Start here

- `Assets/Scripts/SS3D/Networking/NetworkSessionSubSystem.cs` — session host/join subsystem; on `UNITY_SERVER` also self-starts on `ApplicationInitializing` since `IntroUIHelper` (the only other caller) lives in a scene the server skips
- `Assets/Scripts/SS3D/Editor/ServerBuildScript.cs` — `SS3D/Build/Dedicated Server (Linux)` menu item and CI build method
- `Assets/Scripts/SS3D/Editor/ClientBuildScript.cs` — `SS3D/Build/Client (Linux)` menu item; now also `-buildMethod`/`-customBuildPath`-invocable from CI, mirroring `ServerBuildScript`
- `Assets/Scripts/SS3D/Editor/ClientAndServerBuildScript.cs` — `SS3D/Build/Client + Dedicated Server (Linux)` runs both in sequence for local smoke-test rebuilds
- `Assets/Scripts/SS3D/Systems/Testing/AutomationSubSystem.cs` — self-bootstrapping (no scene edit), drives a headless process through a `-testscript=` script using the same client→server broadcast/RPC APIs the lobby UI calls; no-op unless that flag is set
- `Testing/multiplayer/run_smoketest.sh` — the actual multiplayer test harness: launches a real server + N real client processes and asserts on their logs; see [2026-07_multiplayer-test-harness](../2026-07_multiplayer-test-harness.md)
- `Builds/start_ss3d_server.sh`, `Builds/start_ss3d_client.sh` — local launch scripts
- `Dockerfile`, `docker-compose.yml` — containerized server deployment
- `.github/workflows/main.yml` `build-server` job — cheap boot-and-grep smoke test, builds + uploads the release server artifact
- `.github/workflows/multiplayer-smoke-test.yml` — the real multi-process CI gate (`push: develop`, `workflow_dispatch`, label-gated `pull_request`)

## Extension points

- Boot.unity's `ServerManager._startOnHeadless` must stay `0` — FishNet's own headless
  auto-start races with `NetworkSessionSubSystem.StartNetworkSession`, see
  [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md).
- New `AutomationSubSystem` instructions (the line-DSL used by `Testing/multiplayer/scenarios/*.txt`) go in `AutomationScript.cs`/`AutomationSubSystem.RunInstruction` - keep using real client→server broadcast/RPC calls, never the `#if UNITY_EDITOR`-only stub broadcasts on `ReadyPlayersSubSystem`/`RoundSubSystem` (those don't compile into a real built player/server).

## Pitfalls

- **`UseCompactJsonFormatter` / JSON file logs.** Player builds always write compact JSON (`.json`) regardless of the asset — the multiplayer harness (`Testing/multiplayer/lib/logwait.sh`) requires `LogServer.json`/`LogClient<ckey>.json`. The LogSettings toggle only affects Editor Play Mode. A `false` C# default plus Coimbra `OnValidate` dirtying the asset used to bake plain-text `.log` into Builds and make every scenario time out; the field default is now `true` and player builds ignore the toggle.
- **Log file path is chosen in `CommandLineArgsSubSystem` after CLI parsing, not from a peer `ApplicationPreInitializing` listener.** `NetworkType`/`Ckey` come from `-serveronly`/`-ip=`/`-ckey=`; initializing Serilog from `NetworkSessionSubSystem` on the same event raced and could open `LogHost.json` while the harness waited on `LogServer.json`. Path helper: `NetworkSessionSubSystem.GetFileLogName`.

## Depends on / Used by

- **Used by:** All `NetworkSubSystem` domains, [tile](tile.md) AOI

## Related docs

- [INDEX.md](../INDEX.md)
- [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md) — headless
  server build, runtime guards, known issues, testing-harness gap
- [2026-07_multiplayer-test-harness](../2026-07_multiplayer-test-harness.md) — the harness that closes that gap
