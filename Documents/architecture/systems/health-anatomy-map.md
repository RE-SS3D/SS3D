> Parent map: [health.md](health.md)
> Code paths: Assets/Scripts/SS3D/Systems/Health/
> Status: partial
> Verified: e2f86cf20 — 2026-11-05

# Health anatomy map

## Overview

The `Human.fbx` / `Human.prefab` anatomy contract for the health rewrite: which body parts, zones and organs exist and how severing works. Design authority: [health.md](../../design/health.md).

### Body-part tree (Human.prefab)

| Prefab | Anatomy role | Detachable | Maps to zone(s) |
|--------|--------------|------------|-----------------|
| `HumanTorso` | Root | No | Chest (+ Groin via banding) |
| `HumanHead` | Head | Yes | Head |
| `HumanArmLeft` / `HumanArmRight` | Upper limb | Yes | LeftArm / RightArm |
| `HumanHandLeft` / `HumanHandRight` | Hand | Yes (child of arm) | LeftArm / RightArm |
| `HumanLegLeft` / `HumanLegRight` | Upper leg | Yes | LeftLeg / RightLeg |
| `HumanFootLeft` / `HumanFootRight` | Foot mesh | Part of leg tree | LeftLeg / RightLeg |

Each body-part prefab carries **`AnatomyNode`** (replaces legacy `HumanBodypart` scripts). Phase 5b: severance hides the nested subtree on the character and spawns a world **`Item`** copy via `HumanAnatomyController` + `ItemSubSystem`.

### Severance (Phase 5b)

| Zone | Anatomy root | World drop item |
|------|--------------|-----------------|
| Head | `HumanHead` | `Items.HumanHead` (+ mind-swap to severed head `Entity`) |
| LeftArm / RightArm | `HumanArmLeft/Right` | matching body-part item |
| LeftLeg / RightLeg | `HumanLegLeft/Right` | matching body-part item |

Chest and Groin are not severable. Reattachment deferred to Phase 7c surgery.

Trigger: zone at **Disabled** + sharp melee (`CanSever` on `MeleeDamagePacket`) or admin `sever` command (`force` skips the Disabled check).

### Skeleton colliders → `BodyZone`

Armature colliders in Human.prefab carry **`ZoneTargetCollider`**:

| Collider bone | `BodyZone` |
|---------------|------------|
| `head` | Head |
| `chest`, `spine` | Chest |
| `BodyColliderArm_l`, `upper_arm_l`, `forearm_l`, `hand_l` | LeftArm |
| `BodyColliderArm_r`, `forearm_r`, `hand_r` | RightArm |
| `BodyColliderLeg_l`, `lower_leg_l`, `thigh_l`, `foot_l` | LeftLeg |
| `BodyColliderLeg_r`, `thigh_r`, `foot_r` | RightLeg |

**Groin zone:** `BodyZone.Groin` has **no dedicated collider**. Lower-chest/spine hits resolve to Groin via `ZoneTargetResolver` vertical banding (`GroinTorsoBandFraction` = 0.35) — shipped Phase 4.

Physics layer: **`BodyParts`** (name-based via `HealthLayers`; index may differ per project) for combat/medical raycast.

### Organs

| Organ | FBX mesh | Prefab | Phase | Gameplay role |
|-------|----------|--------|-------|---------------|
| Brain | Yes | `HumanBrain` | 2 | Consciousness; sole death trigger |
| Heart | Yes | `HumanHeart` | 2 | Circulation; cardiac arrest at 0% |
| Lungs ×2 | Yes | `HumanLungLeft/Right` | 2 | O2 intake |
| Liver | Yes | `HumanLiver` | 2 (shipped) | Toxin clearance; inline on Human.prefab + item prefab |
| Kidneys | **No mesh** | **No prefab** | interim | Renal clearance derived from liver until art ships |
| Eyes ×2 | Yes | `HumanEye` | 7+ | Vision — deferred |
| Stomach | Yes | `HumanStomach` | deferred | Hunger (existing alert) |
| Intestines, Appendix | Yes | Yes | deferred | None in v1 |

Each wired organ prefab carries **`OrganInstance`** (replaces legacy `Heart`/`Lungs`/`Brain` pulse scripts).

Surgical access: chest (heart, lungs, liver); head (brain) per [surgery.md](../../design/surgery.md) §2.

## Start here

Files are under `Assets/Scripts/SS3D/Systems/Health/`; prefabs under `Assets/Content/WorldObjects/Entities/Humanoids/Human/` (`HumanBodyParts/`, `HumanOrgans/`).

| File | Role |
|------|------|
| `HumanHealthController.cs` | Server 1 Hz tick, zone damage, pools, snapshot SyncVar |
| `HealthSimulation.cs` | Pure pool math + critical/death checks (EditMode tested) |
| `ZoneTargetCollider.cs` | Collider → zone mapping |
| `AnatomyNode.cs` | Body-part tree anchor; severed visual state |
| `HumanAnatomyController.cs` | Zone → anatomy map, sever visuals, world drops, head mind-swap |
| `OrganInstance.cs` | Organ registration |
| `WoundVfx.cs` | Bleed VFX: bone anchors; intensity/cadence from snapshot bleed rates |

## Extension points

### Asset gaps

| Gap | V1 approach |
|-----|-------------|
| Kidneys missing from FBX | Renal clearance = liver function × `RenalClearanceFactor` (0.5), added in `HealthSimulation.LiverClearance` |
| Ears not wired in Human.prefab | Out of scope v1 |
| Wound blendshapes on FBX | Bleed particles + material tint first; art pass later |

## Depends on / Used by

- **Depends on:** [entities](entities.md) (`Human.prefab` nested body-part prefabs), items (`ItemSubSystem` world drops)
- **Used by:** [health](health.md), [combat](combat.md) (zone raycasts), dev console `sever`

## Related docs

- Plan: [health_implementation_plan.md](../../plans/health_implementation_plan.md)
- Design: [health.md](../../design/health.md)
- HUD zones: [main-hud.md](../../design/main-hud.md) §5–6
