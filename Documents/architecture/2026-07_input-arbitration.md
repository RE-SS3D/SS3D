> Implements: infrastructure — no dedicated design doc; supports the input surfaces in Documents/design/main-hud.md
> Touches systems: inputs, player-control, interactions-runtime, machine-interface, chat-audio-screens, tile, examine, ingame-console, screens
> Status: shipped

# Input arbitration

Rebuild of the input layer around one principle: **input state is derived from a set of owned,
self-releasing requests, never from a shared counter.** Adds a single pointer authority spanning
uGUI and UI Toolkit, and removes the remaining legacy `UnityEngine.Input` polling from gameplay
gates so all input decisions flow through one system.

## Why

The previous `InputSubSystem` kept one global `Dictionary<InputAction,int>` reference count. Any
system could increment/decrement it via `ToggleAction` / `ToggleActionMap` / `ToggleAllActions` /
`ToggleBinding` / `ToggleCollisions`. Because the counter was a single shared integer with no notion
of *who* disabled what, correctness depended on every caller perfectly pairing its enable/disable
calls across async, network-driven lifecycles. In practice this produced:

- **Dead keys** — an unbalanced disable left an action stuck off.
- **Leaked input** — an over-decrement let input pass through UI.
- **Enabled/count desync** — `ForceEnableActionMap` and `HumanoidController.EnsureMovementInputEnabled`
  (a per-frame re-enable from `Update`) existed only to paper over wedged counts, and could convert a
  dead-key bug into a leak elsewhere.

Symptoms: interrupted keys, UI not showing, clicks not registering. Root cause: distributed ownership
of a shared counter, plus four uncoordinated input authorities (the refcount, uGUI `EventSystem`,
UI Toolkit panels, and legacy `UnityEngine.Input`).

## Model

Two request kinds, both returning an `IInputHandle : IDisposable`. Disposing removes exactly that
request; `InputArbiter.Recompute()` is the single writer of `InputAction.enabled`.

- **Context** (coarse/modal): the highest-priority live context is *active* and declares the exact set
  of maps/actions enabled. Ties break by most-recent push.
- **Suppression** (fine): removes specific actions/bindings on top of the active context.

```
enabled(action) = activeContext.Includes(action) AND not suppressed(action)
```

Handles are owned by one object and disposed in `OnDisabled`/`OnDestroyed`, so a missed pointer-exit,
an early disable, or an ownership change can never strand input. Disposing is idempotent and
order-independent.

### Contexts

Priority is the `InputContext` enum value (higher wins). `Global` is pushed once at startup and never
released. Definitions live in `InputSubSystem.BuildContexts()`.

| Context | Priority | Enables |
|---|---|---|
| `Global` | 0 | `Other` map; `Console.Open`; `TileCreator.ToggleMenu` |
| `Gameplay` | 10 | `Movement`, `Camera`, `Interactions`, `Hotkeys`, `Other`; `Console.Open`; `TileCreator.ToggleMenu`; `DetailedExamine` |
| `TileMenu` | 20 | `Movement`, `Camera`, `TileCreator`, `Other`; `Console.Open`; `DetailedExamine` (world interactions/hotkeys dropped) |
| `MachineUI` | 30 | `Hotkeys`, `Interactions`; `UiCancel` (Escape). `Movement`/`Camera`/`Other` masked so Escape closes the panel instead of toggling the lobby |
| `Console` | 40 | `Console` map only |
| `TextEntry` | 50 | nothing (generic field focused; typing goes to the field via uGUI/TMP) |
| `ChatEntry` | 60 | `Other.SendChatMessage` only (chat field focused, so Enter still submits) |

`TileMenu` replaces the old `ToggleCollisions` runtime binding-path matching (including its
`leftShift`/`rightShift` special case) with an explicit map set.

### Suppressions

- Radial menu holds a `<Mouse>/leftButton` suppression while open (`RadialInteractionSubSystem`).
- Pointer-over-UI holds `<Mouse>/scroll/y` (chat, crafting, tile menu) and `TileCreator.Place` (tile
  menu) suppressions, released on pointer exit or component disable.
- Camera transition holds a `Camera` map suppression; the mouse-rotation snap holds a
  `MouseRotation` suppression released by a timeout (both released on disable too).

## Pointer authority

`InputInterface.IsPointerOverInterface()` returns true if the pointer is over any uGUI element
(`EventSystem.IsPointerOverGameObject()`) or any registered, enabled UI Toolkit panel
(`panel.Pick(RuntimePanelUtils.ScreenToPanel(...))` with bottom-left screen pixels — do not
pre-flip Y). `InteractionController` world-click gates and `SelectionCamera` hover clearing both
use it, so examine/outlines/clicks do not target world objects through registered UI Toolkit panels.
Runtime documents register in setup: `RadialInteractionSubSystem`, `ArmedInteractionSubSystem`,
`MachineInterfaceHost`, `MainHudSubSystem`.

## Legacy input removed

- `MachineInterfaceSubSystem` no longer polls `Input.GetKeyDown(KeyCode.Escape)`; it subscribes to the
  arbitrated `UiCancel` action while the `MachineUI` context is active.
- `ExamineUI` no longer polls `Input.GetKey(Shift)`; it reads the arbitrated `DetailedExamine` action.

## Deviation from plan

`UiCancel` (Escape) and `DetailedExamine` (Shift) are defined **in code** as a small `System`
`InputActionMap` in `InputSubSystem`, rather than added to `Controls.inputactions` + regenerating
`Controls.cs`. The Unity Input System code generator was not available in the implementation
environment, and hand-editing the ~2.6k-line generated wrapper plus the embedded asset JSON is
error-prone and unverifiable. Code-defined actions are fully arbitrated (they are enabled/disabled by
the same contexts) and behave identically at runtime. They can be folded back into the asset later
without changing callers.

## API map (old to new)

| Old | New |
|---|---|
| `ToggleActionMap(map, true/false)` | `PushContext(...)` / dispose handle |
| `ToggleAllActions(false)` (generic field) | `PushContext(InputContext.TextEntry)` / dispose (via `InputTextEntryScope`) |
| `ToggleAllActions(false, exclude SendChatMessage)` (chat) | `PushContext(InputContext.ChatEntry)` / dispose (via `InputTextEntryScope`) |
| `ToggleAction(a, false)` | `SuppressAction(a)` / dispose |
| `ToggleBinding(path, false)` | `SuppressBinding(path)` / dispose |
| `ToggleCollisions(...)` | context map set (removed) |
| `ForceEnableActionMap(...)` | removed (no longer needed) |
| `EventSystem...IsPointerOverGameObject()` gate | `InputInterface.IsPointerOverInterface()` |

## Tests

- EditMode: `Assets/Scripts/Tests/EditMode/InputArbiterTests.cs` (priority masking, exact restore on
  dispose, suppression, `SuppressBinding` targeting, same-priority hold, double-dispose safety);
  `Assets/Scripts/Tests/EditMode/InputInterfaceTests.cs` (pointer query null-safety).
- Runtime behaviour (movement, world click gating, opening/closing each modal, chat focus) is verified
  in-editor/PlayMode.

## Related

- System map: [systems/inputs.md](systems/inputs.md)
