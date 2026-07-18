> Code paths: Assets/Scripts/SS3D/Systems/Interactions/
> Entry points: InteractionController, RadialInteractionSubSystem, ArmedInteractionSubSystem
> Status: shipped
> Verified: a20853b1c — 2026-07-18

# Interactions (runtime)

## Overview

Client-side interaction routing: discovers available interactions from the current selection and player state, presents the three-tier radial menu, arms targeted interactions, and dispatches `InteractionIdentifier`-based requests to the server. Bridges [selection](selection.md) hover targets with the shared [interactions-framework](interactions-framework.md).

## Start here

- `Assets/Scripts/SS3D/Systems/Interactions/InteractionController.cs` — primary click, radial dispatch, intent sync, armed resolution, outline feedback
- `Assets/Scripts/SS3D/Systems/Interactions/RadialInteractionSubSystem.cs` — three-tier radial menu subsystem
- `Assets/Scripts/SS3D/Systems/Interactions/UI/RadialInteractionMenuView.cs` — radial menu UI (UI Toolkit)
- `Assets/Scripts/SS3D/Systems/Interactions/UI/RadialInteractionPetal.cs` — dynamic petal elements
- `Assets/Scripts/SS3D/Systems/Interactions/UI/ArmedInteractionOverlayView.cs` — reticle, chip, and target highlight overlay
- `Assets/Scripts/SS3D/Systems/Interactions/ArmedInteractionSubSystem.cs` — armed-mode interaction overlay
- `Assets/Scripts/SS3D/Systems/Interactions/InteractionOutlineView.cs` — hover and pending interaction outlines (exclude from pick via [selection](selection.md) rendering layers; clear on pickup)
- `Assets/Scripts/SS3D/Systems/Interactions/ArmedTargetEvaluation.cs` — armed target filtering
- `Assets/Art/Graphics/InteractionOutline.shader` — inverted-hull outline material for availability feedback

## Player flow

1. `SelectionSubSystem` resolves hovered `Selectable`.
2. `InteractionController` builds viable list via `InteractionPipeline` + active hand/tool source.
3. Primary click or instant radial choice sends `CmdRunInteraction` with `InteractionIdentifier`.
4. Targeted radial choices arm the cursor via `TryRouteRadialInteraction`; second click resolves the matching `InteractionEntry` by `GetGenericName()` and dispatches RPC.
5. Server re-validates gates (intent, stamina, ownership, permissions) then `InteractionSource.Interact`.
6. Observers run client FX; rejections use `TargetRejectInteraction` to roll back optimistic UI.

## Outline feedback

| Color | Meaning |
|-------|---------|
| Green | Target-bound interaction viable now (in range, intent, gates) |
| Yellow | Target-bound interaction discovered but not currently viable (e.g. out of range) |
| Blue (pending) | Instant interaction awaiting server confirm |
| Hidden | No hover, entity target, no source, or only source-only entries (e.g. Drop while holding) |

Entities (`Human`, ghosts) are excluded from hover outlines; medical targeting will use dedicated UI.

Hover outlines ignore source-only discoveries such as `Drop` (`InteractionEntry.Target == null`). Those always appear while an item is held and must not outline every `Selectable` under the cursor.

Structural Discover/source-list debt: [interactions-framework](interactions-framework.md) § Architecture smells.

## Pitfalls

- **Outline on every hover while holding an item:** `Item.CreateSourceInteractions` always discovers `Drop` with a null target. Outline evaluation must run `InteractionPipeline.FilterForOutline` (keep only `Target != null`) before treating Discover as "available."
- **Outline on every hover with empty hands:** obsolete `Craft` on hands used to discover `OpenCraftingMenu` for every target. Holding an item switches the source to the item (no `Craft`), so the bug only showed empty-handed. Do not extend crafting — purge per [crafting](crafting.md); until then discover must stay gated.

## Cancellation

- **C** — `CmdCancelInteraction` for in-progress delayed interactions.
- Movement — `DelayedInteraction` auto-cancel via `CharacterMoveCheck`.

## Extension points

- New world interactions: implement in domain system via framework contracts; they appear automatically when source/target resolution succeeds.
- Radial menu tiers: implement `IInteractionTierProvider` on sources/targets.
- Armed mode: extend `ArmedTargetEvaluation` for new armed interaction categories.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [selection](selection.md), [player-control](player-control.md), [inputs](inputs.md)
- **Used by:** Nearly all player-facing gameplay actions

## Related docs

- Effort: [2026-07_interaction-system-hardening](../2026-07_interaction-system-hardening.md)
- Plan: [radial_menu_implementation_5a83bdf9.plan.md](../../plans/radial_menu_implementation_5a83bdf9.plan.md)
- Plan: [interaction_system_improvements_9e14ae22.plan.md](../../plans/interaction_system_improvements_9e14ae22.plan.md)
- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
- Tests: EditMode `InteractionPipelineTests`; PlayMode `InteractionPlayModeTests` / `ClientGameActions.PlayerCanDropAndPickUpItem`
