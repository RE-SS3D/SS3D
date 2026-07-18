> Code paths: Assets/Scripts/SS3D/Systems/Stamina/
> Entry points: StaminaController, StaminaFactory
> Status: partial
> Verified: 9145f200f — 2026-07-18

# Stamina

## Overview

Phase **7a-core** rewrite per [stamina.md](../../design/stamina.md) and health plan Phase 7a. Fast exertion pool with health-modulated regen (heart/lungs/blood), carried-weight encumbrance from [inventory](inventory.md) `HumanInventory.CarriedWeight`, sprint drain via `HumanoidController.OnSpeedChangeEvent`, and push-past-empty → `HumanHealthController.ApplyOxyDebt`. **No permanent stamina bar** — obsolete `StaminaBarView` / PlayerCanvas bar purged.

Actions are **not** hard-locked at zero (`CanCommenceInteraction` / `CanContinueInteraction` always true). Exhaustion applies `ExertionPenalty` (0..1) to movement in `HumanoidLivingController` / `HumanoidPredictedMovement`. Combat swing/block/fire costs and dedicated winded screen FX are deferred.

## Start here

- `Assets/Scripts/SS3D/Systems/Stamina/StaminaController.cs` — networking, modifiers, overdraw → oxy
- `Assets/Scripts/SS3D/Systems/Stamina/Stamina.cs` / `IStamina.cs` — pool math
- `Assets/Scripts/SS3D/Systems/Stamina/StaminaFactory.cs` — defaults
- `Assets/Scripts/SS3D/Systems/Health/HumanHealthController.cs` — `ApplyOxyDebt` / `ApplyOxyRelief`

## Extension points

- Combat drains: call `ServerDepleteStamina` / existing consume path from combat verbs (deferred).
- Compact HUD indicator near vitals: Main HUD / Phase 6 — do not revive `StaminaBarView`.

## Pitfalls

- **Do not reintroduce a permanent stamina bar** on PlayerCanvas — design forbids permanent chrome; oxy/exhaustion feedback rides health screen-effects + future vitals.
- **Modifier refresh is server-tick:** `CarriedWeight` and organ function are read each server update — clients see synced `CurrentStamina` / `ExertionPenalty` SyncVars.

## Depends on / Used by

- **Depends on:** [health](health.md), [inventory](inventory.md), [entities](entities.md)
- **Used by:** [interactions-runtime](interactions-runtime.md) (`Hand` gates — currently always allow), movement controllers

## Related docs

- Design (read-only): [stamina.md](../../design/stamina.md), [inventory-storage.md](../../design/inventory-storage.md) §10, [armor.md](../../design/armor.md) §4
- [health_implementation_plan.md](../../plans/health_implementation_plan.md) Phase 7a
- [INDEX.md](../INDEX.md)
