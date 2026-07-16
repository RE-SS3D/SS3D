# Player Body & Animation System

Design document resolving [#1060](https://github.com/RE-SS3D/SS3D/issues/1060) and guiding implementation of [#1333](https://github.com/RE-SS3D/SS3D/issues/1333) and [#1246](https://github.com/RE-SS3D/SS3D/issues/1246).

## Overview

The player body system is the central authority for what a humanoid character is doing. Animation is the visual output of **body state + capabilities + intents**, not the driver of gameplay.

## Design Decisions (#1060)

| Question | Decision |
|----------|----------|
| Seat entry | Proximity gate (~0.5 m) + facing seat front (~45° cone) + snap to seat anchor. No pathfinding. |
| Seat exit | Try exit offsets: front → left → right → back. If all blocked, snap to seat top. |
| Seat animation | Short sit/stand clips synced to anchor snap. Programmatic snap preferred over root motion. |
| Seat interrupt | Yes — hit or ragdoll cancels seated state and attempts unbuckle. |
| Combat stagger | Brief flinch (0.2–0.4 s) blocks new attacks; does not block movement unless weapon specifies knockback. |
| Knockback | Server-applied CharacterController impulse; optional per-weapon. |
| Hit timing (v1) | Instant damage; animation starts before hit VFX. Animation-event hits in v2. |
| Get-up blocked overhead | Crawl/get-up-low variant if headroom check fails; else remain prone. |
| Table climbing | Deferred to Alpha 0.2; `CanClimb` capability hook only. |
| Floating (ghost) | `Floating` animator bool driven by ghost controller. |
| Player control | No AI locomotion for interactions. Player always initiates movement. |

## Architecture

```
Input / Health / World → HumanoidBodyStateMachine → Capabilities
                              ↓                           ↓
                    BodyAnimationSnapshot          Movement Controller
                              ↓
                    AnimationOrchestrator → Animator Layer Stack
                              ↓
                         FishNet SyncVar
```

### Body States

- **Locomotion** — idle, walk, run, limp
- **Seated** — buckled to chair; reduced movement capabilities
- **Crawling** — prone movement
- **Staggered** — brief flinch after hit
- **Ragdoll** — physics knockdown (interrupts all states)
- **Dead / Unconscious** — no player control

### Capabilities

Each state exposes: `CanMove`, `CanRotate`, `CanRun`, `CanUseHands`, `CanInteract`, `CanBeInterrupted`.

### Animator Layers

1. **Base** — three FreeformCartesian2D locomotion blends switched by `CombatStance` (0 Peaceful / 1 Melee / 2 Ranged):
   - **Peaceful** — [Locomotion Pack](../Assets/Art/Animations/Locomotion%20Pack/) idle / walk / run / strafes
   - **Melee** — [Pro Melee Axe Pack](../Assets/Art/Animations/Pro%20Melee%20Axe%20Pack/) standing idle / walk F-B-L-R / run F-B (includes backpedal)
   - **Ranged** — [Basic Shooter Pack](../Assets/Art/Animations/Basic%20Shooter%20Pack/) rifle idle / walk / walk back / strafes / run / run back
2. **UpperBody** (arms + head mask) — combat-only item/weapon holds and `AttackSwing`; weight is always 0 in Peaceful so locomotion returns to normal even while holding items
3. **Additive** — flinch (`Flinch` uses melee gut react), injured arm overlay
4. **FullBody Override** — sit, crawl, emote, stand-up

`AttackSwing` is an upper-body one-shot over Melee locomotion (layer weight raised for the swing only).

Rebuild via **SS3D → Animation → Rebuild Combat Stance Blend Trees** after reimporting pack FBX clips.

### Combat stances

| Stance | Pack | How entered |
|--------|------|-------------|
| Peaceful | Locomotion Pack | Default; `C` toggles combat off |
| Melee | Pro Melee Axe Pack | `C` on + unarmed or non-ranged weapon |
| Ranged | Basic Shooter Pack | `C` on + hand item trait contains Ranged/Gun/Firearm/Rifle |

Peaceful movement faces the move direction. Melee/Ranged face the mouse: body yaw from the planar aim, head/torso pitch via look-at IK (AimPitch). Pack clips bake root rotation into pose so GameObject aim yaw stays authoritative.

`BodyAnimationSnapshot` replicates `CombatMode` (2 bits), `AimYaw`, and `AimPitch`. Orchestrator drives animator `CombatStance` / aim floats and combat look-at IK.

### Networking

- `BodyAnimationSnapshot` replicated via SyncVar (packed uint + aim yaw float)
- Animation intents sent via ServerRpc; server validates and updates snapshot
- Humanoids reduce reliance on `NetworkAnimator`; props keep bool/trigger sync
- Server-authoritative movement via FishNet prediction/reconcile (Phase 0.0.8)

## Integration Points

| System | Hook |
|--------|------|
| Movement | Reads `BodyCapabilities`; combat mode enables strafe |
| Health | Foot damage → limp; arm damage → injured overlay; hit → stagger |
| Inventory | Item in hand → `ArmHold` pose; ranged traits refresh combat stance while in combat |
| Interactions | Seat → `TrySit(anchor)`; throw/hit → animation triggers |
| Combat | `C` toggles Peaceful ↔ Melee/Ranged; mouse aim + strafe; LMB (Run Primary) melee swing instead of interactions |
| Ragdoll | Universal interrupt via existing `Ragdoll` SyncVar |

## Phased Delivery

- **Phase 0** — Rig references, body state machine, orchestrator, server movement
- **Phase 1 (#1333)** — Layered controller, core clips, sit/throw/hit/emote
- **Phase 2 (#1246)** — Combat mode, strafe, stagger/knockback
- **Phase 3 (#937)** — IK, crawl polish, clothing hooks
