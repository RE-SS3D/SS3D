> Code paths: Assets/Scripts/SS3D/Systems/Furniture/
> Entry points: (various world object behaviours)
> Status: partial

# Furniture / world objects

## Overview

Station furniture and interactable world objects — airlocks, vending machines, jukebox, and related prefab behaviours. Power consumption and visuals delegate to [electricity](electricity.md) consumers; airlock opening respects area APC power.

## Start here

- `Assets/Scripts/SS3D/Systems/Furniture/AirLockOpener.cs` — proximity open/close; gated on `BasicPowerConsumer`; closes on power loss
- `Assets/Scripts/SS3D/Systems/Furniture/AirlockStateMachine.cs` — animator panel colors during open/close
- `Assets/Scripts/SS3D/Systems/Furniture/VendingMachine.cs` — vendor dispense; uses `MachinePowerConsumer`
- `Assets/Scripts/SS3D/Systems/Audio/Boombox.cs` — jukebox toggle; stops audio on power loss

## Extension points

- New furniture interactions: add `InteractionTargetNetworkBehaviour` (or `InteractionSource`) + domain interactions per [interactions-framework](interactions-framework.md).
- Powered machines: attach `MachinePowerConsumer` or `BasicPowerConsumer` + `ElectricDeviceAdjacencyConnector`; add `ConsumerPowerVisual` when emissive meshes should dim unpowered (see [electricity](electricity.md)).
- Wall light switches: use [area](area.md) `LightSwitchController`, not furniture scripts.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [interactions-runtime](interactions-runtime.md), [tile](tile.md), [electricity](electricity.md), [area](area.md) (airlock/switch area resolution)

## Related docs

- Plan: [areas_implementation_plan_c0639343.plan.md](../../plans/areas_implementation_plan_c0639343.plan.md)
- [INDEX.md](../INDEX.md)
