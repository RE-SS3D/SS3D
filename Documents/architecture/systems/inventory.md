> Code paths: Assets/Scripts/SS3D/Systems/Inventory/
> Entry points: ItemSubSystem
> Status: partial

# Inventory

## Overview

Items, containers, hands, and identification cards (`IDCard`, `PDA`). ID cards bind to server-side crew records via [id-access](id-access.md); spawn-time binding in `RoleSubSystem`.

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

- [INDEX.md](../INDEX.md)
