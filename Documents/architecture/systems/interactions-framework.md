> Code paths: Assets/Scripts/SS3D/Interactions/
> Entry points: IInteraction, IInteractionSource, IInteractionTarget, InteractionPipeline, InteractionIdentifier
> Status: shipped
> Verified: a20853b1c — 2026-07-18

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
- `Assets/Scripts/SS3D/Interactions/InteractionPipeline.cs` — shared discover → filter → sort; `FilterForOutline` drops source-only entries for hover feedback
- `Assets/Scripts/SS3D/Interactions/InteractionEvent.cs` — source/target/point/normal; default `Point` is `Vector3.zero` when unset (see smells)
- `Assets/Scripts/SS3D/Interactions/InteractionTier.cs` — instant / targeted / folder tiers for radial menu
- `Assets/Scripts/SS3D/Interactions/Interfaces/IInteractionTierProvider.cs` — per-interaction tier override
- `Assets/Scripts/SS3D/Interactions/Extensions/InteractionExtensions.cs` — `RangeCheck`, `GetInteractionTier()`
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
- Prefer gating `IInteractionSourceExtension.GetSourceInteractions` on a real availability check (like `HandHit`), not unconditional `Add` — see smells below.

## Architecture smells

Structural debt (not one-off bugs). Bandages live in Pitfalls / [interactions-runtime](interactions-runtime.md); prefer fixing the contract when touching this area.

1. **`Discover` has no contract.** Some source extensions always `Add` (e.g. `Drop`); others gate on `CanInteract` at discover time (`HandHit`, CPR). Consumers cannot tell whether an entry means “candidate for this target,” “source-only world action,” or “already range-checked.” Empty-hand vs held-item also swaps which extensions run (`Hands.GetActiveInteractionSource` → Hand or Item), so the same hover can look fine with an item and broken with empty hands.
2. **Source-only and target-bound entries share one list.** `Drop` uses `Target == null` in the same bag as object interactions. Anything that assumes Discover ≈ “doable *to this hover*” needs a consumer filter (`FilterForOutline`). Longer-term: mark source-only interactions or split discover lists.
3. **`InteractionEvent.Point` uses `Vector3.zero` as unset.** Magnitude checks cannot distinguish “no point resolved” from a real hit at world origin. Prefer an explicit `HasPoint` (or nullable) when reshaping the event type.
4. **Pickable ≠ rangeable** (owned with [selection](selection.md)): shader pick works without colliders; range/drop need a resolved point from colliders. Missing colliders on `Selectable` wall mounts silently break range until `RangeCheck` falls back to the transform.

## Pitfalls

- **Source-only interactions pollute hover outlines:** Drop always discovers while holding an item (`Target == null`). Runtime must `FilterForOutline` before treating Discover as “available on this object” (smell #2).
- **Missing interaction point used to skip range:** `RangeCheck` treated default zero point as unlimited range. Unresolved points now range against the target transform/collider; wall mounts should still ship a `BoxCollider` for selection rays (smells #3–4).
- **Unconditional source `Add` pollutes Discover:** any extension that adds for every target (historical `Craft` on hands) lights yellow outlines / menus on every hover when that source is active. Gate at discover time or remove the obsolete extension ([crafting](crafting.md) is due for purge).

## Depends on / Used by

- **Used by:** [interactions-runtime](interactions-runtime.md), [inventory](inventory.md), [furniture](furniture.md), [tile](tile.md), and most gameplay systems
- **Depends on:** [core-subsystems](core-subsystems.md) (network actors)

## Related docs

- Effort: [2026-07_interaction-system-hardening](../2026-07_interaction-system-hardening.md)
- Plan: [interaction_system_improvements_9e14ae22.plan.md](../../plans/interaction_system_improvements_9e14ae22.plan.md)
- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md) § intent chording
- Tests: `Assets/Scripts/Tests/EditMode/InteractionPipelineTests.cs`, `InteractionRangeCheckTests.cs`
