> Status: active
> Touches systems: interactions, selection, UI, inputs

# Interactions runtime

Player-facing orchestration: input, radial menu, server RPCs, cancellation, and visual feedback.

## Components

| Piece | Location |
|-------|----------|
| `InteractionController` | Player prefab — primary click, radial menu, inventory interactions, intent sync |
| `RadialInteractionSubSystem` | Scene — radial UI |
| `InteractionOutlineView` | Added at runtime on hovered `Selectable` |
| `SelectionSubSystem` | Hover pick → `InteractionController` discovery |

## Player flow

1. `SelectionSubSystem` resolves hovered `Selectable`.
2. `InteractionController` builds viable list via `InteractionPipeline` + active hand/tool source.
3. Primary click or radial choice sends `CmdRunInteraction` / `CmdRunInventoryInteraction` with `InteractionIdentifier`.
4. Server re-validates gates (intent, stamina, ownership, permissions) then `InteractionSource.Interact`.
5. Observers run client FX; rejections use `TargetRejectInteraction` to roll back optimistic UI.

## Cancellation

- **C** — `CmdCancelInteraction` for in-progress delayed interactions.
- Movement — `DelayedInteraction` auto-cancel via `CharacterMoveCheck`.
- Optimistic loading bars and pending outlines clear on cancel/reject/confirm.

## Outline feedback

| Color | Meaning |
|-------|---------|
| Green | Viable interaction in range |
| Yellow | Hovered but not viable |
| Blue (pending) | Instant interaction awaiting server confirm |
| Hidden | No hover, entity target, or no interaction source |

Entities (`Human`, ghosts) are excluded from hover outlines; medical targeting will use dedicated UI.

## Replication audit (Jul 2026)

| Interaction | Replication path |
|-------------|------------------|
| `OpenInteraction` | `NetworkedOpenable.SetOpenState` when present; legacy animator fallback for non-networked props |
| `ToggleInteraction` | `GenericToggleInteractionTarget` `SyncVar`; `[Server] Toggle()` |
| `LockerDoorInteraction` | `Locker.IsOpen` `SyncVar` |
| `PickupInteraction` / `DropInteraction` | Inventory/container server APIs |
| `HitInteraction` | Server-side damage on `BodyPart` |

## Tests

- EditMode: `InteractionPipelineTests`
- PlayMode: `ClientGameActions.PlayerCanDropAndPickUpItem` (pickup/drop regression)
