> Code paths: Assets/Scripts/SS3D/Interactions/
> Entry points: IInteraction, IInteractionSource, IInteractionTarget, InteractionInstance
> Status: shipped

# Interactions (framework)

## Overview

Shared interaction contracts used across all gameplay systems. Defines how interaction sources discover targets, build `InteractionInstance` objects, and route client/server execution. Domain systems implement `IInteraction` on behaviours; runtime routing lives in [interactions-runtime](interactions-runtime.md).

## Start here

- `Assets/Scripts/SS3D/Interactions/Interfaces/IInteraction.cs` — core interaction contract
- `Assets/Scripts/SS3D/Interactions/Interfaces/IInteractionSource.cs` — objects that offer interactions (hands, items)
- `Assets/Scripts/SS3D/Interactions/Interfaces/IInteractionTarget.cs` — objects that receive interactions
- `Assets/Scripts/SS3D/Interactions/InteractionInstance.cs` — resolved source + target + interaction triple
- `Assets/Scripts/SS3D/Interactions/InteractionSource.cs` — base source behaviour
- `Assets/Scripts/SS3D/Interactions/InteractionTargetBehaviour.cs` — base target on world objects
- `Assets/Scripts/SS3D/Interactions/IntentType.cs` — player intent enum (help, harm, grab, etc.)
- `Assets/Scripts/SS3D/Interactions/DelayedInteraction.cs` — timed interaction base class

## Extension points

- Implement `IInteraction` (or subclass `DelayedInteraction`) on a `NetworkBehaviour` for new interaction types.
- Add `InteractionTargetBehaviour` (or `InteractionTargetNetworkBehaviour`) to world objects that should receive interactions.
- Use `Requirement` and `IInteractionRangeLimit` / `RangeLimit` for gating.
- Register interaction icons via generated `InteractionIcons` asset refs ([data-codegen](data-codegen.md)).

## Depends on / Used by

- **Used by:** [interactions-runtime](interactions-runtime.md), [inventory](inventory.md), [furniture](furniture.md), [tile](tile.md), [examine](examine.md), and most gameplay systems
- **Depends on:** [core-subsystems](core-subsystems.md) (network actors)

## Related docs

- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md) § intent chording
- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md)
