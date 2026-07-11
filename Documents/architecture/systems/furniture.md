> Code paths: Assets/Scripts/SS3D/Systems/Furniture/, Assets/Content/WorldObjects/Furniture/
> Entry points: (various world object behaviours)
> Status: stub

# Furniture / world objects

## Overview

Station furniture and interactable world objects — airlocks, lockers, disposal units, draggable objects. Vending machines now open a diegetic panel via [machine-interface](machine-interface.md) instead of a direct dispense interaction.

## Start here

- `Assets/Scripts/SS3D/Systems/Furniture/` — furniture implementations (airlocks, lockers, etc.)
- `Assets/Scripts/SS3D/UI/MachineInterface/VendingMachineController.cs` — vending machines (machine-interface controller)
- `Assets/Content/WorldObjects/Furniture/Machines/Vendors/` — vending machine prefabs

## Extension points

- New furniture: add `InteractionTargetBehaviour` + domain-specific interactions per [interactions-framework](interactions-framework.md).
- Panel-driven machines: subclass `MachineInterfaceBehaviour` and register UI per [machine-interface](machine-interface.md) extension recipe.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [tile](tile.md), [machine-interface](machine-interface.md) (vending)
- **Used by:** world scenes and construction content

## Related docs

- [machine-interface](machine-interface.md) — vending UI and diegetic shell
- [INDEX.md](../INDEX.md)
