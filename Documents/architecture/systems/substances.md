> Code paths: Assets/Scripts/SS3D/Systems/Substances/
> Entry points: SubstancesSubSystem, SubstanceContainer, TransferSubstanceInteraction
> Status: partial

# Substances

## Overview

Chemical substances, containers, and transfer interactions. `TransferSubstanceInteraction` is the Tier 2 (armed targeted) proof-of-concept for the [interactions-runtime](interactions-runtime.md) radial menu.

## Start here

- `Assets/Scripts/SS3D/Systems/Substances/SubstancesSubSystem.cs` — subsystem entry point
- `Assets/Scripts/SS3D/Systems/Substances/SubstanceContainer.cs` — networked substance storage
- `Assets/Scripts/SS3D/Systems/Substances/Interactions/TransferSubstanceInteraction.cs` — armed transfer (`ITargetedInteraction`, wire ID `TransferSubstance`)

## Extension points

- Add substance interactions via `IInteraction` on containers or tools; use `IInteractionTierProvider` when radial tier is not instant.
- Targeted transfers: implement `ITargetedInteraction.CanTarget` for origin→target validation.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [inventory](inventory.md)
- **Used by:** Chemistry gameplay (partial)

## Related docs

- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md) § Phase 3
