> Code paths: Assets/Scripts/SS3D/Systems/Combat/
> Entry points: MeleeHitInteraction, MeleeWeaponItemExtension, HandHit
> Status: partial (Phase 4 melee vertical slice; blocking/ranged deferred)

# Combat

## Overview

Phase 4 ships zone-targeted melee hits wired to the health rewrite. Harm-intent targeted hits resolve body zones via raycast on the `BodyParts` layer, apply brute/burn through `HumanHealthController.ApplyDamage`, and enforce per-weapon windup/recovery. Fists and crowbar are the first weapon profiles; full combat model (blocking, ranged accuracy) remains per design spec.

## Start here

- `Assets/Scripts/SS3D/Systems/Combat/Interactions/MeleeHitInteraction.cs` — Harm intent, windup, zone damage, recovery
- `Assets/Scripts/SS3D/Systems/Combat/Interactions/MeleeWeaponItemExtension.cs` — held-item melee (crowbar)
- `Assets/Scripts/SS3D/Systems/Combat/Interactions/HandHit.cs` — empty-hand fists
- `Assets/Scripts/SS3D/Systems/Combat/MeleeWeaponProfile.cs` — damage + timing presets
- `Assets/Scripts/SS3D/Systems/Combat/MeleeRecoveryTracker.cs` — blocks follow-up swings on the hand
- `Assets/Content/WorldObjects/Items/Functional/Tools/Engineering/Crowbar.prefab` — crowbar melee profile

## Extension points

- Add weapons by attaching `MeleeWeaponItemExtension` with a `MeleeWeaponProfile` (or extend profiles in code).
- Combat zone resolution lives in `ZoneTargetResolver.TryResolveCombatZone` (health system); groin uses torso vertical banding.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [health](health.md) (`ApplyDamage`, `ZoneTargetResolver`, `BodyParts` layer)
- **Used by:** player Harm-intent targeted interactions

## Related docs

- Design (read-only): [Documents/design/combat.md](../../design/combat.md)
- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md) Phase 4
