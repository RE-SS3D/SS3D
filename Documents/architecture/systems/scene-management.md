> Code paths: Assets/Scripts/SS3D/SceneManagement/
> Entry points: SceneSubSystem
> Status: stub
> Verified: 2e2d03815 — 2026-07-18

# Scene management

## Overview

Scene loading and switching. Integrates with editor toolbar Scene Switcher.

Scenes are launch pads, not system composition roots ([agent-first composition](../2026-07_agent-first-composition.md)).

## Start here

- `Assets/Scripts/SS3D/SceneManagement/SceneSubSystem.cs` — scene loading subsystem
- `Assets/Scripts/SS3D/Data/Generated/Scenes.cs` — codegen scene references

## Extension points

(stub)

## Pitfalls

- **Duplicate EventSystem when Game loads additively over Intro:** Intro Objects prefab and Game both have an EventSystem. Unload is async — disable Intro/Launcher EventSystems synchronously when Game becomes active, then unload. Do not leave both enabled.

## Depends on / Used by

- **Depends on:** [data-codegen](data-codegen.md)

## Related docs

- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [INDEX.md](../INDEX.md)
