# Humanoid Animation System

> Tracks GitHub issue **#1333** ("Animation system part 1"). This page documents the state of the
> feature in the current working tree — none of it is committed yet.

## Status at a glance

| Piece | State |
|---|---|
| Animator parameters & layers | Present on `HumanCharacterAnimator.controller` |
| C# API (`HumanoidAnimatorController`) | Implemented |
| Networking (owner-driven + server hit relay) | Implemented |
| Debug console commands | Implemented (6 commands) |
| Combat integration | Hit animation wired into `HitInteraction` |
| Animation clips | Authored, but **misfiled** — see [Known Issues](#known-issues) |
| Emote data asset (Wave) | Created and wired to `Human.prefab`, but misfiled |
| `HitTriggerLeft` parameter | **Broken** — typo, see [Known Issues](#known-issues) |

## Overview

The humanoid Animator now drives more than movement speed. It's organized as one **base
layer** (existing: Movement/Floating/GettingUp/Sit/Prone) plus additive/override layers added for
this feature:

- **Injury** — one-shot flinch reactions per body region (additive)
- **Hold Left** / **Hold Right** — per-arm held-item pose, driven independently
- **Emote** — data-driven emotes, gated by posture
- **Action** — masked to the upper body so it plays even while sitting/prone (hit swings, throws)
- **Limp** — leg injury blend

All parameters are referenced from code via `Animator.StringToHash` constants in
[`Animations.Humanoid`](../Assets/Scripts/SS3D/Systems/Entities/Data/Animations.cs), never by
string literal at the call site. Because Unity treats `Animator.SetX` on an unrecognized parameter
hash as a silent no-op (a console warning, not an exception), the C# side compiles and runs safely
independent of whether the Animator Controller has caught up — which is exactly the situation this
document exists to close the gap on.

## Parameter contract

| Parameter | Type | Layer | Set by |
|---|---|---|---|
| `Sit` | Bool | Base | `SetPosture` |
| `Prone` | Bool | Base | `SetPosture` |
| `LegInjury` | Float [-1, 1] | Limp | `SetLegInjury` |
| `HurtArmLeft` / `HurtArmRight` | Trigger | Injury | `TriggerFlinch` |
| `HurtLegLeft` / `HurtLegRight` | Trigger | Injury | `TriggerFlinch` |
| `HurtHeadFront` / `HurtHeadBack` | Trigger | Injury | `TriggerFlinch` |
| `HurtTorsoFront` / `HurtTorsoBack` | Trigger | Injury | `TriggerFlinch` |
| `HoldPoseLeft` / `HoldPoseRight` | Int (`HoldPose` enum) | Hold Left / Hold Right | `SetHoldPose` |
| `EmoteIndex` | Int | Emote | `TryPlayEmote` |
| `EmoteTrigger` | Trigger | Emote | `TryPlayEmote` |
| `HitTriggerLeft` / `HitTriggerRight` | Trigger | Action | `ServerTriggerHit` (via RPC) |
| `ThrowTriggerLeft` / `ThrowTriggerRight` | Trigger | Action | `TriggerThrowLocal` |

## Networking model

`HumanoidAnimatorController` now derives from `NetworkActor` (FishNet's `NetworkBehaviour`)
instead of the plain `Actor`, giving it `IsOwner`/`[Server]`/`[ObserversRpc]`.

- **Owner-driven parameters** (posture, limp, hold pose, flinch, emote, throw): every setter checks
  `IsOwner` and no-ops otherwise. The existing `NetworkAnimator` component on the Human/Ghost
  prefabs replicates the resulting parameter changes to observers — no extra RPC needed.
- **Hit trigger is the exception.** `HitInteraction` runs authoritatively on the server (it's only
  reached from `Start`, which is server-side), so there's no local owner to drive `NetworkAnimator`
  replication. `HitInteraction.TriggerAttackerHitAnimation` resolves the attacker's
  `HumanoidAnimatorController` from the swinging hand and calls `ServerTriggerHit(HandSide)`
  (`[Server]`), which fires `RpcTriggerHit` (`[ObserversRpc]`) to set the trigger on every client.

## API reference

`Assets/Scripts/SS3D/Systems/Entities/Humanoid/HumanoidAnimatorController.cs`

| Method | Access | Description |
|---|---|---|
| `SetPosture(Posture)` | Owner | Drives `Sit`/`Prone`, gates which emotes may play |
| `SetLegInjury(float)` | Owner | Clamped to [-1, 1]; nothing in Health drives this yet |
| `SetHoldPose(HandSide, HoldPose)` | Owner | Sets one arm's held-item pose independently |
| `TriggerFlinch(FlinchRegion)` | Owner | One-shot additive flinch on a body region |
| `TryPlayEmote(EmoteData)` | Owner | Fails silently (`false`) if disallowed by current posture |
| `TryGetEmote(string, out EmoteData)` | — | Case-insensitive lookup against the inspector-assigned `_emotes` list |
| `TriggerThrowLocal(HandSide)` | Owner | No real throw interaction exists yet; exercised via console only |
| `ServerTriggerHit(HandSide)` | `[Server]` | Called from `HitInteraction`; broadcasts the swing to observers |

Supporting types, all in `Assets/Scripts/SS3D/Systems/Entities/Humanoid/`: `Posture`
(Standing/Sitting/Prone), `HandSide` (Left/Right), `HoldPose` (None/Briefcase/Drink/Underarm/
Shoulder/Waiter — placeholder names per the issue, freely renameable), `FlinchRegion` (8 body
regions), and `EmoteData` (a `ScriptableObject`, `Assets > Create > SS3D > Animations > Emote
Data`, holding an emote name, animator index, reference clip, and disallowed-posture list).

## Combat integration

`HitInteraction` (melee hit resolution) now calls `TriggerAttackerHitAnimation` after applying
damage, resolving the attacker's `Hands` → `HumanoidAnimatorController` and which side swung from
`Hands.PlayerHands.IndexOf(hand)`.

## Debug console commands

All `CommandType.Client` (run locally on the caller), resolving "my own entity" via the new
`EntitySubSystem.TryGetLocalEntity` (an `IsOwner`-based lookup that doesn't need a
`NetworkConnection` — client-only command paths don't have one). Shared via
`LocalHumanoidAnimatorResolver`.

| Command | Access | Usage |
|---|---|---|
| `posture` | Administrator | `(stand\|sit\|prone)` |
| `limp` | Administrator | `(amount)` — float, -1 (left hurt) .. 1 (right hurt) |
| `holdpose` | Administrator | `(left\|right) (none\|briefcase\|drink\|underarm\|shoulder\|waiter)` |
| `flinch` | Administrator | `(armleft\|armright\|legleft\|legright\|headfront\|headback\|torsofront\|torsoback)` |
| `emote` | User | `(emote name)` |
| `throwanim` | Administrator | `(left\|right)` — debug-only stand-in until a real throw interaction exists |

## Known Issues

These were found while writing this doc and still need fixing before the feature is playable
end-to-end:

1. **`HitTriggerLeft` animator parameter has a leading space.** In
   `HumanCharacterAnimator.controller`, the parameter is literally named `" HitTriggerLeft"` (and
   the transition condition that reads it matches), not `"HitTriggerLeft"`. `Animator.StringToHash`
   in code hashes the correct name, so the two never match — `ServerTriggerHit(HandSide.Left)`
   currently sets a trigger that no transition is listening for and **silently does nothing**. Fix
   by renaming the parameter (and its condition) in the Animator Controller to drop the leading
   space.
2. **19 authored `.anim` clips (+ `.meta`) are sitting directly under `Assets/`** instead of
   `Assets/Content/WorldObjects/Entities/Humanoids/Human/`: `HumanBriefcaseL/R`, `HumanDrinkL/R`,
   `HumanFlinchArmL/R`, `HumanFlinchLegL/R`, `HumanFlinchTorsoF/B`, `HumanFlinchHeadB/F`
   (duplicates — a correctly-filed copy of these two also exists), `HumanHItL`, `HumanHitR`,
   `HUmanLimpLeft`, `HumanLimpRight` (also duplicated in the correct folder, under the correctly
   spelled `HumanLimpLeft`), `HumanShoulderL/R`, `HumanUnderarmL/R`, `HumanWaiterL/R`. The Animator
   Controller's states currently reference the misfiled copies by GUID, so the feature *works* from
   this state, but the clips need moving into the Human folder (via the Unity Editor, so meta/GUID
   wiring stays intact) and the stray duplicates need deleting.
3. **`WaveEmote.asset`** (the `EmoteData` instance wired into `Human.prefab`'s `_emotes` list) is
   also sitting directly under `Assets/` instead of alongside the other Human animation assets.
4. Two clip filenames have casing typos that don't match their sibling clips: `HumanHItL.anim`
   (capital I) and `HUmanLimpLeft.anim` (capital U) — cosmetic, but worth fixing for consistency
   before these move out of `Assets/` root.
5. `SetLegInjury` has no caller yet — the Health system's `FeetController` only exposes one
   combined, non-directional health factor today, not a per-leg one. The method is in place for
   when that plumbing exists.
6. `TriggerThrowLocal` has no real gameplay caller — there is no throw-item interaction in the
   codebase yet, only `Hit`. It exists solely so the throw animation can be exercised via the
   `throwanim` console command.