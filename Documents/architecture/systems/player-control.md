> Code paths: Assets/Scripts/SS3D/Systems/PlayerControl/
> Entry points: PlayerSubSystem
> Status: stub
> Verified: 2e2d03815 — 2026-07-18

# Player control

## Overview

Player subsystem — connection to controllable entity, input routing, round join ordering.

## Start here

- `Assets/Scripts/SS3D/Systems/PlayerControl/PlayerSubSystem.cs` — player subsystem; UnauthorizedPlayer → auth → Player
- `Assets/Scripts/SS3D/Systems/PlayerControl/UnauthorizedPlayer.cs` — temporary NOB that broadcasts `UserAuthorizationMessage`

## Extension points

(stub)

## Pitfalls

- **`OnClientLoadedStartScenes` must gate on `asServer`:** host also fires the client-side (`asServer=false`) callback before `LoadedStartScenes(true)` is set. Spawning there warns and can create a duplicate UnauthorizedPlayer that later fails despawn ("already deinitializing"). Match FishNet `PlayerSpawner`.
- **Despawn UnauthorizedPlayer carefully:** snapshot `conn.Objects.ToArray()` and skip non-spawned objects before `ServerManager.Despawn` (`IsDeinitializing` is FishNet-internal).

## Depends on / Used by

- **Depends on:** [entities](entities.md), [inputs](inputs.md)
- **Used by:** [interactions-runtime](interactions-runtime.md), [rounds-lobby](rounds-lobby.md)

## Related docs

- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
