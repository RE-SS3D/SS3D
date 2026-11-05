> Code paths: Assets/Scripts/SS3D/Systems/Inputs/, Assets/Content/Systems/Input/
> Entry points: InputSubSystem, InputArbiter, InputInterface
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Inputs

## Overview

Central input layer wrapping the Unity Input System. Two responsibilities:

1. **Arbitration** — which actions are live at any moment. State is *derived from a set of owned,
   self-releasing requests*, never from a shared counter. Callers push an `InputContext` or a
   suppression and receive an `IInputHandle`; disposing the handle removes exactly that request.
   `InputArbiter` recomputes each action's `enabled` flag from the live request set as the single
   writer, so dead keys, leaked input, and enabled/refcount desync are structurally impossible.
2. **Pointer authority** — `InputInterface.IsPointerOverInterface()` is the one place that answers
   "is the pointer over UI", spanning uGUI (`GraphicRaycaster`) and UI Toolkit (`panel.Pick`).
   It is also true while `InputTextEntryScope` holds a text-capture (compose / focused fields) so
   world clicks and selection clear for the whole typing session, not only when the cursor is over
   the field. Callers include interaction click gates and selection hover clearing (examine/outlines).

**Default key layout** (Caps sprint, Shift examine, F intent, Backspace cancel, E = Use only):
[2026-07_default-input-scheme.md](../../archive/architecture/2026-07_default-input-scheme.md). See that doc before adding
bindings. Arbitration model: [2026-07_input-arbitration.md](../../archive/architecture/2026-07_input-arbitration.md).

## Start here

- `Assets/Scripts/SS3D/Systems/Inputs/InputSubSystem.cs` — owns `Controls`, builds the context table,
  exposes `PushContext` / `SuppressMap` / `SuppressAction` / `SuppressBinding` and the code-defined
  `UiCancel` / `DetailedExamine` / `ToggleIntent` / `OpenLocalSpeechCompose` / `ToggleDebugMenu` /
  `OpenBugReport` (F8, rebindable as "Report a Bug"; live in every context but Console, Dialog and TextEntry) /
  `ToggleRoundSummary` (Tab, collapses the round-end summary; Global, Gameplay, TileMenu and MapEditor;
  not in the Controls tab, so Tab is reserved) actions.
- `Assets/Content/Systems/Input/Controls.inputactions` — asset bindings (keep in sync with generated
  `Controls.cs` when regenerating).
- `Assets/Scripts/SS3D/Systems/Inputs/InputArbiter.cs` — pure resolution engine (unit tested).
- `Assets/Scripts/SS3D/Systems/Inputs/InputContext.cs` — the context enum (value = priority).
- `Assets/Scripts/SS3D/Systems/Inputs/InputInterface.cs` — unified pointer query + document registry.
- `Assets/Scripts/SS3D/Systems/Inputs/InputTextEntryScope.cs` — shared focus helper for text fields; holds `TextEntry` by default, or the context passed in (the console field holds `Console`).
- `Assets/Scripts/SS3D/Systems/Inputs/InputSubSystem.Keybinds.cs` — player rebinding ([2026-10_player-settings](../2026-10_player-settings.md) Phase 3): `BuildKeybindRows` (the Controls tab's fixed visible list; every other live binding becomes a hidden read-only row, so it is reserved), `Rebind`, override save/load/reset and `CaptureNextButton`. Pure conflict/swap rules: `KeybindRules.cs` (`KeybindRulesTests`).

## Extension points

- **Need input while some UI/state is active?** Add a value to `InputContext` (higher value = higher
  priority) and a matching `InputContextDefinition` entry in `InputSubSystem.BuildContexts()`. Push it
  from the owner and dispose on teardown.
- **Need to temporarily block a key/map?** Use `SuppressBinding` / `SuppressMap` / `SuppressAction`
  and dispose the handle when done. Prefer disposing in `OnDisabled`/`OnDestroyed` so a missed
  pointer-exit or early disable can never strand the suppression.
- **New runtime UI Toolkit panel that should block world clicks?** Call
  `InputInterface.RegisterDocument` in setup and `UnregisterDocument` in teardown — unless the document is a
  shared one owned by `UiShellSubSystem` ([ui-shell](ui-shell.md)), in which case register (idempotent) but don't
  unregister on your own surface's teardown, since other surfaces sharing that document still need it registered.
- **Need a one-off debug/UI chord without regenerating `Controls.cs`?** Add a code-defined action on
  `InputSubSystem`'s `System` map (see `UiCancel`, `ToggleIntent`, `OpenLocalSpeechCompose`,
  `ToggleDebugMenu`), include it in the contexts that should enable it, and subscribe to `performed`.
  Prefer a console command first for debug; hotkey panels are debt — [TECH_DEBT.md](../TECH_DEBT.md) §1.13.
- **Tab ownership:** `ToggleRoundSummary` owns Tab outside text entry; compose channel cycle owns it
  while drafting (Keyboard poll in `LocalSpeechBubbleController`, under `TextEntry`). Legacy `Other/Fade`
  and `Spawn Cans` Tab bindings, and unused `Other/Attack` + `Show Owner` (F), are erased at startup.

### Conventions

- Never call `InputAction.Enable/Disable` directly; go through a context or suppression handle.
- A handle must be owned by exactly one object and disposed once; disposing is idempotent and
  order-independent, so out-of-order release across objects is safe.
- Do not bind debug views to gameplay letters (E/C/F/Q/X). Check the default scheme doc first.

## Pitfalls

- **"Is the Escape menu open?" is `IsContextPushed(InputContext.Menu)`**, with `ContextsChanged` to react. Ask whether a context is *pushed*, not whether it is active: a higher context (TextEntry while typing in the menu's chat) would otherwise flip the answer. Main HUD and the storage panel hide on it.
- **Debug tools get no key.** Register with `DebugTools.Get(DebugToolNames.X)` (`Invoked += …`, `IsOn = …` for a toggle) and the F1 debug window lists it (editor/dev builds only). A per-tool key would be reserved away from players and was the reason the old H/P/I/L/M/Y/F2–F6 keys were removed.
- **Key capture must keep its mask until the key is released.** `onAnyButtonPress` runs inside the press's own event; dropping the TextEntry context there re-enables actions in time for that same press to fire them (Escape closed the menu).
- **Rebind conflicts come from the context table.** Two rows conflict when some `InputContextDefinition` enables both (a bit per context in `KeybindRow.ContextMask`). Adding an action to a context therefore changes which keys players may share; Tile Creator conflicts with Movement/Camera (TileMenu runs them together) but not with Hotkeys/Interactions.
- **A new bound action is reserved automatically.** Anything not in `BuildKeybindRows`' visible list but with a binding (console, debug toggles, hacks) blocks players from taking its key. To make it rebindable, add it to the list; to free the key, unbind the action.
- **Key capture uses `InputSystem.onAnyButtonPress` under `InputContext.TextEntry`, not `PerformInteractiveRebinding`.** The latter needs the action disabled, which the arbiter would undo on its next recompute. Accepted: keyboard keys and mouse middle/forward/back; Escape cancels.
- **One key per action:** rebinding overrides the action's other plain bindings to empty (`ApplyBindingOverride(i, "")`), e.g. Square Drag loses right Shift.

- **`EventSystem.IsPointerOverGameObject()` + UI Toolkit:** the UITK `PanelRaycaster` reports the whole
  `UIDocument` as a hit, including `PickingMode.Ignore` layout roots. Fullscreen shells (map editor)
  then look like invisible UI and cancel world placement on mouse-up. `InputInterface` uses
  `panel.Pick` for UITK (honours Ignore) and only `GraphicRaycaster` hits for uGUI — never the
  parameterless `IsPointerOverGameObject()` shortcut.
- **Registered `UIDocument` root steals picks:** the document root defaults to `PickingMode.Position` and
  often fills the screen (Main HUD). Even with content `display:none` / Ignore children, `panel.Pick`
  hits the root. `RegisterDocument` forces the root to `Ignore`; content must opt in with Position.
- **UITK pick Y is top-left, mouse is bottom-left:** before `RuntimePanelUtils.ScreenToPanel` / `panel.Pick`,
  flip with `y = Screen.height - y` (or use `InputInterface.ScreenToPanel`). Skipping the flip maps the
  top of the screen onto bottom chrome (map editor Object Library) — an invisible full-width block in the
  upper half, while the lower UI appears click-through.
- **Every UITK `TextField` needs `InputTextEntryScope`:** `Global` always enables `TileCreator/ToggleMenu`
  (B = map editor) and `Other` (Escape), so an unscoped field types straight into keybinds. Also `Exit` when the
  view rebuilds or hides: a focused field removed from the panel gets no `FocusOut` (lobby shell/creator do both).
- **Escape owners:** `Other/Toggle Menu` = in-round lobby menu (`LobbyUiSubSystem`, `Menu` context);
  `UiCancel` = machine panel; armed interactions poll the key and win (the lobby menu skips while armed);
  the bug report dialog pushes `InputContext.Dialog` (every action off) and polls Escape / Ctrl+Enter itself.
- **Console context follows field focus, not Open/Close:** `ConsolePanelView` holds `InputContext.Console` through an `InputTextEntryScope` on the field's select/deselect, and F12 `Open` toggles. Open and Close share F12 inside `Console`, so a press-scoped handle could fire both in either order and leave the console open over a live Gameplay context (T/B opened chat or the map editor, #154).
- **Regenerating `Controls.cs`:** re-apply Caps / Backspace (and startup erasures, including the retired F6 debug binding) if the generator
  restores old paths — asset + generated wrapper must match [2026-07_default-input-scheme.md](../../archive/architecture/2026-07_default-input-scheme.md).

## Tests

- EditMode: `Assets/Scripts/Tests/EditMode/InputArbiterTests.cs`,
  `Assets/Scripts/Tests/EditMode/InputInterfaceTests.cs`; rebinding conflicts/swaps in `KeybindRulesTests.cs` and named debug registration in `DebugToolsTests.cs`.

## Depends on / Used by

- **Used by:** [player-control](player-control.md), [interactions-runtime](interactions-runtime.md),
  [machine-interface](machine-interface.md), [chat-audio-screens](chat-audio-screens.md),
  [tile](tile.md), [examine](examine.md), [ingame-console](ingame-console.md), [inventory](inventory.md),
  [entities](entities.md) (intent toggle), [rounds-lobby](rounds-lobby.md) (Escape menu, `Menu` context;
  round summary Tab), [ui-shell](ui-shell.md) (bug report F8, `Dialog` context)

## Related docs

- [2026-07_default-input-scheme.md](../../archive/architecture/2026-07_default-input-scheme.md)
- [2026-07_input-arbitration.md](../../archive/architecture/2026-07_input-arbitration.md)
- [INDEX.md](../INDEX.md)
