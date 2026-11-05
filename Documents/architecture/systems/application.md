> Code paths: Assets/Scripts/SS3D/Application/, Assets/Scripts/SS3D/Systems/Bootstrap/
> Entry points: ApplicationInitializerSubSystem, SystemsBootstrap, ApplicationSettings
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Application

## Overview

App bootstrap and startup initialization. `ApplicationInitializerSubSystem` is DDOL via `SystemsBootstrap` (created last so listeners Awake first). Its `OnStart` raises `ApplicationPreInitializing` (command-line args applied), `ApplicationInitializing` (`SceneSubSystem` loads the first scene), optional Discord init, then `ApplicationInitialized`. Boot/Game are thin launch pads ([agent-first composition](../2026-07_agent-first-composition.md) follow-on (a) shipped).

`ApplicationSettings` (Coimbra project settings, `SS3D/Core`) holds Discord, SkipIntro, ForceLauncher, the automation `TestScriptPath` and the editor-only `EditorStationTemplate`; built executables reset them via `ResetOnBuiltApplication` before command-line args apply. `MultiplayerPlayModePlayer` says which MPPM player this editor is and which `smoke:` script its tag asks for.

## Start here

- `Assets/Scripts/SS3D/Application/ApplicationInitializerSubSystem.cs` — startup event sequence
- `Assets/Scripts/SS3D/Application/Events/` — `ApplicationPreInitializing`, `ApplicationInitializing`, `ApplicationInitialized`
- `Assets/Scripts/SS3D/Application/ApplicationSettings.cs` — app-wide settings; `ActiveTestScriptPath` = `-testscript=` or the MPPM tag
- `Assets/Scripts/SS3D/Application/MultiplayerPlayModePlayer.cs` — MPPM player name, virtual-player check, `smoke:` tag script path
- `Assets/Scripts/SS3D/Systems/Bootstrap/SystemsBootstrap.cs` — owns ApplicationInitializer + other process-wide services

## Extension points

- New process-wide services: register in `SystemsBootstrap` before `ApplicationInitializerSubSystem`, not Boot YAML.
- New app setting: add to `ApplicationSettings` and clear it in `ResetOnBuiltApplication` if it must not leak from the editor asset into builds; command-line parsing lives in `SS3D/CommandLine/`.

## Pitfalls

- **Boot storm if offline reloads Boot after Online.** Session owner arms Empty — see [networking-session](networking-session.md).
- **PlayMode tests start in the Test Runner's `InitTestScene<guid>`.** `SystemsBootstrap` must not cold-start there: `SceneSubSystem` would load Intro over the runner's scene and no test starts. The bootstrap waits for the first other scene (the tests load Boot themselves). The MCP test runner enters Play Mode without a domain reload, so the handler is cleared on `SubsystemRegistration`.
- **Listener order:** anything that subscribes to ApplicationPreInitializing / ApplicationInitializing must exist before ApplicationInitializer.OnStart — bootstrap creates ApplicationInitializer last in the AfterSceneLoad batch.

## Tests

- PlayMode `GeneralTests/PlayModePrerequisites.cs` (`HostSession_ReachesLobby`) runs the real bootstrap; MPPM smoke runs via [networking-session-testing](networking-session-testing.md).

## Depends on / Used by

- **Depends on:** [core-subsystems](core-subsystems.md), [logging](logging.md)
- **Used by:** all systems at startup; `ApplicationSettings` read by [scene-management](scene-management.md), command line, automation, intro and [tile](tile.md) (`EditorStationTemplate`)

## Related docs

- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [2026-07_session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md)
- [INDEX.md](../INDEX.md)
