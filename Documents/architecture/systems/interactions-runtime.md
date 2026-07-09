> Code paths: Assets/Scripts/SS3D/Systems/Interactions/
> Entry points: InteractionController, RadialInteractionSubSystem, ArmedInteractionSubSystem
> Status: shipped

# Interactions (runtime)

## Overview

Client-side interaction routing: discovers available interactions from the current selection and player state, presents the radial menu, and dispatches interaction requests to the server. Bridges [selection](selection.md) hover targets with the shared [interactions-framework](interactions-framework.md).

## Start here

- `Assets/Scripts/SS3D/Systems/Interactions/InteractionController.cs` — main client interaction router; uses selection for targeting
- `Assets/Scripts/SS3D/Systems/Interactions/RadialInteractionSubSystem.cs` — three-tier radial menu subsystem
- `Assets/Scripts/SS3D/Systems/Interactions/UI/RadialInteractionMenuView.cs` — radial menu UI
- `Assets/Scripts/SS3D/Systems/Interactions/ArmedInteractionSubSystem.cs` — armed-mode interaction overlay
- `Assets/Scripts/SS3D/Systems/Interactions/InteractionFolder.cs` — groups interactions for radial tiers
- `Assets/Scripts/SS3D/Systems/Interactions/ArmedTargetEvaluation.cs` — armed target filtering

## Extension points

- New world interactions: implement in domain system via framework contracts; they appear automatically when source/target resolution succeeds.
- Radial menu tiers: implement `IInteractionTierProvider` on sources/targets.
- Armed mode: extend `ArmedTargetEvaluation` for new armed interaction categories.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [selection](selection.md), [player-control](player-control.md), [inputs](inputs.md)
- **Used by:** Nearly all player-facing gameplay actions

## Related docs

- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
