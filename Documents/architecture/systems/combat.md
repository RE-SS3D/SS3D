> Code paths: Assets/Scripts/SS3D/Systems/Combat/, Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/
> Entry points: MeleeHitInteraction, MeleeWeaponItemExtension, HandHit; combat stance via HumanoidCombatController
> Status: partial (Phase 4 melee vertical slice; stance/aim presentation shipped; blocking/ranged deferred)

# Combat

## Overview

Phase 4 ships zone-targeted melee hits wired to the health rewrite. Harm-intent targeted hits resolve body zones via raycast on the `BodyParts` layer, apply brute/burn through `HumanHealthController.ApplyDamage`, and enforce per-weapon windup/recovery. Fists and crowbar are the first weapon profiles; full combat model (blocking, ranged accuracy) remains per design spec.

**Shipped adjacent foundation (presentation):** Peaceful/Melee/Ranged stance locomotion, aim look-at IK, and melee swing triggers live under [entities](entities.md) body animation — see [player-body-animation](../2026-07_player-body-animation.md). Stance presentation and the Phase 4 hit resolution now sit on the same base after the health-rewrite/develop integration; wiring swing telegraph to windup timing is a combat build-out task.

## Start here

- `Assets/Scripts/SS3D/Systems/Combat/Interactions/MeleeHitInteraction.cs` — Harm intent, windup, zone damage, recovery
- `Assets/Scripts/SS3D/Systems/Combat/Interactions/MeleeWeaponItemExtension.cs` — held-item melee (crowbar)
- `Assets/Scripts/SS3D/Systems/Combat/Interactions/HandHit.cs` — empty-hand fists
- `Assets/Scripts/SS3D/Systems/Combat/MeleeWeaponProfile.cs` — damage + timing presets
- `Assets/Scripts/SS3D/Systems/Combat/MeleeRecoveryTracker.cs` — blocks follow-up swings on the hand
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/HumanoidCombatController.cs` — stance / swing triggers (animation side)
- `Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Crowbar.prefab` — crowbar melee profile

## Extension points

- Add weapons by attaching `MeleeWeaponItemExtension` with a `MeleeWeaponProfile` (or extend profiles in code).
- Combat zone resolution lives in `ZoneTargetResolver.TryResolveCombatZone` (health system); groin uses torso vertical banding.
- Reuse the stance packs already on the humanoid animator when building windup/recovery presentation.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [health](health.md) (`ApplyDamage`, `ZoneTargetResolver`, `BodyParts` layer), [entities](entities.md) (stance presentation)
- **Used by:** player Harm-intent targeted interactions

## Related docs

- Design (read-only): [Documents/design/combat.md](../../design/combat.md)
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md) Phase 4
- Effort (stance foundation): [2026-07_player-body-animation.md](../2026-07_player-body-animation.md)
- [entities](entities.md)
