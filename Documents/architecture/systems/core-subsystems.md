> Code paths: Assets/Scripts/SS3D/Core/
> Entry points: SubSystem, NetworkSubSystem, SubSystems
> Status: shipped

# Core / SubSystems

## Overview

Base actor/subsystem pattern and runtime service locator. All gameplay domains expose a `*SubSystem` registered via `SubSystems.Get<T>()`. `NetworkSubSystem` extends FishNet `NetworkActor` for networked subsystems.

## Start here

- `Assets/Scripts/SS3D/Core/Behaviours/SubSystem.cs` — non-networked subsystem base
- `Assets/Scripts/SS3D/Core/Behaviours/NetworkSubSystem.cs` — networked subsystem base
- `Assets/Scripts/SS3D/Core/Subsystems.cs` — `SubSystems` static locator
- `Assets/Scripts/SS3D/Core/ViewLocator.cs` — view discovery helper

## Extension points

- New domain subsystem: subclass `SubSystem` or `NetworkSubSystem`, register on a scene actor.

## Depends on / Used by

- **Used by:** All subsystem-backed domains

## Related docs

- [INDEX.md](../INDEX.md)
