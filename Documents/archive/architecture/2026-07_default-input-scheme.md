> Implements: Documents/design/main-hud.md §5/§7, Documents/design/examine.md §1/§4, Documents/design/stamina.md §2, Documents/design/comms.md §5 — default key layout (not a new gameplay domain)
> Touches systems: inputs, player-control, interactions-runtime, examine, combat, entities, chat-audio-screens
> Status: shipped

# Default input scheme

Locks the player-facing keyboard defaults after Shift (examine vs run) and C (cancel vs
intent) collided in play. SS13/SS14 conventions plus this fork's design docs are the
baseline; 3D continuous movement and stamina invert SS14's walk-modifier model.

## Decisions (owner, 2026-07-27)

1. **Sprint = Caps Lock toggle** — frees Left Shift for examine; matches existing
   `Movement/Toggle Run` semantics (toggle, not hold). Note: Caps Lock also flips the
   OS key state, so compose/text fields may type uppercase while sprint is "on" until
   Caps is pressed again — acceptable for now; rebind later if it bites playtests.
2. **Help/Harm intent = F** — tg combat-mode muscle memory; frees C.
3. **No Shift+click momentary intent override** — design `main-hud.md` §7 listed it;
   tapping F is enough with a two-state toggle. Shift+click stays examine (character
   paperdoll / closer look).

## Default bindings

Gameplay context only. Debug/admin chords stay on function keys or console — see
[TECH_DEBT.md](../../architecture/TECH_DEBT.md) §1.13. Map editor / console contexts mask these as today
([2026-07_input-arbitration.md](2026-07_input-arbitration.md)).

### Locomotion & camera

| Action | Binding |
|--------|---------|
| Move | WASD |
| Sprint (toggle) | Caps Lock |
| Camera rotate | Middle-mouse drag |
| Camera snap | Ctrl+Q / Ctrl+E |

### Interaction

| Action | Binding |
|--------|---------|
| Primary (intent-resolved) | LMB |
| Radial menu | RMB hold |
| Use / activate / reload path | E |
| Drop | Q |
| Swap hands | X |
| Examine hold-to-peek (object or self) | Shift (hold) |
| Character examine window | Shift+LMB |
| Cancel delayed / armed interaction | Backspace |
| Close machine UI / escape menu | Escape |

Backspace (not Escape) owns cancel so Escape stays free for `Other/Toggle Menu` and
`UiCancel` without opening the lobby while aborting a take/arm.

### Combat / intent

| Action | Binding |
|--------|---------|
| Help ↔ Harm toggle | F |
| Disarm | Ctrl+LMB *(designed; not built)* |
| Grab / escalate | Alt+LMB *(designed; not built)* |
| Momentary opposite-intent click | **Dropped** — use F |

### Comms

| Action | Binding |
|--------|---------|
| Open local compose | T |
| Whisper (in compose) | Shift+Enter |
| Channel radial | hold T *(designed; compose Tab cycle shipped)* |

### Debug (not gameplay letters)

| Action | Binding |
|--------|---------|
| Selection pick shader | F6 |
| Local speech test lines | F3 |
| Alert stack debug | F4 |
| Atmos debug | P |
| Dev console | F12 |
| Map editor | B |

## Design divergences (do not edit design docs)

| Design | This scheme |
|--------|-------------|
| `main-hud.md` §7 Shift+click = momentary intent override | Dropped; Shift+click = examine |
| `main-hud.md` §7 unnamed "tap toggle key" | Bound to **F** |
| `examine.md` unnamed hold key | Bound to **Shift** (SS13/SS14) |
| SS14 Shift = walk (default run) | Inverted: default walk, Caps = sprint (`stamina.md`) |

## Code touchpoints

- `Assets/Content/Systems/Input/Controls.inputactions` (+ generated `Controls.cs`): Caps sprint,
  Backspace cancel, F6 selection debug; unused `Other/Attack` + `Show Owner` F bindings
  erased at `InputSubSystem` startup so F is free.
- `InputSubSystem`: code-defined `ToggleIntent` (F), `DetailedExamine` (Shift), `UiCancel`
  (Escape), compose (T).
- `HumanoidCombatController`: subscribes to `ToggleIntent` (no raw `cKey` poll).
- `IntentModule` hints: F / Ctrl / Alt (no Shift swap).

## Related

- System map: [systems/inputs.md](../../architecture/systems/inputs.md)
- Prior effort: [2026-07_input-arbitration.md](2026-07_input-arbitration.md)
- Design (read-only): [main-hud.md](../../design/main-hud.md) §7, [examine.md](../../design/examine.md),
  [stamina.md](../../design/stamina.md), [comms.md](../../design/comms.md)
