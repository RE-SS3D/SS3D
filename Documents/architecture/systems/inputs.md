> Code paths: Assets/Scripts/SS3D/Systems/Inputs/
> Entry points: InputSubSystem, InputArbiter, InputInterface
> Status: partial
> Verified: 56e4cd004 — 2026-07-17

# Inputs

## Overview

Central input layer wrapping the Unity Input System. Two responsibilities:

1. **Arbitration** — which actions are live at any moment. State is *derived from a set of owned,
   self-releasing requests*, never from a shared counter. Callers push an `InputContext` or a
   suppression and receive an `IInputHandle`; disposing the handle removes exactly that request.
   `InputArbiter` recomputes each action's `enabled` flag from the live request set as the single
   writer, so dead keys, leaked input, and enabled/refcount desync are structurally impossible.
2. **Pointer authority** — `InputInterface.IsPointerOverInterface()` is the one place that answers
   "is the pointer over UI", spanning both uGUI (`EventSystem`) and UI Toolkit runtime panels.
   Callers include interaction click gates and selection hover clearing (examine/outlines).

See the effort doc [2026-07_input-arbitration.md](../2026-07_input-arbitration.md) for the model,
the context table, and the migration from the old refcount API.

## Start here

- `Assets/Scripts/SS3D/Systems/Inputs/InputSubSystem.cs` — owns `Controls`, builds the context table,
  exposes `PushContext` / `SuppressMap` / `SuppressAction` / `SuppressBinding` and the code-defined
  `UiCancel` / `DetailedExamine` actions.
- `Assets/Scripts/SS3D/Systems/Inputs/InputArbiter.cs` — pure resolution engine (unit tested).
- `Assets/Scripts/SS3D/Systems/Inputs/InputContext.cs` — the context enum (value = priority).
- `Assets/Scripts/SS3D/Systems/Inputs/InputInterface.cs` — unified pointer query + document registry.
- `Assets/Scripts/SS3D/Systems/Inputs/InputTextEntryScope.cs` — shared focus helper for text fields.

## Extension points

- **Need input while some UI/state is active?** Add a value to `InputContext` (higher value = higher
  priority) and a matching `InputContextDefinition` entry in `InputSubSystem.BuildContexts()`. Push it
  from the owner and dispose on teardown.
- **Need to temporarily block a key/map?** Use `SuppressBinding` / `SuppressMap` / `SuppressAction`
  and dispose the handle when done. Prefer disposing in `OnDisabled`/`OnDestroyed` so a missed
  pointer-exit or early disable can never strand the suppression.
- **New runtime UI Toolkit panel that should block world clicks?** Call
  `InputInterface.RegisterDocument` in setup and `UnregisterDocument` in teardown.

## Conventions

- Never call `InputAction.Enable/Disable` directly; go through a context or suppression handle.
- A handle must be owned by exactly one object and disposed once; disposing is idempotent and
  order-independent, so out-of-order release across objects is safe.

## Tests

- EditMode: `Assets/Scripts/Tests/EditMode/InputArbiterTests.cs`,
  `Assets/Scripts/Tests/EditMode/InputInterfaceTests.cs`.

## Depends on / Used by

- **Used by:** [player-control](player-control.md), [interactions-runtime](interactions-runtime.md),
  [machine-interface](machine-interface.md), [chat-audio-screens](chat-audio-screens.md),
  [tile](tile.md), [examine](examine.md), [ingame-console](ingame-console.md), [inventory](inventory.md)

## Related docs

- [2026-07_input-arbitration.md](../2026-07_input-arbitration.md)
- [INDEX.md](../INDEX.md)
