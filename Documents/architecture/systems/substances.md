> Code paths: Assets/Scripts/SS3D/Systems/Substances/
> Entry points: SubstancesSubSystem, SubstanceContainer, TransferSubstanceInteraction
> Status: partial
> Verified: 8d5428105 — 2026-07-17

# Substances

## Overview

Chemical substances, containers, and transfer interactions. `TransferSubstanceInteraction` is the Tier 2 (armed targeted) proof-of-concept for the [interactions-runtime](interactions-runtime.md) radial menu. Circulatory bleed / other health paths call `SubstanceContainer` on the server each pulse — keep container hot paths allocation-free.

## Start here

- `Assets/Scripts/SS3D/Systems/Substances/SubstancesSubSystem.cs` — subsystem entry point
- `Assets/Scripts/SS3D/Systems/Substances/SubstanceContainer.cs` — networked substance storage; caches `AsReadOnly()` view once
- `Assets/Scripts/SS3D/Systems/Substances/Interactions/TransferSubstanceInteraction.cs` — armed transfer (`ITargetedInteraction`, wire ID `TransferSubstance`)

## Extension points

- Add substance interactions via `IInteraction` on containers or tools; use `IInteractionTierProvider` when radial tier is not instant.
- Targeted transfers: implement `ITargetedInteraction.CanTarget` for origin→target validation.

## Pitfalls

- **GC on `SubstanceContainer.Substances`:** `List.AsReadOnly()` allocates a new wrapper every call. Cache the view; internal mutators (`IndexOfSubstance`, `RemoveSubstance`, volume recalcs) must use `_substances` directly. Heart bleed previously spiked GC through this property.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [inventory](inventory.md)
- **Used by:** [health](health.md) (circulatory blood container), chemistry gameplay (partial)

## Related docs

- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md) § Phase 3
- Design (read-only): [Documents/design/chemistry.md](../../design/chemistry.md)
