> Parent map: [structural-destruction.md](structural-destruction.md)
> Code paths: Assets/Scripts/SS3D/Systems/StructuralDamage/
> Status: partial
> Verified: 31b397e6a — 2026-11-14

# Explosive items

## Overview

Implements [explosives-destruction.md §6–§7](../../design/explosives-destruction.md#6-explosive-items). The three items share `TimedCharge`; only the server arms, defuses and detonates them. `ExplosiveKind` selects the content. Protocol 28 covers the new behaviour state and the small-blast sound flag; protocol 29 adds the owner blast RPC. Persistence format is unchanged.

| Item | Fuse | Gameplay |
|---|---|---|
| C4 | 15 seconds | Place and arm from its menu, or hold and use on a wall/door to attach and arm. Radial yield 180, falloff 45. |
| X4 | 15 seconds | Hold and use on a wall/door to attach and arm. Radial yield 240/falloff 60, plus yield 180/falloff 40 in a 90-degree cone through the far side of its target. |
| Syndicate minibomb | 5 seconds | Arm in hand using Use, or throw it with throw mode (R, [combat](combat.md)). It arms as it leaves the hand; an already running fuse is preserved. Small radial yield 90/falloff 45. |
| Syndicate bomb | 90 s to 10 min, set in 30 s steps | Too heavy to lift: dragged into place. Arm it, then bolt it down with a wrench (3 s). Once armed the bolts lock. Screwdriver (5 s) then wirecutters (10 s) defuse it, and it can be re-armed. Radial yield 500/falloff 40, the largest in the game. |

Floor and plenum damage is applied after all blast passes; see [floor and hull damage](structural-destruction-floors.md).

The values above are provisional content tuning. X4's additional force uses the same BFS, structural apply path and crew damage as radial blasts; it still stops at surviving structure. Both passes produce one audiovisual detonation. The direction is captured from the attached surface, pointing into the target. Picking up an attached charge dislodges it: its blast then follows the item, without the attached X4 cone.

C4/X4 have physical LED countdowns and a three-second wirecutter defuse. Movement, range loss or losing the tool cancels; expiry wins a same-frame defuse. Minibombs cannot be defused. Throwing is the general held-item throw ([combat](combat.md)); explosives use the grenade lob, and a minibomb's 0.5 kg carries the full 10 m.

`Effects/minibombcountdown.ogg` is imported as `ExplosiveCountdown.ogg` for the five-second minibomb. C4/X4 use `ChargeCountdown.ogg`, a fifteen-second version with ten steady opening cues followed by the original accelerating five-second finale. One non-looping recording starts at arming; a client seeks from the replicated deadline when it first observes an armed charge. Playback follows the item through the SFX pool with normal occlusion. Defuse/despawn stops and detaches that voice. C4/X4 reuse registered SS14 `explosion1.ogg`; minibombs use `explosion_small1.ogg`. Distant listeners retain the existing blast rumble.

**Bodies in a blast** ([explosives-destruction.md §5](../../design/explosives-destruction.md#5-damage-to-crew)): `BlastResolutionService` calls `applyCrewPush` once per detonation, after all passes, with the force per tile summed as for floors, so X4 pushes once. `BlastPush` turns a tile's force into planar velocity straight away from the epicenter: 0.05 m/s per force, capped at 10 m/s. Force 90 or more knocks down, which is within two hops of a C4 and only on a minibomb's own tile. These values are provisional. `Ragdoll.ServerApplyBlast` sends the velocity to the physics authority: a floored body launches its bones, with a little lift over a floor; an upright one takes a decaying `CharacterController` shove. A blast knockdown recovers on floor support like a wind knockdown, and keeps a health-owned collapse timeless. Magboots stop a shove, but a knockdown ends their hold ([armor.md §3](../../design/armor.md)). Invincibility stops the damage, not the push. Direction is a straight line, so a body round a corner is pushed toward the corner's wall.

**Syndicate bomb** (`SyndicateBomb`, rules in `SyndicateBombState`). It is an `Item` with `TooHeavyToLift` set ([inventory](inventory.md)), so no hand, slot or locker takes it, and a `Draggable` sits beside it. The map editor, the admin `spawn` command and saves all treat it as an ordinary item. `SyndicateBomb` implements `IDragLock`: a bolted bomb offers no Drag and the server refuses one. Bolted, its rigidbody is kinematic, so wind and shoves don't move it; loose, the wind can carry it like any heavy item (100 kg). Its menu reads "Set timer to 2:00", "Arm bomb (1:30)", "Bolt to floor"/"Unbolt from floor", "Unscrew wiring panel" and "Cut the wires (defuse)"; the action lines reuse the nuke's arm/panel/wire keys plus `BoltDown`/`Unbolt`. While armed, each client beeps the nuke's `NukeTick` out to 25 m (one per second, then two and four per second under 30 s and 10 s) and flashes a small red point light over the core, both derived from the synced deadline. Examine shows the timer or time left, whether it is bolted, and an open panel. Nothing is announced station-wide. It is delivered by `SyndicateBombBeacon`, a one-use item: Use (E) with it in the active hand drops the bomb in front of the user (pulled back from a wall) with the SS14 teleport-arrival sound and deletes the beacon. There is no uplink yet, so the first operative to spawn each round carries the squad's one beacon in their bag (`RoleLoadout.FirstHolderContents`, [gamemodes-roles-traits](gamemodes-roles-traits.md)). The bomb model is a contributed `.blend` (60 cm wide, 6 cm tall, kept at the authored size by owner choice 2026-11-14). The beacon is a generated placeholder (dark box, antenna, red lamp) until someone models one.

**Design deviations** ([explosives-destruction.md §6–§7](../../design/explosives-destruction.md#6-explosive-items)): the bomb has no numeric countdown on the model, only the flashing lamp, the beeps and examine. It has no proximity alert.

Remote triggers, a proximity alert, other grenades and separate-client acceptance of the items themselves remain outstanding. Moving airlock leaves do not carry the attached model; attachment is fixed at the selected world contact.

## Start here

- `Assets/Scripts/SS3D/Systems/StructuralDamage/TimedCharge.cs` — profiles, server fuse, arm/attach/defuse interactions, arm-on-throw (`IThrowReleaseHandler`) and local countdown voice
- `Assets/Scripts/SS3D/Systems/StructuralDamage/ChargeCountdownDisplay.cs` — physical LED segments
- `Assets/Scripts/SS3D/Systems/StructuralDamage/SyndicateBomb.cs` / `SyndicateBombState.cs` — the bomb's server adapter and client beep/lamp; pure timer, bolt and defuse rules
- `Assets/Scripts/SS3D/Systems/StructuralDamage/SyndicateBombBeacon.cs` — one-use delivery beacon
- `Assets/Scripts/SS3D/Systems/StructuralDamage/Editor/SyndicateBombPrefabSetup.cs` — bomb and beacon prefabs, map-editor entries, sounds, examine text (batch: `SetupBatch`); meshes from `Tools/export_syndicate_bomb.py <Syndicate_Bomb.blend>`
- `Assets/Scripts/SS3D/Systems/StructuralDamage/Editor/ExplosivePrefabSetup.cs` — repeatable FBX mesh bake, prefabs, item/sound database registration and examine text
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralDamageSubSystem.cs` — radial blast and `ResolveShapedCharge`
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BlastResolutionService.cs` — common BFS and `ResolveCone`
- `Assets/Scripts/SS3D/Systems/StructuralDamage/BlastPush.cs` — force to body velocity and knockdown; dispatched from `StructuralDamageSubSystem.ApplyCrewBlastPush`
- `Assets/Scripts/SS3D/Systems/Entities/Humanoid/Ragdoll.Blast.cs` — server knockdown, owner RPC, bone launch and upright shove
- `Assets/Scripts/SS3D/Systems/StructuralDamage/StructuralAudioPresenter.cs` — near/small/far explosion sound selection

## Extension points

- Run **SS3D → Structural Damage → Run Content Prefab Recipes** to rebuild content. It creates `C4`, `X4`, `SyndicateMiniBomb`, `SyndicateBomb` and `SyndicateBombBeacon` under `Assets/Content/WorldObjects/Items/Weapons/` and registers their item refs/Addressables. After the bomb recipe, run `JobKitSetup.Apply` (operative kit) and `ExamineIdentificationKeySetup.AddExamineFeedbackKeys` (examine and action strings); they live in other editor assemblies.
- Bomb tuning: `SyndicateBomb.BlastYield`/`BlastFalloff`, the step times on `SyndicateBomb`, and the timer range on `SyndicateBombState`. The defuse tools are matched by item id (`Items.Wrench`, `Items.ScrewdriverBlue`, `Items.Wirecutters`), as on the nuke.
- The admin console can give any of them with `spawn <name> hand`. Map Editor uses the same registered items.
- New sound files belong under `Assets/Art/Sound/Items/Weapons/Explosive/SS14/`, with source/licence rows in Sound ATTRIBUTIONS. Registration lives in the recipe.

## Pitfalls

- **RPC discards the client normal:** attachment must raycast the target's colliders again from the interaction origin; using `InteractionEvent.Normal` alone works in discovery and fails server revalidation.
- **A completed pooled voice can already belong to a new sound:** stop/detach the countdown only while its source is still parented to that charge, or despawn can cut off the explosion that reused it.
- **Held/attached items have special poses:** apply the wall contact after `PlaceHeldItemOutOfHand` (its helper lifts floor drops), keep the collider selectable, and use the server-authoritative item NetworkTransform.
- **A blast is not a gust:** `ServerApplyWind` looks reusable, but the 5 Hz field overwrites its target within a tick in a breach, and drag caps it at `AtmosWind.MaxSpeed`. `Ragdoll.Blast` shares wind's routing to the physics authority and nothing else.
- **Lethal force does not kill at once:** `Human.Kill` runs from a later health tick (1 s interval), not from `ApplyDamage`, and only then does bone authority pass from the owner to the server. Velocity is not handed over, so a body dying in flight should stop there; this comes from reading the code and has not been seen in play.
- **Blasting a test room's walls holds the body down:** a breach's wind keeps extending a timed knockdown, so a scenario waiting for recovery times out. The test room's walls fall to force 90; see `OpenRunEpicenter` in `ExplosivesScenarios`.
- **FBX root scale/axis conversion is part of the pose:** bake the source's full `localToWorldMatrix` into the mesh. Dropping the imported root makes a tiny upright charge.

- **Draggable has no despawn cleanup:** the dragger's `HumanoidLivingController.IsDragging` is a client flag set by `DragInteraction`. If a bomb detonates or is deleted mid-drag, the flag stays set until the next drag. That is unlikely to matter next to a bomb this size.

## Tests

- `TimedChargeTests`: profile fuse lengths, physical model size, LED presence, registered countdown clips, defuse tool and expiry gates, interface priority.
- `SyndicateBombTests` (EditMode): timer floor and cap, arming, bolts locking while armed, the two-step defuse and re-arm, spent state, beep rate, the prefab is draggable and too heavy for any container, the beacon reference, and the beacon in the operative kit's first-holder contents only.
- `SyndicateBombScenarios` (`scenarios:Explosives`): the beacon calls the bomb down, Pick up is refused, timer up/down, drag and let go, arm, bolt while armed, unbolt refused, drag refused while bolted, screwdriver then wirecutters defuse, unbolt after the defuse; and one detonation opens the hull under it. `NukeOpsScenarios` checks that the host operative spawns with the beacon in their bag.
- `BlastPushTests`: direction, force scaling and cap, the on-epicenter fallback, the knockdown line and one push from X4's two passes.
- `ExplosivesScenarios`: owner floored and thrown then recovered, upright shove, unowned body, and a launch at `MaxSpeed` stopping at a wall. `EvaGearScenarios` covers boots against a shove. `Testing/ui/validate.sh --steps mppm:blast` proves the shove, throw and recovery on a remote owner. Its throw check is peak planar speed: a plain collapse can end half a metre to the side.
- `BlastResolutionTests`: directional force reaches behind a breached target, excludes rear/side tiles and stops at a surviving barrier.
- `ThrowScenarios` (`scenarios:Combat`) covers a minibomb arming at release and staying unarmed when a knockdown interrupts the throw.
- Host acceptance exercises minibomb arming/throw/fuse preservation and X4 attachment, countdown seek and target breach. Separate-client/late-observer acceptance remains pending.
- Run `Testing/ui/validate.sh --steps compile,editmode`; visual changes also require `visual` plus inspecting item renders.

## Depends on / Used by

- **Depends on:** [interactions-framework](interactions-framework.md), [inventory](inventory.md), [audio](audio.md), [tile](tile.md), [entities](entities.md) (body presentation)
- **Used by:** [structural-destruction](structural-destruction.md)

## Related docs

- [Station structural damage plan](../../plans/station_structural_damage.plan.md)
- [MVP1 combat core](../../milestones/mvp1-nuke-ops.md)
