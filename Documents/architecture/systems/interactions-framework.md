> Code paths: Assets/Scripts/SS3D/Interactions/
> Entry points: IInteraction, IInteractionSource, IInteractionTarget, InteractionPipeline, InteractionIdentifier
> Status: shipped

# Interactions (framework)

## Overview

Shared interaction contracts used across all gameplay systems. Defines how interaction sources discover targets, build `InteractionEntry` lists, and route client/server execution. Domain systems implement `IInteraction` on behaviours; runtime routing lives in [interactions-runtime](interactions-runtime.md).

RPCs identify interactions with `InteractionIdentifier` (`genericName` + `targetComponentIndex`), never display `GetName()`.

## Start here

- `Assets/Scripts/SS3D/Interactions/Interfaces/IInteraction.cs` — core contract (`GetGenericName`, `Priority`, server-only `Start`)
- `Assets/Scripts/SS3D/Interactions/Interfaces/IInteractionSource.cs` — objects that offer interactions (hands, items)
- `Assets/Scripts/SS3D/Interactions/Interfaces/IInteractionTarget.cs` — objects that receive interactions
- `Assets/Scripts/SS3D/Interactions/InteractionEntry.cs` — target + interaction + wire identifier
- `Assets/Scripts/SS3D/Interactions/InteractionIdentifier.cs` — stable RPC wire ID
- `Assets/Scripts/SS3D/Interactions/InteractionPipeline.cs` — shared discover → filter → sort
- `Assets/Scripts/SS3D/Interactions/InteractionTier.cs` — instant / targeted / folder tiers for radial menu
- `Assets/Scripts/SS3D/Interactions/Interfaces/IInteractionTierProvider.cs` — per-interaction tier override
- `Assets/Scripts/SS3D/Interactions/Extensions/InteractionExtensions.cs` — `GetInteractionTier()` helper
- `Assets/Scripts/SS3D/Interactions/InteractionOptimisticFeedback.cs` — delayed loading bars during server confirm
- `Assets/Scripts/SS3D/Interactions/Interfaces/IIntentRestrictedInteraction.cs` — Help/Harm gate
- `Assets/Scripts/SS3D/Interactions/Interfaces/ITargetedInteraction.cs` — armed-mode second-click targeting
- `Assets/Scripts/SS3D/Interactions/DelayedInteraction.cs` — timed interaction base class
- `Assets/Scripts/SS3D/Interactions/InteractionIconLookup.cs` — resolves radial/menu sprites from `InteractionIcons`

## Extension points

- Implement `IInteraction` (or subclass `DelayedInteraction`) on a `NetworkBehaviour` for new interaction types.
- Add `InteractionTargetBehaviour` (or `InteractionTargetNetworkBehaviour`) to world objects that should receive interactions.
- Implement `IInteractionTierProvider` to control radial menu tier (instant vs armed targeted).
- Use `Requirement` and `IInteractionRangeLimit` / `RangeLimit` for gating.
- Register interaction icons via generated `InteractionIcons` asset refs ([data-codegen](data-codegen.md)); expose named helpers on `InteractionIconLookup` when shared.
- Replicated state changes in `Start()` must go through networked components (`NetworkedOpenable.SetOpenState`, `SyncVar` toggles), not local-only animator writes.

## Depends on / Used by

- **Used by:** [interactions-runtime](interactions-runtime.md), [inventory](inventory.md), [furniture](furniture.md), [tile](tile.md), and most gameplay systems
- **Depends on:** [core-subsystems](core-subsystems.md) (network actors)

## Related docs

- Effort: [2026-07_interaction-system-hardening](../2026-07_interaction-system-hardening.md)
- Plan: [interaction_system_improvements_9e14ae22.plan.md](../../plans/interaction_system_improvements_9e14ae22.plan.md)
- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md) § intent chording
- Tests: `Assets/Scripts/Tests/EditMode/InteractionPipelineTests.cs`
