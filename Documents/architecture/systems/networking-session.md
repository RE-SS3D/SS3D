> Code paths: Assets/Scripts/SS3D/Networking/, Assets/Scripts/SS3D/Editor/ServerBuildScript.cs, Assets/Scripts/SS3D/Editor/ClientBuildScript.cs
> Entry points: NetworkSessionSubSystem
> Status: partial

# Networking (session)

## Overview

FishNet session management — host/join, network type and port settings. Distinct from tile AOI helpers under `Systems/Networking/`. Includes a genuine headless dedicated-server build (`UNITY_SERVER` subtarget), not just a client build launched with `-serveronly` — see [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md).

## Start here

- `Assets/Scripts/SS3D/Networking/NetworkSessionSubSystem.cs` — session host/join subsystem; on `UNITY_SERVER` also self-starts on `ApplicationInitializing` since `IntroUIHelper` (the only other caller) lives in a scene the server skips
- `Assets/Scripts/SS3D/Editor/ServerBuildScript.cs` — `SS3D/Build/Dedicated Server (Linux)` menu item and CI build method
- `Assets/Scripts/SS3D/Editor/ClientBuildScript.cs` — `SS3D/Build/Client (Linux)` menu item
- `Builds/start_ss3d_server.sh`, `Builds/start_ss3d_client.sh` — local launch scripts
- `Dockerfile`, `docker-compose.yml` — containerized server deployment
- `.github/workflows/main.yml` `build-server` job — builds + boot smoke-tests the server binary in CI

## Extension points

- Boot.unity's `ServerManager._startOnHeadless` must stay `0` — FishNet's own headless
  auto-start races with `NetworkSessionSubSystem.StartNetworkSession`, see
  [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md).

## Depends on / Used by

- **Used by:** All `NetworkSubSystem` domains, [tile](tile.md) AOI

## Related docs

- [INDEX.md](../INDEX.md)
- [2026-07_headless-dedicated-server](../2026-07_headless-dedicated-server.md) — headless
  server build, runtime guards, known issues, testing-harness gap
