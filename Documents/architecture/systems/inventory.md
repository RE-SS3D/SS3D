> Code paths: Assets/Scripts/SS3D/Systems/Inventory/
> Entry points: ItemSubSystem
> Status: partial

# Inventory

## Overview

Items, containers, hands, and identification cards (`IDCard`, `PDA`). ID cards bind to server-side crew records via [id-access](id-access.md); spawn-time binding in `RoleSubSystem`.

**Condemned UI:** inventory / hands / intent uGUI — do not extend; replace per [main-hud.md](../../design/main-hud.md) and [inventory-storage.md](../../design/inventory-storage.md) with Phase 0 purge. Domain items/containers may remain until that redesign. Hands wiring on `Human.prefab` is prefab composition debt ([agent-first composition](../2026-07_agent-first-composition.md)).

## Start here

- `Assets/Scripts/SS3D/Systems/Inventory/Items/ItemSubSystem.cs` — item subsystem entry point
- `Assets/Scripts/SS3D/Systems/Inventory/Containers/HumanInventory.cs` — on-person containers and hands
- `Assets/Scripts/SS3D/Systems/Inventory/Items/Identification/IDCard.cs` — physical ID token
- `Assets/Scripts/SS3D/Systems/Inventory/Items/Identification/PDA.cs` — PDA with internal ID slot

## Extension points

(stub)

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md)
- **Used by:** [examine](examine.md), [player-control](player-control.md), [id-access](id-access.md)

## Related docs

- Design (read-only): [Documents/design/inventory-storage.md](../../design/inventory-storage.md), [main-hud.md](../../design/main-hud.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
- [INDEX.md](../INDEX.md)
