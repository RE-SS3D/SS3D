> Code paths: Assets/Scripts/SS3D/Systems/ScreenEffects/
> Entry points: ScreenEffectsSubSystem
> Status: partial (health wired; atmos deferred)
> Verified: eecaa7881 — 2026-07-18

# Screen-space effects

## Overview

Client-only URP Volume overlays for diegetic feedback from [main-hud](../../design/main-hud.md) §5: temperature, fire/freezing, low oxygen, dying/critical, blood-loss tunnel vision, concussion, unconsciousness, plus a momentary melee hit flash. Driven by intensity (0..1) via `SetEffect` / `TriggerHitFlash`. Also hosts `SetUiBackdropBlur`, which drives Dual Kawase fullscreen blur (`UiBackdropBlurContext` → [rendering](rendering.md) `UiBackdropBlurRendererFeature`) for soft world focus behind sharp UI Toolkit overlays — separate from `ScreenEffectType` so health clears do not wipe it. Diegetic panels also paint a dark UITK scrim on the overlay root.

**Health wiring shipped:** local-owner [health](health.md) drives dying/blood-loss/oxy/concussion/unconscious via `HealthScreenEffectMapper`, and hit flash via `HumanHealthController` TargetRpc. Temperature/fire/frost remain debug/console-only until atmospherics wires them.

Bootstraps itself with `RuntimeInitializeOnLoadMethod` (not in Boot.unity) so it can land without scene YAML edits.

**Condemned UI:** F2 debug Canvas (`ScreenEffectsDebugMenuView`) — do not port to UITK; delete when health rewrite absorbs debug. Volume/effect path stays.

## Start here

- `Assets/Scripts/SS3D/Systems/ScreenEffects/ScreenEffectsSubSystem.cs` — Volume + blackout + ember/frost particles; `SetEffect` / `TriggerHitFlash` / `SetUiBackdropBlur`
- `Assets/Scripts/SS3D/Systems/ScreenEffects/ScreenEffectType.cs` — sustained effect enum
- `Assets/Scripts/SS3D/Systems/Health/HealthScreenEffectMapper.cs` — `HealthSnapshot` → health-driven intensities
- `Assets/Scripts/SS3D/Systems/ScreenEffects/ScreenEffectsDebugMenuView.cs` — F2 debug menu (lazy UI build)
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/ScreenEffectCommand.cs` — `screeneffect <type> <0-1>`
- `Assets/Scripts/SS3D/Systems/IngameConsoleSystem/Commands/ScreenEffectHitFlashCommand.cs` — hit-flash trigger

## Extension points

- Atmos integration: call `SetEffect` for HotRoom/OnFire/ColdRoom/Freezing from temperature/fire state (leave health types alone).
- New sustained effect: add to `ScreenEffectType`, handle in `ScreenEffectsSubSystem` update/composite, expose in debug menu + command usage string.
- UI focus blur: call `SetUiBackdropBlur(0..1)` while a Screen Space Overlay panel is open; clear on close. Strength feeds `UiBackdropBlurContext` (Dual Kawase), not Volume DoF.

## Pitfalls

- **Other players wipe your overlays:** `ScreenEffectsSubSystem` is global. Only clear health-driven intensities from a controller that was driving them (`_drivingLocalScreenEffects`); never `Clear` on every non-owner mind change.
- **Do not fold UI blur into `ScreenEffectType`:** `HealthScreenEffectMapper.Clear` zeros health types; UI backdrop blur must stay on the separate `SetUiBackdropBlur` path.

## Depends on / Used by

- **Depends on:** URP Volume stack on the player camera
- **Used by:** [health](health.md) (local-owner snapshot + hit flash); [machine-interface](machine-interface.md) (diegetic backdrop blur); [ingame-console](ingame-console.md) debug commands; future atmospherics

## Related docs

- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md) §5
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md) (Phase 6 screen feedback shipped; vitals cluster still open)
- Effort: [2026-07_screen-space-effects.md](../2026-07_screen-space-effects.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md)
