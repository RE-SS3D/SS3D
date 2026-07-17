> Code paths: Assets/Scripts/SS3D/Core/
> Entry points: SubSystem, NetworkSubSystem, SubSystems
> Status: shipped

# Core / SubSystems

## Overview

Base actor/subsystem pattern and runtime service locator. All gameplay domains expose a `*SubSystem` registered via `SubSystems.Get<T>()`. `NetworkSubSystem` extends FishNet `NetworkActor` for networked subsystems.

Scene-placed registration on Boot/Game actors is **legacy**. Target is code bootstrap ([agent-first composition](../2026-07_agent-first-composition.md)); do not add new systems by editing scene YAML.

## Start here

- `Assets/Scripts/SS3D/Core/Behaviours/SubSystem.cs` — non-networked subsystem base
- `Assets/Scripts/SS3D/Core/Behaviours/NetworkSubSystem.cs` — networked subsystem base
- `Assets/Scripts/SS3D/Core/Subsystems.cs` — `SubSystems` static locator
- `Assets/Scripts/SS3D/Core/ViewLocator.cs` — view discovery helper

## Extension points

- New domain subsystem: subclass `SubSystem` or `NetworkSubSystem`. Prefer code bootstrap / self-register (see ScreenEffects) over adding a GameObject to Boot/Game. Scene registration remains until the bootstrap follow-on ships.

## Depends on / Used by

- **Used by:** All subsystem-backed domains

## Related docs

- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [INDEX.md](../INDEX.md)
