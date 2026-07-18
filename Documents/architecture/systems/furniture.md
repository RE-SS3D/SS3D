> Code paths: Assets/Scripts/SS3D/Systems/Furniture/, Assets/Content/WorldObjects/Furniture/
> Entry points: (various world object behaviours)
> Status: partial
> Verified: 2e2d03815 — 2026-07-18

# Furniture / world objects

## Overview

Station furniture and interactable world objects — airlocks, lockers, disposal units, vending machines, jukebox, and related prefab behaviours. Power consumption and visuals delegate to [electricity](electricity.md) consumers; airlock opening respects area APC power. Vending machines open a diegetic panel via [machine-interface](machine-interface.md) instead of a direct dispense interaction.

## Start here

- `Assets/Scripts/SS3D/Systems/Furniture/Locker.cs` — door + ID lock; implements `IStorageAccessGate` so view/store only while open; closing door closes storage panels
- `Assets/Scripts/SS3D/Systems/Furniture/AirLockOpener.cs` — proximity open/close; power-gated; `IDynamicTileOccupant` notifies [atmospherics](atmospherics.md) on door state change
- `Assets/Scripts/SS3D/Systems/Furniture/AirlockStateMachine.cs` — animator panel colors during open/close
- `Assets/Scripts/SS3D/UI/MachineInterface/VendingMachineController.cs` — vending machines (machine-interface controller)
- `Assets/Scripts/SS3D/Systems/Audio/Boombox.cs` — jukebox toggle; stops audio on power loss
- `Assets/Content/WorldObjects/Furniture/Machines/Vendors/` — vending machine prefabs

## Extension points

- New furniture interactions: add `InteractionTargetNetworkBehaviour` (or `InteractionSource`) + domain interactions per [interactions-framework](interactions-framework.md).
- Powered machines: attach `MachinePowerConsumer` or `BasicPowerConsumer` + `ElectricDeviceAdjacencyConnector`; add `ConsumerPowerVisual` when emissive meshes should dim unpowered (see [electricity](electricity.md)).
- Panel-driven machines: subclass `MachineInterfaceBehaviour` and register UI per [machine-interface](machine-interface.md) extension recipe.
- Wall light switches: use [area](area.md) `LightSwitchController`, not furniture scripts.

## Pitfalls

- **Airlocks stuck open after leaving the trigger:** `AirLockOpener` closes on `OnPowerStatusUpdated(Inactive)`. If [electricity](electricity.md) `PowerAreaConsumers` writes `Inactive` then `Powered` every tick (~0.2s), the SyncVar OnChange restarts the 2s close timer forever. Assign final `PowerStatus` once per consumer; never clear-then-set. Defense: `ScheduleCloseAfterDelay` must not restart an already-running timer. Regression test: `PowerAreaConsumers_AssignsFinalStatusOnceWithoutFlicker`. Hit three times (91b053c1d, Jul 16 uncommitted, 2026-07-18).

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [interactions-runtime](interactions-runtime.md), [tile](tile.md), [electricity](electricity.md), [area](area.md) (airlock/switch area resolution), [atmospherics](atmospherics.md) (airlock passability), [machine-interface](machine-interface.md) (vending)
- **Used by:** world scenes and construction content

## Related docs

- [machine-interface](machine-interface.md) — vending UI and diegetic shell
- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md)
- [INDEX.md](../INDEX.md)
