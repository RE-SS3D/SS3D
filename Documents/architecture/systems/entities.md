> Code paths: Assets/Scripts/SS3D/Systems/Entities/
> Entry points: EntitySubSystem, MindSubSystem, HumanoidBodyStateMachine
> Status: partial
> Verified: d6d269dea — 2026-07-17

# Entities

## Overview

Humanoid/silicon entity spawning, minds, and join/round ordering with [rounds-lobby](rounds-lobby.md). Humanoid body animation is driven by a packed `BodyAnimationSnapshot` SyncVar, not ad-hoc Animator calls.

**Prefab composition debt:** `Human.prefab` is a mega-prefab (~15k lines, ~120 script refs). Do not hand-add features on it. Target is a thin visual/network anchor; health Phase 0d is strip-and-rewire, not grow. See [2026-07_agent-first-composition.md](../2026-07_agent-first-composition.md).

**Body presentation debt:** collapse / death / ragdoll / walk-cycle ownership is fragmented across Health, `Ragdoll`, `AnimationOrchestrator`, body-state bridge, and movement. Interim collapse APIs exist; **do not add another path** — refactor per [2026-07_body-presentation-authority.md](../2026-07_body-presentation-authority.md).

## Start here

- `Assets/Scripts/SS3D/Systems/Entities/EntitySubSystem.cs` — entity spawn/management
- `Assets/Scripts/SS3D/Systems/Entities/MindSubSystem.cs` — mind/player mind assignment
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/HumanoidBodyStateMachine.cs` — authoritative body/combat snapshot
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/AnimationOrchestrator.cs` — snapshot → Animator (`CombatStance`, `VelX`/`VelZ`); `SetPosingSuppressed` for collapse
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Ragdoll.cs` — knockdown / death collapse visuals (`ApplyCollapseVisuals`)
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/HumanoidIkController.cs` — combat look-at IK (aim yaw/pitch)
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/HumanoidBodyStateBridge.cs` — inventory → arm hold + combat stance (Melee/Ranged); must not pose while collapsed
- `Assets/Content/WorldObjects/Entities/Humanoids/Human/HumanCharacterAnimator.controller` — Peaceful/Melee/Ranged locomotion blends

## Extension points

- Combat stance packs: Peaceful (Locomotion Pack), Melee (Pro Melee Axe), Ranged (Basic Shooter). Rebuild with **SS3D → Animation → Rebuild Combat Stance Blend Trees**.
- `HumanoidCombatMode` is 2 bits in the snapshot (`Peaceful` / `Melee` / `Ranged`); `C` toggles Peaceful ↔ inventory-derived combat stance.
- **Animator vs code:** blend trees, transitions, and masks are animator-owned (see [player-body-animation](../2026-07_player-body-animation.md) “Who tunes what”). Code only sets parameters, fires triggers, and applies look-at IK — do not add new motion timing in C# when the controller can own it.
- **Collapse / death:** go through `Ragdoll.ApplyCollapseVisuals` / death reinforce RPCs until body-presentation authority ships. Do not gate collapse only on SyncVar `OnChange` or `ServerRpc` from server code.

## Pitfalls

- **Ghost spawn stack-overflow:** `HumanoidGhostController.OnAwake` must call `base.OnAwake()`, never `base.Awake()`.
- **Walk cycle while “collapsed”:** Coimbra `UpdateEvent` keeps firing after `enabled=false`; limp bridge can still publish snapshots. Use `SetPosingSuppressed` + shared collapse visuals — see [body-presentation-authority](../2026-07_body-presentation-authority.md).
- **`Ragdoll.OnDisable` must not `Recover()`:** ownership/network teardown would stand a corpse back into locomotion.

## Depends on / Used by

- **Used by:** [rounds-lobby](rounds-lobby.md), [player-control](player-control.md), [health](health.md)

## Related docs

- [2026-07_body-presentation-authority](../2026-07_body-presentation-authority.md) — **planned** collapse/death presentation refactor
- [2026-07_player-body-animation](../2026-07_player-body-animation.md)
- [2026-07_agent-first-composition](../2026-07_agent-first-composition.md) (prefab debt)
- [animation_system_design plan](../../plans/animation_system_design_250de599.plan.md)
- [INDEX.md](../INDEX.md)
