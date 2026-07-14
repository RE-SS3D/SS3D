> Code paths: Assets/Scripts/SS3D/Systems/Health/
> Entry points: HumanHealthController, HealthSimulation
> Status: partial (Phase 1 bleeding vertical slice shipped)

# Health

## Overview

Greenfield rewrite in progress per [health_implementation_plan.md](../../plans/health_implementation_plan.md). Legacy layer/organ simulation deleted; new per-zone damage model and normalized systemic pools (`HumanHealthController` + `HealthSimulation`). Phase 1 adds wound severity → bleeding, blood drain → oxy debt, bandage treatment, per-zone VFX, and a bleeding HUD alert chip.

## Start here

- `Assets/Scripts/SS3D/Systems/Health/HumanHealthController.cs` — server tick, damage/treatment entry points, snapshot SyncVar, client VFX/HUD hooks
- `Assets/Scripts/SS3D/Systems/Health/HealthSimulation.cs` — pure pool math, severity/bleeding rates, critical/death evaluation
- `Assets/Scripts/SS3D/Systems/Health/Interactions/BandageInteraction.cs` — Help-intent, Tier 2 zone-targeted bandage
- `Assets/Scripts/SS3D/Systems/Health/WoundVfx.cs` — per-zone bleeding particles from snapshot mask
- `Assets/Scripts/SS3D/Systems/Health/HealthAlertsView.cs` — top-right "Bleeding" alert chip (PlayerCanvas)
- [health-anatomy-map.md](health-anatomy-map.md) — Human.prefab anatomy contract

## Extension points

- `IHealthEffectModifier` — virology/chemistry/stamina deltas (Phase 7+)
- `ApplyDamage(BodyZone, brute, burn)` — combat input (Phase 4: full combat raycast)
- `ApplyTreatment(...)` — medical interactions (Phase 5 field treatments)
- `ZoneTargetResolver` — interaction-point zone resolution (Phase 4 expands for combat)

## Depends on / Used by

- **Depends on:** [entities](entities.md), [interactions-framework](interactions-framework.md)
- **Used by:** [combat](combat.md) (HitInteraction stub), dev console hurt commands, MedicalPatch item (bandage)
- **Stamina:** relocated to `Assets/Scripts/SS3D/Systems/Stamina/` — bridge Phase 7a

## Related docs

- Design (read-only): [Documents/design/health.md](../../design/health.md), [main-hud.md](../../design/main-hud.md) §9, [stamina.md](../../design/stamina.md)
- Anatomy map: [health-anatomy-map.md](health-anatomy-map.md)
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md)
