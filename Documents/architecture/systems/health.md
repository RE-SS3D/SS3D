> Code paths: Assets/Scripts/SS3D/Systems/Health/
> Entry points: HumanHealthController, HealthSimulation, OrganSimulation
> Status: partial (Phase 2 organ function shipped)

# Health

## Overview

Greenfield rewrite in progress per [health_implementation_plan.md](../../plans/health_implementation_plan.md). Phase 1 shipped bleeding, bandage, VFX, and alert chip. Phase 2 wires asset-backed organs (brain, heart, lungs, liver) into pool math, cardiac arrest, consciousness/movement debuffs, and limb capability stubs.

## Start here

- `Assets/Scripts/SS3D/Systems/Health/HumanHealthController.cs` — server tick, damage/treatment, organ registration, snapshot SyncVar
- `Assets/Scripts/SS3D/Systems/Health/HealthSimulation.cs` — pool math, severity/bleeding, critical/death evaluation
- `Assets/Scripts/SS3D/Systems/Health/OrganSimulation.cs` — zone→organ damage, organ tick drains, perfusion, limb multipliers
- `Assets/Scripts/SS3D/Systems/Health/OrganInstance.cs` — organ registration on Human prefab / organ item prefabs
- `Assets/Scripts/SS3D/Systems/Health/HealthDebugController.cs` — IMGUI overlay (H) for full zone/organ/pool inspection
- `Assets/Scripts/SS3D/Systems/Health/HealthDebugDetail.cs` — per-zone/per-organ SyncVar payload for debug UI
- [health-anatomy-map.md](health-anatomy-map.md) — anatomy contract

## Extension points

- `IHealthEffectModifier` — virology/chemistry/stamina deltas (Phase 7+)
- `ApplyDamage(BodyZone, brute, burn)` — combat input (Phase 4: full combat raycast)
- `ApplyTreatment(...)` — medical interactions (Phase 5 field treatments)
- `ZoneTargetResolver` — interaction-point zone resolution (Phase 4 expands for combat)

## Depends on / Used by

- **Depends on:** [entities](entities.md), [interactions-framework](interactions-framework.md)
- **Used by:** [combat](combat.md), dev console `hurt`/`heal`, `HumanoidLivingController` (movement/consciousness), `Hand` (arm debuff stub)
- **Stamina:** `Assets/Scripts/SS3D/Systems/Stamina/` — bridge Phase 7a

## Related docs

- Design (read-only): [Documents/design/health.md](../../design/health.md), [main-hud.md](../../design/main-hud.md) §9
- Anatomy map: [health-anatomy-map.md](health-anatomy-map.md)
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md)
