> Status: active
> Touches systems: interactions, inventory, combat, crafting, furniture

# Interactions framework

Composable **source/target** discovery for SS3D gameplay. Gameplay code implements `IInteraction` on targets and sources; the framework handles discovery, filtering, server execution, and client FX.

## Core types

| Type | Role |
|------|------|
| `IInteraction` | Single interaction contract (`CanInteract`, `Start`, stable `GetGenericName`, `Priority`) |
| `IInteractionSource` | Executor (hands, items, machines). Server `Interact()` / `CancelInteraction()` |
| `IInteractionTarget` | Produces target-side interactions via `CreateTargetInteractions` |
| `InteractionEntry` | Target + interaction + `InteractionIdentifier` |
| `InteractionIdentifier` | Wire ID: `genericName` + `targetComponentIndex` |
| `InteractionPipeline` | Shared discover → filter → sort used by client UI and server RPC re-validation |
| `DelayedInteraction` | Timed interactions with movement/stamina cancel |
| `Requirement` | Decorator wrapping another interaction |
| `IIntentRestrictedInteraction` | Optional Help/Harm gate |

## Discovery flow

1. Collect `IInteractionTarget` components on the clicked object (or synthesize `InteractionTargetGameObject`).
2. `CreateTargetInteractions` on each target; `CreateSourceInteractions` on source/extensions.
3. `InteractionPipeline.FilterAndSort` applies `CanInteract`, intent, `CanExecuteInteraction`, then sorts by `Priority` descending.

## Networking rules

- RPCs identify interactions with `InteractionIdentifier`, never display `GetName()`.
- Server re-runs the pipeline before `Interact()`.
- `Start()` is server-only. Replicated state changes go through networked components (`NetworkedOpenable.SetOpenState`, `SyncVar` toggles, locker `SyncVar`s).
- Observer client FX skip dedicated server (`IsServer` early-out in observer RPCs).

## Client feedback

- `InteractionOptimisticFeedback` — delayed loading bars while awaiting server confirm.
- `InteractionOutlineView` — hover availability (green/yellow) and instant pending (blue) outlines; skipped on `Entity` targets (medical UI later).

## Entry points

- `Assets/Scripts/SS3D/Interactions/` — framework types
- `Assets/Scripts/SS3D/Systems/Interactions/InteractionController.cs` — player input + RPCs

## Tests

- `Assets/Scripts/Tests/EditMode/InteractionPipelineTests.cs` — priority, intent, identifier resolution
