> Code paths: Assets/Scripts/SS3D/Systems/Stamina/
> Entry points: StaminaController, StaminaFactory
> Status: partial
> Verified: 18a9e1421 — 2026-11-03

# Stamina

## Overview

Phase **7a-core** rewrite per [stamina.md](../../design/stamina.md) and health plan Phase 7a. Fast exertion pool with health-modulated regen (heart/lungs/blood), carried-weight encumbrance from [inventory](inventory.md) `HumanInventory.CarriedWeight`, sprint drain via `HumanoidController.OnSpeedChangeEvent`, and push-past-empty → `HumanHealthController.ApplyOxyDebt`. **No permanent stamina bar** — obsolete `StaminaBarView` / PlayerCanvas bar purged.

Actions are **not** hard-locked at zero (`CanCommenceInteraction` / `CanContinueInteraction` always true). Sprint (M1m): only gait above walk drains (`SprintExertion.DrainFraction`, 1 stamina/s at run, so a full pool lasts about 10 s), the owner batches drain into one RPC per 0.2 s, and the server pauses regen until 1 s after the last sprint drain. Movement reads `ExertionPenalty` through `SprintExertion.MovementSpeedFactor` — full speed down to 25 % stamina, then a ramp to 0.55× at empty — in `HumanoidLivingController` / `HumanoidPredictedMovement`. The linear penalty still feeds combat: — per combat plan Phase 4 — to combat performance: `RangedWeaponProfile.ExhaustionSpreadDegrees` widens the ranged accuracy cone and `MeleeHitInteraction.ComputeExertionTimeMultiplier` (up to 1.6x) lengthens melee windup/recovery. Ranged fire and melee swings both drain via `StaminaController.ServerDepleteStamina` (`RangedWeaponProfile.StaminaCost` / `MeleeWeaponProfile.StaminaCost`). Block stamina drain and dedicated winded screen FX are still deferred (block itself has no interaction yet — combat plan Phase 6, optional for MVP).

## Start here

- `Assets/Scripts/SS3D/Systems/Stamina/StaminaController.cs` — networking, modifiers, overdraw → oxy
- `Assets/Scripts/SS3D/Systems/Stamina/Stamina.cs` / `IStamina.cs` — pool math
- `Assets/Scripts/SS3D/Systems/Stamina/StaminaFactory.cs` — defaults
- `Assets/Scripts/SS3D/Systems/Stamina/SprintExertion.cs` — sprint drain, regen delay and movement slowdown curve (pure, EditMode-tested in `SprintExertionTests`)
- `Assets/Scripts/SS3D/Systems/Health/HumanHealthController.cs` — `ApplyOxyDebt` / `ApplyOxyRelief`

## Extension points

- Combat drains: melee swings and ranged fire both call `ServerDepleteStamina` via their profile's `StaminaCost` ([combat](combat.md)); block drain still deferred (no block interaction exists yet).
- Compact HUD indicator near vitals: Main HUD / Phase 6 — do not revive `StaminaBarView`.
- Personal breathing cue: [audio](audio.md) via `StaminaPersonalAudioMapper` (`PersonalAudioSubSystem.SetStaminaBreathingIntensity`), driven from `StaminaController.SyncCurrentStamina`'s `IsOwner` gate — max-merged with health labored breathing.

## Pitfalls

- **Do not reintroduce a permanent stamina bar** on PlayerCanvas — design forbids permanent chrome; oxy/exhaustion feedback rides health screen-effects + future vitals.
- **Regen delay is sprint-only:** `CmdDepleteStaminaScaled` resets the regen timer; combat `ServerDepleteStamina` does not, so swings and shots do not pause regen. Change both together if that rule widens.
- **Movement slowdown is not the linear penalty:** reticle bloom and melee timing scale with `ExertionPenalty` from the first point spent; movement must go through `SprintExertion.MovementSpeedFactor`, or the "sluggish at half stamina" feel returns.
- **Modifier refresh is server-tick:** `CarriedWeight` and organ function are read each server update — clients see synced `CurrentStamina` / `ExertionPenalty` SyncVars.

## Depends on / Used by

- **Depends on:** [health](health.md), [inventory](inventory.md), [entities](entities.md), [audio](audio.md) (`PersonalAudioSubSystem`)
- **Used by:** [interactions-runtime](interactions-runtime.md) (`Hand` gates — currently always allow), [combat](combat.md) (swing/fire drains + exertion feedback into windup/cone), movement controllers, Main HUD ranged reticle bloom (`MainHudSubSystem.GetSelectedRangedBloom01`)

## Related docs

- Design (read-only): [stamina.md](../../design/stamina.md), [inventory-storage.md](../../design/inventory-storage.md) §10, [armor.md](../../design/armor.md) §4
- [health_implementation_plan.md](../../plans/health_implementation_plan.md) Phase 7a
- [INDEX.md](../INDEX.md)
