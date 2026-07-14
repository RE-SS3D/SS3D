> Code paths: Assets/Scripts/SS3D/Systems/Health/
> Entry points: HumanHealthController, HealthSimulation
> Status: partial (Phase 0 clean-slate foundation shipped)

# Health

## Overview

Greenfield rewrite in progress per [health_implementation_plan.md](../../plans/health_implementation_plan.md). Legacy layer/organ simulation deleted; new per-zone damage model and normalized systemic pools (`HumanHealthController` + `HealthSimulation`).

## Start here

- `Assets/Scripts/SS3D/Systems/Health/HumanHealthController.cs` — server tick, damage/treatment entry points, snapshot SyncVar
- `Assets/Scripts/SS3D/Systems/Health/HealthSimulation.cs` — pure pool math and critical/death evaluation
- [health-anatomy-map.md](health-anatomy-map.md) — Human.prefab anatomy contract

## Extension points

- `IHealthEffectModifier` — virology/chemistry/stamina deltas (Phase 7+)
- `ApplyDamage(BodyZone, brute, burn)` — combat input (Phase 4: `ZoneTargetResolver`)
- `ApplyTreatment(...)` — medical interactions (Phase 5)

## Depends on / Used by

- **Depends on:** [entities](entities.md)
- **Used by:** [combat](combat.md) (HitInteraction stub), dev console hurt commands
- **Stamina:** relocated to [stamina](../Stamina/) folder — bridge Phase 7a

## Related docs

- Design (read-only): [Documents/design/health.md](../../design/health.md), [stamina.md](../../design/stamina.md), [armor.md](../../design/armor.md)
- Anatomy map: [health-anatomy-map.md](health-anatomy-map.md)
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md)
