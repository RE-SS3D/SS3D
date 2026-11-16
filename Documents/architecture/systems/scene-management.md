> Code paths: Assets/Scripts/SS3D/SceneManagement/
> Entry points: SceneSubSystem
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Scene management

## Overview

Thin first-scene loader plus the editor Scene Switcher. FishNet owns online/offline scene loads; this domain only picks the first scene after Boot and cleans up when Game arrives.

`SceneSubSystem` is DDOL via `SystemsBootstrap`. On `ApplicationInitializing` it loads `Game` (dedicated server), `Intro` (editor or command-line client), or `Launcher` (`ApplicationSettings.ForceLauncher`, or a build without args). An editor automation host (active test script, not an MPPM virtual player) loads nothing so FishNet's online-scene load is not raced. When `Game` loads it becomes the active scene and Intro/Launcher are unloaded. Scenes are launch pads, not system composition roots ([agent-first composition](../2026-07_agent-first-composition.md)). FishNet offline after first Online is `Scenes.Empty` — see [networking-session](networking-session.md).

## Start here

- `Assets/Scripts/SS3D/SceneManagement/SceneSubSystem.cs` — first-scene choice and Game-load cleanup
- `Assets/Scripts/SS3D/SceneManagement/Scene.cs` — static `Load`/`LoadAsync` wrapper and editor `Names` list
- `Assets/Scripts/SS3D/SceneManagement/Editor/SceneSwitcherTool.cs` — left toolbar dropdown (`Tools/Scene Switcher`) built from `Scene.Names`
- `Assets/Scripts/SS3D/SceneManagement/Editor/EditorSceneUtils.cs` — stops Play if running, then opens the chosen scene in the Editor
- `Assets/Scripts/SS3D/Data/Generated/Scenes.cs` — codegen scene refs (`Boot`, `Intro`, `Game`, `Launcher`, `Empty`, `EmptyPath`, `BootPath`)

## Extension points

- New scene: add it to `Scenes.cs` (codegen) and to `Scene.Names` so the switcher lists it.
- First-scene rules live only in `SceneSubSystem.LoadMainScene`; network scene changes go through FishNet ([networking-session](networking-session.md)).

## Pitfalls

- **Duplicate EventSystem when Game loads additively over Intro:** Intro Objects prefab and Game both have an EventSystem. Unload is async — disable Intro/Launcher EventSystems synchronously when Game becomes active, then unload. Do not leave both enabled.
- **Automation host must not load Intro:** loading Intro/Launcher on an MPPM smoke host races FishNet's online Game load and the local client times out. Clients still load Intro, because starting in Boot makes FishNet register Game's scene objects before they exist.
- **Disconnect must not reload Boot after first Online.** CCR arms Empty; see [networking-session](networking-session.md) Pitfalls.

## Depends on / Used by

- **Depends on:** [data-codegen](data-codegen.md), [application](application.md) (`ApplicationInitializing`, `ApplicationSettings`, `MultiplayerPlayModePlayer`), [core-subsystems](core-subsystems.md)
- **Used by:** [networking-session](networking-session.md), [application](application.md)

## Related docs

- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [2026-07_session-world-lifecycle](../../archive/architecture/2026-07_session-world-lifecycle.md)
- [INDEX.md](../INDEX.md)
