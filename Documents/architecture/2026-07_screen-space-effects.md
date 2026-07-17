> Implements: Documents/design/main-hud.md §5 (screen-space feedback)
> Touches systems: screen-effects, ingame-console, health (wired), atmospherics (integration deferred)
> Status: shipped (foundation + health wiring; atmos deferred)

# Screen-space effects foundation (Jul 2026)

Client overlay system for diegetic screen feedback. Navigation map: [systems/screen-effects.md](systems/screen-effects.md).

## Overview

Shipped a `ScreenEffectsSubSystem` that drives URP Volume effects (vignette, chromatic aberration, color adjustments, depth of field), a full-screen blackout image, and simple fire/frost particle overlays. Sustained states use `ScreenEffectType` intensities; melee hit flash is a one-shot via `TriggerHitFlash`.

## Shipped

- Self-bootstrapping subsystem (runtime init; not Boot.unity)
- Effect types: HotRoom, OnFire, ColdRoom, Freezing, LowOxygen, DyingCritical, BloodLossTunnelVision, Concussion, Unconscious
- F2 debug menu (lazy UI, no duplicate EventSystem)
- Console: `screeneffect`, hit-flash command
- **Health wiring:** `HealthScreenEffectMapper` maps local-owner `HealthSnapshot` → health-driven intensities; `HumanHealthController` TargetRpc fires hit flash on damage

## Deferred

- Drive HotRoom/OnFire/ColdRoom/Freezing from atmospherics
- Optional move of bootstrap into Boot.unity once preferred
