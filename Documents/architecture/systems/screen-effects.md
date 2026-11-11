> Code paths: Assets/Scripts/SS3D/Systems/ScreenEffects/
> Entry points: ScreenEffectsSubSystem
> Status: partial
> Verified: 7785df8dc — 2026-11-06

# Screen-space effects

## Overview

Client-only URP Volume overlays for diegetic feedback from [main-hud](../../design/main-hud.md) §5: temperature, fire/freezing, low oxygen, dying/critical, blood-loss tunnel vision, concussion, unconsciousness, plus momentary melee hit flash, blast flash and the station-detonation white-out. Driven by intensity (0..1) via `SetEffect` / `TriggerHitFlash` / `TriggerBlastFlash` / `TriggerWhiteout`. Also hosts `SetUiBackdropBlur`, which drives Dual Kawase fullscreen blur (`UiBackdropBlurContext` → [rendering](rendering.md) `UiBackdropBlurRendererFeature`) for soft world focus behind sharp UI Toolkit overlays — separate from `ScreenEffectType` so health clears do not wipe it. Diegetic panels also paint a dark UITK scrim on the overlay root.

**Health wiring shipped:** local-owner [health](health.md) drives dying/blood-loss/oxy/concussion/unconscious via `HealthScreenEffectMapper`, and hit flash via `HumanHealthController` TargetRpc. While dying/critical is active, LowOxygen screen intensity is attenuated so the red heartbeat blink reads; unconscious uses a world-only flat blackout (no pulsed red veil). Dying pulses Dual Kawase via the same `UiBackdropBlurContext` as machine UI (max of UI vs health channels). Temperature/fire/frost are driven from synced `HealthSnapshot.Environment` via `AtmosScreenEffectMapper` (F1 → Screen Effects / `screeneffect` still for debug solos). Blast flash is fired by [structural-destruction](structural-destruction.md) `BlastVfxPresenter` (distance-gated from the controlled body). It drives a 6 EV full-frame exposure flash with a 40 ms attack and 650 ms recovery, plus a warm-white vignette; health and station white-out keep their own channels.

Bootstraps itself with `RuntimeInitializeOnLoadMethod` (not in Boot.unity) so it can land without scene YAML edits.

**Condemned UI:** Screen Effects debug Canvas (F1 tool) (`ScreenEffectsDebugMenuView`) — do not port to UITK; delete when health rewrite absorbs debug. Volume/effect path stays.

## Start here

- `Assets/Scripts/SS3D/Systems/ScreenEffects/ScreenEffectsSubSystem.cs` — Volume + world blackout + ember/frost particles; `SetEffect` / `TriggerHitFlash` / `TriggerBlastFlash` / `TriggerWhiteout` (post-exposure burn to white plus a white vignette) / `SetUiBackdropBlur`
- `Assets/Scripts/SS3D/Systems/ScreenEffects/StationDetonationPresentation.cs` — station detonation ([2026-11_round-end-loop](../../archive/architecture/2026-11_round-end-loop.md) Decision 19): white-out plus a heavy `CameraFollow.AddImpulse`; the round-end overlay waits while `IsPlaying`
- `Assets/Scripts/SS3D/Systems/ScreenEffects/ScreenEffectType.cs` — sustained effect enum
- `Assets/Scripts/SS3D/Systems/Health/HealthScreenEffectMapper.cs` — `HealthSnapshot` → health-driven intensities
- `Assets/Scripts/SS3D/Systems/ScreenEffects/ScreenEffectsDebugMenuView.cs` — Screen Effects tool opened from F1 (lazy uGUI build)
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/ScreenEffectCommand.cs` — `screeneffect set <effect> <0-1>` / `screeneffect release`
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/ScreenEffectHitFlashCommand.cs` — hit-flash trigger

## Extension points

- Atmos integration: call `SetEffect` for HotRoom/OnFire/ColdRoom/Freezing from temperature/fire state (leave health types alone).
- New sustained effect: add to `ScreenEffectType`, handle in `ScreenEffectsSubSystem` update/composite, expose in debug menu + command usage string.
- UI focus blur: call `SetUiBackdropBlur(0..1)` while a Screen Space Overlay panel is open; clear on close. Strength feeds `UiBackdropBlurContext` (Dual Kawase), not Volume DoF.

## Pitfalls

- **Unconsciousness must black out the world, not menus:** `ScreenEffectsSubSystem` writes `UiBackdropBlurContext.BlackoutIntensity`; the URP backdrop pass blends black after post-processing and before overlay UI. Do not restore the old sorting-order-999 blackout Canvas.
- **Other players wipe your overlays:** `ScreenEffectsSubSystem` is global. Only clear health-driven intensities from a controller that was driving them (`_drivingLocalScreenEffects`); never `Clear` on every non-owner mind change.
- **Do not fold UI blur into `ScreenEffectType`:** `HealthScreenEffectMapper.Clear` zeros health types; UI backdrop blur must stay on the separate `SetUiBackdropBlur` path.
- **Critical washed out by LowOxy + Unconscious:** mapper attenuates LowOxygen under dying; compositor scales oxy vignette. Dying is a hard heartbeat blink (vignette + Dual Kawase), not a soft continuous red. Unconscious blackout stays flat opaque black — do not pulse/tint it with dying (that looked worse than the old ending).
- **Kawase is shared:** `SetUiBackdropBlur` (machine UI) and health dying both write channels; `UiBackdropBlurContext.Intensity = max(ui, health)`. Do not have health call `SetUiBackdropBlur` directly or it will fight panel open/close.
- **Debug panel / screeneffect wiped by health:** live `HealthScreenEffectMapper.Apply` overwrites health channels every snapshot. The Screen Effects panel holds `SetDebugOverrideActive(true)` while open; `screeneffect` engages the same override until `screeneffect release` or panel close. Mappers must no-op Apply/Clear while override is active.
- **Map editor camera carried the player's damage overlay:** the editor camera is not the in-body camera, but health/atmos mappers kept writing intensities every snapshot. `MapEditorSubSystem` now toggles `SetGameplayFeedbackSuppressed`; suppressed frames zero *current* intensities and hide hit/blast/whiteout while keeping authored targets, so effects fade back after close. Do not clear targets on suppression. Hit 2026-11-04 (#126).

## Tests

- `validate.sh --steps scenarios:NukeOps` (`NukeOpsScenarios`) checks the host client plays the detonation white-out.
- `LobbyLifecyclePilot.RunHealthRegressions()` runs a short host scenario for critical item drop/pickup refusal and recovery, blackout with Escape open, ghost FOV and respawn. The full sandbox pilot includes the same checks. Review `sandbox-blackout-menu.png`.

## Depends on / Used by

- **Depends on:** URP Volume stack on the player camera
- **Used by:** [health](health.md) (local-owner snapshot + hit flash + `AtmosScreenEffectMapper` from Environment); [structural-destruction](structural-destruction.md) (blast flash); [machine-interface](machine-interface.md) (diegetic backdrop blur); [rounds-lobby](rounds-lobby.md) (`RoundSubSystem` RPC plays the detonation white-out; round-end overlay waits on it); [tile](tile.md) map editor (`SetGameplayFeedbackSuppressed`); [ingame-console](ingame-console.md) debug commands

## Related docs

- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md) §5
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md) (Phase 6 screen feedback shipped; vitals cluster still open)
- Effort: [2026-07_screen-space-effects.md](../../archive/architecture/2026-07_screen-space-effects.md); turf temp/fire via [2026-07_health-env-feel.md](../../archive/architecture/2026-07_health-env-feel.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
