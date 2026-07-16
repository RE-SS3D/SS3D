> Code paths: Assets/Scripts/SS3D/Systems/Entities/
> Entry points: EntitySubSystem, MindSubSystem, HumanoidBodyStateMachine
> Status: partial

# Entities

## Overview

Humanoid/silicon entity spawning, minds, and join/round ordering with [rounds-lobby](rounds-lobby.md). Humanoid body animation is driven by a packed `BodyAnimationSnapshot` SyncVar, not ad-hoc Animator calls.

## Start here

- `Assets/Scripts/SS3D/Systems/Entities/EntitySubSystem.cs` — entity spawn/management
- `Assets/Scripts/SS3D/Systems/Entities/MindSubSystem.cs` — mind/player mind assignment
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/HumanoidBodyStateMachine.cs` — authoritative body/combat snapshot
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/AnimationOrchestrator.cs` — snapshot → Animator (`CombatStance`, `VelX`/`VelZ`)
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/HumanoidIkController.cs` — combat look-at IK (aim yaw/pitch)
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/HumanoidBodyStateBridge.cs` — inventory → arm hold + combat stance (Melee/Ranged)
- `Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanCharacterAnimator.controller` — Peaceful/Melee/Ranged locomotion blends

## Extension points

- Combat stance packs: Peaceful (Locomotion Pack), Melee (Pro Melee Axe), Ranged (Basic Shooter). Rebuild with **SS3D → Animation → Rebuild Combat Stance Blend Trees**.
- `HumanoidCombatMode` is 2 bits in the snapshot (`Peaceful` / `Melee` / `Ranged`); `C` toggles Peaceful ↔ inventory-derived combat stance.
- **Animator vs code:** blend trees, transitions, and masks are animator-owned (see [PLAYER_BODY_ANIMATION.md](../../PLAYER_BODY_ANIMATION.md) “Who tunes what”). Code only sets parameters, fires triggers, and applies look-at IK — do not add new motion timing in C# when the controller can own it.

## Depends on / Used by

- **Used by:** [rounds-lobby](rounds-lobby.md), [player-control](player-control.md), [health](health.md)

## Related docs

- [PLAYER_BODY_ANIMATION.md](../../PLAYER_BODY_ANIMATION.md)
- [animation_system_design plan](../../plans/animation_system_design_250de599.plan.md)
- [INDEX.md](../INDEX.md)
