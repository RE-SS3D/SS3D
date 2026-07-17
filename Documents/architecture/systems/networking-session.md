> Code paths: Assets/Scripts/SS3D/Networking/, Assets/Scripts/SS3D/Editor/ServerBuildScript.cs, Assets/Scripts/SS3D/Editor/ClientBuildScript.cs, Assets/Scripts/SS3D/Systems/Testing/, Testing/multiplayer/
> Entry points: NetworkSessionSubSystem, SS3D.Systems.Testing.AutomationSubSystem
> Status: partial
> Verified: b7b7dcf3 — 2026-07-17

# Networking (session)

## Overview

FishNet session management — host/join, network type and port settings. Distinct from tile AOI helpers under `Systems/Networking/`. Includes a genuine headless dedicated-server build (`UNITY_SERVER` subtarget), not just a client build launched with `-serveronly` — see [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md). A real multi-process test harness now exercises this end to end — see [2026-07_multiplayer-test-harness](../2026-07_multiplayer-test-harness.md).

## Start here

- `Assets/Scripts/SS3D/Networking/NetworkSessionSubSystem.cs` — session host/join subsystem; on `UNITY_SERVER` also self-starts on `ApplicationInitializing` since `IntroUIHelper` (the only other caller) lives in a scene the server skips
- `Assets/Scripts/SS3D/Editor/ServerBuildScript.cs` — `SS3D/Build/Dedicated Server (Linux)` menu item and CI build method
- `Assets/Scripts/SS3D/Editor/ClientBuildScript.cs` — `SS3D/Build/Client (Linux)` menu item; now also `-buildMethod`/`-customBuildPath`-invocable from CI, mirroring `ServerBuildScript`
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

- **`Assets/Settings/LogSettings.asset`'s `UseCompactJsonFormatter` defaulted to `0`** (plain-text file logs), even though `LogManager.cs` has full support for compact JSON file output gated behind it. Nothing errors either way - the Serilog file sink silently writes `.log` text instead of `.json`. The multiplayer harness's structured-log signal detection (`Testing/multiplayer/lib/logwait.sh`, `jq` over `LogServer.json`/`LogClient<ckey>.json`) requires this flag on; it's now set to `1`. If it ever gets flipped back off, the harness's `wait_for_signal`/`check_for_json_errors` will find no file at the expected `.json` path and every scenario will time out with no obvious cause in the harness's own code.

## Depends on / Used by

- **Used by:** All `NetworkSubSystem` domains, [tile](tile.md) AOI

## Related docs

- [INDEX.md](../INDEX.md)
- [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md) — headless
  server build, runtime guards, known issues, testing-harness gap
- [2026-07_multiplayer-test-harness](../2026-07_multiplayer-test-harness.md) — the harness that closes that gap
