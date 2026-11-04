> Implements: Documents/design/audio.md §7 (volume categories), §10 (levels are a settings concern)
> Touches systems: rounds-lobby, ui-shell, audio, main-hud (panel scale), rendering (URP quality assets), input
> Status: in-progress

**Progress:** Phase 1 shipped 2026-10-31 (model + store, boot apply, mixer routing, Sound tab). Phase 2 shipped 2026-10-31 (display, quality tiers, upscaling, UI Scale; resolution verified in a build). T6 done. Phase 3 shipped 2026-10-31 (Controls rebinding, F1 debug window, in-round menu blur + HUD hide). Phase 4 not started. Phases 1–2 count toward [test-server.md](../milestones/test-server.md) T6. Phase 3 is polish outside the gates.

# Player settings: display, audio, controls (Oct 2026)

The player **Settings** tab in the lobby shell also serves as the in-round Escape menu. Display, Audio and Controls now apply and persist through `PlayerSettingsSubSystem`; Phases 1–3 are shipped, with the Phase 4 verification checklist still open. The starting-state inventory below records the pre-implementation mockup. Scope was settled on 2026-10-31.

No design doc covers player settings: `main-hud.md` lists settings menus as out of scope, and `audio.md` §10 treats volume levels as a settings concern. The only design premise is audio.md §7's four volume categories. Brightness is deliberately left out (see Decisions).

This effort **absorbs** [2026-07_audio-foundation](2026-07_audio-foundation.md) Phase 0 (mixer groups and routing) and Phase 5's slider binding. audio-foundation keeps the lobby music bed.

## Starting state (2026-10-31)

- `LobbyShellView` holds settings in view-owned dictionaries (`_settingsValues`, `_keybinds`, `_resolution`, `_fullscreen`, `_muted`). The view applies nothing and persists nothing.
- The resolution list is hardcoded to four entries. Fullscreen is a plain bool. A Brightness slider exists. The 8 keybind rows are placeholders (Jump, Inventory…) that don't match `Assets/Content/Systems/Input/Controls.inputactions`.
- That asset has **no control schemes** and only keyboard/mouse bindings, across 7 maps: Camera, Movement, Console, Hotkeys, Other, Tile Creator and Interactions. Some actions are debug-only (Spawn Cans, Swap Minds, Toggle Atmos Debug, Toggle Selection Debug, Toggle Runtime Editor). No rebinding or override code exists.
- `MainMixer.mixer` has Master, SFX, Music and Ambience groups but exposes **no volume parameters**; the only exposed parameters are the SFX muffle ones. There's no Personal group. Ambience and Personal `AudioSource`s output to Master (audio-foundation's known gap).
- All six quality levels (Very Low through Ultra; Ultra is the default) point at the single `Assets/Settings/URP/SS3D_URPAsset.asset`. URP ignores the built-in shadow, pixel-light and AA fields, so the levels differ only in LOD bias, texture mip limit, anisotropic filtering, realtime reflection probes and VSync. The URP asset itself is fixed at high cost: 450 m shadow distance, 4 cascades, 2048 maps, high soft shadows, 8 lights per object, MSAA off, render scale 1, upscaling filter Auto.
- Local persistence precedent: `CharacterProfileStore` writes `persistentDataPath/Lobby/characters.json`, with a path override for pilots.

## Decisions

| # | Decision |
|---|---|
| 1 | Tabs: **Display, Audio, Controls**. Interface and Accessibility are parked. |
| 2 | Storage is client-local JSON at `persistentDataPath/Settings/settings.json`, with a `version` field. Keybinds are stored as `SaveBindingOverridesAsJson` output in the same file. Not server-side: `persistence-save.md`'s player layer covers playtime and lobby preferences, not machine options. |
| 3 | Audio and keybinds apply immediately. Resolution and window mode apply immediately, then show a **15 s "Keep these settings?"** prompt that reverts unless the player confirms. There is no global Apply button. |
| 4 | Audio: **Master, Music, SFX, Ambience, Personal** sliders, plus **Mute All** and **Mute when unfocused** (default on). |
| 5 | Display: resolution from `Screen.resolutions`; **Fullscreen / Borderless / Windowed**; VSync; frame cap (Off / 30 / 60 / 120 / 144 / 240); quality preset; render scale; upscaler; anti-aliasing; UI Scale. **Brightness is dropped** so darkness keeps its gameplay meaning (owner's call). |
| 6 | Quality presets are **Low / Medium / High / Ultra**, **one URP asset per tier**: shadow distance and resolution, cascades, soft-shadow quality, additional-light shadows and per-object light limit, plus each level's LOD, mip and anisotropic fields. VSync and frame cap are re-applied after every tier switch, because each quality level carries its own `vSyncCount`. |
| 7 | Upscaling and AA stay outside the presets. Render scale is 100 / 77 / 67 / 50 %. The upscaler applies below 100 %: **STP** (default; needs SM5 compute, falls back to FSR 1 if unsupported), FSR 1 or Bilinear. AA is Off / FXAA / SMAA / TAA, shown locked while STP is active (STP forces TAA). DLSS and FSR 2/3 are parked: URP 17.3 has no built-in support, so they would need a third-party plugin. |
| 8 | UI Scale is 75–150 % in 5 % steps and applies when the slider is released. It affects the screen-space panels: lobby shell (`Assets/UI Toolkit/PanelSettings.asset`), HUD overlay and comms overlay. The machine interface is excluded because it's diegetic; confirm whether its panel is screen-space before excluding it. |
| 9 | Controls are **keyboard and mouse only**. Rebindable: Movement (four rows: Forward/Back/Left/Right), Toggle Run, Camera, Hotkeys, Interactions, chat and Attack, plus a **Map Editor** group (Tile Creator). Debug and Console actions are hidden. Headings follow the action map names. |
| 10 | Read-only rows: the `main-hud.md` §7 intent modifiers (Ctrl/Alt/Shift; their hints print literal key names), Toggle Menu (Escape), and Primary/Secondary Click. For modifier+key bindings only the key is rebindable; the modifier shows as a fixed prefix. |
| 11 | One key per action, no alternate slot and no unbinding. A conflict within maps that can be active together **swaps** the two bindings and shows a "Use is now Q" notice. Maps that are never active together (Tile Creator vs gameplay) may share keys. Escape cancels capture. Mouse buttons 3–5 can be bound. |
| 12 | ~~Mouse **sensitivity** and **invert vertical** are processor overrides on the Camera actions.~~ Dropped in Phase 3 (no mouse-look; see Phase 3 notes). Zoom speed is parked. |
| 13 | Each tab gets a **Reset to defaults** button with confirmation. There is no per-row reset. |
| 14 | First-launch defaults: native resolution, Borderless, VSync on, **High**, 100 % render scale, SMAA, all volumes 80 %, mute-when-unfocused on. Volume values are placeholders; balancing them is out of scope (audio.md §10). |
| 15 | No Settings entry in the launcher yet. Boot-time apply means saved settings are already in effect before the player connects. |
| 16 | A corrupt file is copied to `settings.json.bak`, defaults load, and a warning is logged. Missing keys get their defaults; unknown keys are ignored. Overrides for removed actions are dropped by the Input System. |

## Shape

- **`PlayerSettingsSubSystem`** (client-only, not networked) owns a plain `PlayerSettings` model. Loading and saving go through a store with the same path-override pattern as `CharacterProfileStore`. The subsystem applies the settings **at boot, before the intro and lobby**, and saves after a short debounce. It does nothing on a dedicated or headless server. Apply logic is split per area: audio (mixer), display (`Screen`, `QualitySettings`, URP camera/asset), input (binding overrides, processors) and UI scale (PanelSettings).
- **`LobbyShellView`** raises "set X" intents and rebuilds the tab from the model. Its settings dictionaries are deleted. This follows the `ServerSettings` / `LobbyServerSettingsView` split ([rounds-lobby](systems/rounds-lobby.md)).
- Conflict detection and swapping is pure logic over a binding table, so it can be tested without the Input System runtime.
- Design premises are respected: audio-foundation's one-clip-loader rule, agent-first composition's asset-over-code rule (quality tiers are URP assets), and the UiShell view/subsystem split.

## Phases

### Phase 1 — Model, persistence, boot apply, audio *(T6)* — **shipped**

Shipped notes (2026-10-31):

- Code: `Assets/Scripts/SS3D/Systems/Settings/` (`PlayerSettings`, `PlayerSettingsStore`, `PlayerAudioMix`, `PlayerSettingsSubSystem`), created first in `SystemsBootstrap.EnsureProcessWideServices`. Tests: `PlayerSettingsStoreTests`, `PlayerAudioMixTests` (EditMode).
- The runtime-loadable mixer reference is the mixer itself, moved (GUID kept) to `Assets/Content/Systems/Audio/Resources/MainMixer.mixer` and read through `SS3D.Systems.Audio.MainAudioMixer`. Exposed: `MasterVolume`, `MusicVolume`, `SFXVolume`, `AmbienceVolume`, `PersonalVolume`.
- Also routed: `FootstepAudio`'s code-built source → SFX. Still on Master: `CommsFeedController`'s announce source (category undecided).
- Mute All and Mute-when-unfocused act on `MasterVolume` only; the five slider values stay as saved.
- Reset to defaults is a two-click button (the first click turns it into Confirm reset).
- The Display and Keyboard sub-tabs keep their view-local mockup state until Phases 2–3; only audio moved into the model.

- Write the `PlayerSettings` model and JSON store test-first (`tdd`): round-trip, version field, corrupt-file → `.bak` + defaults, missing and unknown keys.
- `PlayerSettingsSubSystem`: load and apply at boot, debounced save, skipped when headless.
- Mixer (absorbs audio-foundation Phase 0): add a **Personal** group and expose volume parameters for Master, Music, SFX, Ambience and Personal. Route `AmbienceSubSystem` and `PersonalAudioSubSystem` sources to their groups through a runtime-loadable mixer reference (closes audio-foundation's "outputs to Master" gap). The SFX/Music pool prefabs already target their groups; verify.
- Sound tab: five sliders, Mute All, mute-when-unfocused. Convert linear values to dB, with 0 mapped to −80.
- Move `LobbyShellView`'s settings state into the model.

### Phase 2 — Display, quality, upscaling *(T6)* — **shipped**

Implementation notes (2026-10-31):

- Pure rules, test-first: `PlayerDisplaySettings` (defaults, snapping to offered choices), `PlayerDisplayPlan` (VSync vs frame cap; render scale → upscaler; STP forces and locks TAA; STP → FSR 1 fallback via `STP.IsSupported()`), `PlayerResolutions`, `DisplayChangeConfirmation` (15 s; a second change while pending still reverts to the last confirmed state). Engine writes live in `PlayerDisplayApplier`.
- Quality: four levels, each its own `SS3D_URPAsset_<Tier>.asset` (the old asset is Ultra, GUID kept). Tiers vary shadow distance (40/80/150/450 m), cascades (1/2/4/4), main shadow map (1024/2048/2048/4096), soft shadows (off/Low/Medium/High) and additional-light shadows (off/1024/2048/4096); the old Low/Medium/High/Ultra levels' LOD, mip and anisotropic fields carry over. The per-object light limit is set (2/4/8/8) but Forward+ ignores it.
- An unconfirmed resolution/window change is never written to disk: saves while the prompt is pending store the last confirmed values. In the editor, `Screen.SetResolution` is skipped (the Game view owns its size).
- UI Scale: the screen-space hosts register their `PanelSettings` with `PlayerSettingsSubSystem.RegisterUiPanel`. **Divergence from Decision 8:** the lobby shell, HUD, storage and radial overlays all share `HudOverlayPanelSettings.asset` (not `Assets/UI Toolkit/PanelSettings.asset`, which only the launcher uses), and the machine interface panel is screen-space overlay (no target texture), so it is scaled too. `CommsOverlayPanelSettings.asset` is referenced by nothing.
- **Divergence from Decision 8 (owner's call, 2026-10-31):** UI Scale applies live while dragging, in 1 % steps (not 5 %), instead of on release. The view skips rebuilding the tab for its own UI Scale echo so the drag survives.
- **Show FPS (owner's request, 2026-11-01):** a Display toggle (`PlayerDisplaySettings.ShowFps`, off by default) for a small top-left FPS / frame-time readout, `SS3D.UI.MainHud.Components.FpsCounterView`: process-wide, own `UIDocument` at sorting order 3000, polls the setting and averages frames every 0.5 s (no per-frame work).
- **Show Log Indicator (owner's request, 2026-11-04):** a Display toggle (`PlayerDisplaySettings.ShowLogIndicator`, off by default) for the in-game console's info/warning/error counter, so players without a debugger can opt in to seeing errors. `SS3D.UI.MainHud.Components.LogIndicatorView` is process-wide and checks each frame, because `DebugLogManager.Start` overwrites `PopupEnabled` with its serialized value. Before this the counter was always on. The prefab now starts with `enablePopup: 0` and `startMinimized: 1`; `enablePopup: 0` alone opens the full log window at boot. The console keeps logging while the counter is hidden, so turning it on reports earlier logs, and `` ` `` still opens the log window.
- Frame cap is disabled while VSync is on (VSync paces frames). Sub-tabs are now labelled Display / Audio / Controls (Decision 1).

- Replace the six quality levels with four, each with its own URP asset; default High.
- Display tab per Decisions 5–8: resolution list, window mode with revert prompt, VSync and frame cap re-applied after tier changes, render scale + upscaler + AA with the STP lock and fallback, UI Scale on the screen-space panels.
- Remove the Brightness slider.

### Phase 3 — Controls *(outside the gates)* — **shipped**

Implementation notes (2026-10-31):

- Rows: `InputSubSystem.BuildKeybindRows` (map headings Movement, Camera, Hotkeys, Interactions, plus **General** for the code-defined System actions and **Map Editor** for Tile Creator). Rebindable: the four Movement parts, Toggle Run, camera rotate/snap (Ctrl prefix fixed) and tilt, Swap Hands/Use/Drop, Cancel Interaction, Toggle Help/Harm (the "Attack" of Decision 9: `Other/Attack` is erased at boot and F is `ToggleIntent`), Local Chat (T), Build Menu, Rotate, Replace, Square Drag. Read-only: Menu (Escape), Primary/Secondary Click, Examine (Shift), Disarm/Grab (Ctrl/Alt + click), Mouse Rotation (middle drag), Zoom, plus **Interact / Interaction Menu and Place / Delete**, which are left/right click like Primary/Secondary. `SendChatMessage` (Enter) is unused and hidden; Toggle Internal Clothing has no binding and is hidden.
- Conflicts: co-activity is derived from the `InputContextDefinition` table, not hard-coded. Swaps move every row holding the key (Ctrl+Q holds both Rotate Left and Snap Left). Taking a read-only or hidden-but-live key (Shift, Ctrl, Alt, Escape, Enter, F12 console, F1 debug window) is refused with a toast.
- Capture: `InputSystem.onAnyButtonPress` under `InputContext.TextEntry` instead of `PerformInteractiveRebinding` (see the inputs map Pitfalls). Keyboard and mouse buttons 3–5; Escape or clicking the listening button cancels. The TextEntry context masks Escape's Toggle Menu, so the lobby stays open during capture.
- **Decision 12 dropped (owner's call, 2026-10-31): no mouse sensitivity or invert-Y.** The game has no mouse-look; the cursor is the OS cursor, whose speed the game does not own. The only mouse-to-camera path is middle-drag rotation, and `Mouse Rotation` is a Button action on `delta/x`, so `CameraFollow` gets roughly one delta per drag start and nearly always snaps 90°: a sensitivity slider had nothing smooth to scale. Middle-drag stays as it was. Revisit only if mouse-look or smooth drag rotation is designed.
- Rebind outcomes ("Use is now Q", refusals) show as a toast over the shell; rebuilding the tab keeps the Settings scroll position.
- The capture keeps its key mask until the pressed key is released: `onAnyButtonPress` fires inside the press's own event, so releasing at once let Escape close the menu (and would let a captured B open the build menu).
- **Debug window (owner's call, 2026-10-31):** the scattered debug keys (H health, P atmos, F2 screen effects, F3 speech test line, F4 alert stack, F6 selection buffer, I runtime editor, Y knock-down, L container listing; M swap-minds was already dead) moved into an F1 window (`SS3D.UI.MainHud.Dev.DebugMenuView`) over a named-tool registry (`SS3D.Core.Debugging.DebugTools`, `DebugToolsTests`). Editor and development builds only; F12 stays the console. Their Controls-asset bindings are erased at boot, freeing those letters for players.
- The in-round (Escape) menu blurs the world behind it with the machine interface's backdrop blur (`ScreenEffectsSubSystem.SetUiBackdropBlur`, 0.7), set by `LobbyUiSubSystem` on open/close only. Like a machine panel it also hides the Main HUD and the storage panel: both watch `InputSubSystem.IsContextPushed(InputContext.Menu)` (pushed, not active, so typing in the menu's chat keeps them hidden).
- Overrides persist as two `SaveBindingOverridesAsJson` strings (`Controls` asset and the code-defined System map) and load at boot in `PlayerSettingsSubSystem.OnStart`.

- Controls tab generated from `Controls.inputactions`, using the visible map and action list from Decisions 9–10.
- Interactive rebinding (`PerformInteractiveRebinding`, Escape cancels), conflict swap and notice, overrides saved to the model, sensitivity and invert-Y processors.
- Reset to defaults on each tab (Decision 13; Audio and Display get theirs in Phases 1–2).
- Check that the lobby's text-field keybind masking still holds while a binding is being captured.

### Phase 4 — Verification

- EditMode: model and store (Phase 1), conflict swap (Phase 3).
- Lobby pilot: change one setting per tab, reload the store, and assert the value persisted; assert that the mixer parameter, `QualitySettings` level and binding override actually changed.
- `Testing/ui/validate.sh --steps compile,visual`: screenshots of all three tabs and the revert prompt.
- Then run `update-system-docs` to update the rounds-lobby and audio maps and add pitfalls.

## Parked

DLSS / FSR 2–3 plugins · Interface tab (chat font size, main-hud §7 hint toggle) · Accessibility tab (colorblind palette, shake/flash reduction, subtitles) · monitor selection · gamepad · launcher Settings entry · rebindable intent modifiers (needs main-hud §7 hints to follow bindings) · zoom speed.
