> Implements: Documents/design/combat.md §3 (ranged, two-handed rule, thrown travel time), §5 (knockdown and standing back up); Documents/design/explosives-destruction.md §6 (grenades reuse the general throw); Documents/design/audio.md §3 (thrown items sound like their material)
> Touches systems: entities, combat, inventory, interactions-framework, structural-destruction, player-control
> Status: in-progress

**Progress:** Planned and grilled with the owner on 2026-11-08. The owner put it ahead of MVP1 M0: the pistol stance and throwing serve [mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M1c (Stechkin, security pistol, minibomb), and get-up shows on every knockdown in a round. Drag was cut to the post-playtest feel pass, so M1m closed without it. Tests follow the [testing philosophy](2026-11_testing-philosophy.md). Phase 1 (get-up) was implemented on `claude/humanoid-getup` on 2026-11-08 and passes its EditMode and Environment scenario tests; the owner hand check is still to do. The owner raised the get-up cap from ~1.2 s to ~2.2 s (Decision 2). The owner hand-checked the get-up on 2026-11-09 and found it good, but asked for less time lying down after a wind knockdown: it went from 3 s to 1.5 s, with a 1 s hold while the gust keeps blowing (`Ragdoll.Wind.cs`). Phase 2 (pistol stance) was implemented on `claude/humanoid-pistol` (stacked on the get-up branch) on 2026-11-09 and passes EditMode and the new `scenarios:Combat`. The owner hand-checked it on 2026-11-09 and approved it. Phase 3 has not started.

# Humanoid animation pass: get-up, pistol stance, throwing (Nov 2026)

Three things a playtester sees every round look wrong today. Standing up from a ragdoll passes through a T-pose. Every one-handed gun borrows the rifle's pose. Throwing is an item teleporting out of the hand with no body motion, and only the minibomb can do it. The owner supplied Mixamo clips for all three. This effort imports them and wires each into the existing body state machine and animator.

## Starting state (2026-11-08, `1cca8351f`)

- **Get-up:** `Ragdoll.BonesReset` blends ragdoll bones toward the first frame of the stand-up clip, then `StandUp()` plays that clip by state name (`Ragdoll.cs:635–700`). The target pose comes from `AnimationClip.SampleAnimation(gameObject, 0f)` on `GettingUpFaceUp.anim` / `GettingUpFaceDown.anim`. Those are baked humanoid muscle clips (`HumanoidClipBakeUtility`). Unverified hypothesis: sampling a muscle clip with the Animator disabled leaves the bones in the avatar's reference pose, so the blend goes ragdoll → T-pose → clip. The controller stays off until the clip ends. (Confirmed in Phase 1, and worse: see Phase 1 findings.)
- **Pistol:** `RangedWeaponProfile.RequiresBothHands` is false only for the taser (`RangedWeaponProfile.cs:130`). The taser still plays the rifle fire/reload and combat locomotion trees. `TwoHandedWeaponRules.ShouldMirrorUpperBody` already mirrors the upper body for a one-handed weapon in the left hand. The Stechkin and security pistol do not exist yet (M1c).
- **Throwing:** only `TimedCharge.ThrowInteraction` (`TimedCharge.cs:259`, radial "Throw minibomb") throws. It places the item at chest height and sets a ballistic velocity; the body does not turn or move. A `Throw` trigger, `AnimationTriggerId.Throw` and a `Throw` animator state (baked `HumanThrow`) exist, but nothing fires them.
- **Input:** `R` is bound only in the Map Editor's Tile Creator map (Rotate). Gameplay bindings are rebindable (player settings Phase 3).

## Clips

From the owner's `~/Downloads`, 2026-11-06. Import recipe from M1m: Humanoid rig, avatar `CopyFromOther` from `Locomotion Pack/X Bot.fbx`, `Mix_*` clip names, loop clips get `loopTime` + `lockRootRotation` + `keepOriginal*`. Set it through `ModelImporter` from `execute_code`. The rebuild menu opens a modal dialog, so call `HumanoidLocomotionBlendSetup` methods directly. Each phase imports only its own clips.

| Phase | Files | Target folder |
|---|---|---|
| 1 Get-up | `X Bot@Stand Up From Back.fbx` (face up), `X Bot@Standing Up From Stomach.fbx` (face down) | `Assets/Art/Animations/GetUp/` |
| 2 Pistol | `Pistol_Handgun Locomotion Pack.zip`: idle, walk, run, walk/run backward, strafe ×2, walk/run arcs. The kneel set is imported and not wired | `Assets/Art/Animations/Pistol/` |
| 3 Throw | `X Bot@Throw.fbx` (overhand, ordinary items), `X Bot@Grenade Throw.fbx` (explosives), `X Bot@Run And Throw Grenade.fbx` (kept only if its upper half reads better than `Grenade Throw`) | `Assets/Art/Animations/Throw/` |

**Pistol pack, measured in Blender:** the hands stay 4 cm apart at chest-to-eye height in idle, walk, run and strafe, so the clips grip the gun with both hands. There are no diagonals and no aim, fire or reload clips.

## Decisions

Settled with the owner on 2026-11-08.

1. **Order and shape:** get-up, pistol, throw. One branch and PR per phase, get-up first, so each animator rebuild (~2.4k-line controller diff) is reviewed on its own and get-up does not wait on throwing.
2. **Get-up comes after the knockdown.** Combat §5's roughly 2–3 s is time on the ground, the window others can punish. The stand-up plays after it and is capped near 1.2 s by trimming or playback speed; the cap is checked against real clip lengths at import. *Revised 2026-11-08:* the trimmed clips hold 4.9 s and 5.4 s of motion, so 1.2 s would need about 4× speed. The owner chose a 2.5× limit instead, which gives back movement about 2.2 s after recovery.
3. **Get-up control:** a hit that knocks down during get-up restarts the ragdoll, as for anyone standing (combat §5). No early movement, but the last ~20 % of the clip blends into locomotion when a direction is held.
4. **Hold style is weapon data.** A `HoldStyle` field (Rifle, Pistol; default Rifle) on `RangedWeaponProfile` picks the pose. `RequiresBothHands` stays the gameplay rule and is not used to infer the pose.
5. **The pistol stays one-handed in gameplay** (combat §3 reserves the two-hand rule for rifles and heavy ordnance), but the pose uses the clips' two-handed grip when the off-hand is empty. When the off-hand holds an item, that arm leaves the pistol layer (avatar-mask swap) and keeps its normal hold pose, and the gun arm stays in the clip pose.
6. **Left-hand pistols mirror** the whole upper body through the existing Mirror Upper Body path, including the mask swap in Decision 5.
7. **Pistol aim:** the pack's idle as the upper-body pose, plus the existing `HumanoidIkController` spine/chest muzzle correction, plus a procedural forearm kick per shot. Arm IK only if that cannot land the muzzle on the aim point. Reload keeps the timed refill with no hand animation.
8. **Anything held can be thrown.** Range falls with item weight (the #184 recipe table): light items about 10 m, heavy about 3 m, and above a weight limit an item cannot be thrown. Two-handed items need both hands free. No damage on hit.
9. **Throw mode on `R`** (owner decision; no design doc defines a throw control, see Open questions). A toggle, as in SS13: the next click throws the active hand's item at the point under the cursor, replacing that click's normal interaction. It turns off after a throw, on a click with an empty hand, and on `Esc`.
10. **Throw feedback:** in throw mode the reticle changes to a throw icon and a landing marker sits on the target point, clamped to max range, so a throw that will fall short is visible before release (combat §1).
11. **The minibomb loses its radial "Throw minibomb" entry.** It becomes an ordinary throwable that arms at release. Arming it in hand stays a separate action.
12. **The throw is an upper-body oneshot over locomotion.** The body yaw-snaps toward the target first; while moving, the spine/chest correction from the rifle keeps the arm on target. The item leaves the hand at the clip's release frame, timed the way `MeleeSwingAnimation` times a strike.
13. **An interrupted throw keeps the item.** A knockdown, stun or hand swap before the release frame cancels the throw; the item stays in hand, and a minibomb stays unarmed (combat §2: an interrupted windup does nothing).
14. **Drag is cut** to the post-playtest feel pass, next to body dragging ([mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) Explicitly deferred). See **Notes for the drag pass**.

## Phases

### Phase 1 — get-up without a T-pose

- Import the two get-up clips. Record their lengths here and set the trim or speed for Decision 2.
- Prove the T-pose cause first: capture the sampled `_standUpBones` pose in the editor. Fix what the evidence shows. A likely fix samples frame 0 through a playable graph on the Animator, or bakes a generic copy of frame 0 for sampling.
- Re-knockdown during get-up and the locomotion blend-out per Decision 3.
**Phase 1 findings (2026-11-08):**

- **Cause:** the old `GettingUpFaceUp/Down.anim` evaluated to the avatar's T-pose at every time, through `SampleAnimation`, a `PlayableGraph` and the Animator alike (hips at standing height 0.83 m, arms horizontal). So the blend target was a T-pose, and so was the clip after it. Both clips, and two orphaned states that used them, are deleted.
- **Fix:** [`GetUpAnimation`](../../Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/GetUpAnimation.cs) is the trim table. The rebuild imports and trims the clips from it (shared `ConfigureOneshotImports`, now also used for the melee swings), sets each state's speed, and wires a stance-aware exit at 80 % that blends over the rest of the clip. `Ragdoll` samples frame 0 with `Animator.Play` + `Update(0)` (a re-enabled Animator has zero layer weights, so only the Base Layer pose lands), and blends every humanoid bone, not just the 13 rigidbody parts.
- **Clips:** face up `Stand Up From Back` frames 40–186 (4.87 s), face down `Standing Up From Stomach` frames 96–258 (5.40 s), both at 2.5×. The other `Stand Up*.fbx` in Downloads are the same take as `Stand Up From Back`.
- **Root offset:** the face-up first frame puts the hips 0.68 m behind the root. `Ragdoll` moves the root so that frame's hips land on the ragdoll's hips; without that, the blend dragged the body 0.68 m.
- **Observers** now blend their own bones too (before, they cut from the ragdoll to the clip). Facing is read from the hips on each peer, so the `IsFacingDown` SyncVar is gone.
- **Measured** in the Environment scenarios: movement returns 2.16 s (face up) and 2.33 s (face down) after recovery. Hips peak during the blend is 0.07–0.10 m, with 0.00 m drift. A knockdown while standing up re-ragdolls the body.

- **Done when** a stun-baton knockdown, a shove and a blast knockdown each stand up with no T-pose frame, face up and face down, seen from the owner and from an observer client; get-up time stays within the cap; and a hit during get-up drops the body again. EditMode covers the trim table; owner hand check for the look.

### Phase 2 — pistol stance

- Import the pistol pack. Add `HoldStyle` and set the taser to Pistol; the Stechkin and security pistol take it when they land in M1c.
- Build a combat pistol locomotion tree in `HumanoidLocomotionBlendSetup`, calibrated like the rifle tree (`timeScale` from clip speed × `humanScale`). With no diagonal clips, check whether the directional blend of forward/backward and strafes slides; if it does, record the slide and the fix here.
- Upper body per Decisions 5–7: the two-handed grip with an empty off-hand, the gun-arm-only mask with a full one, mirrored for the left hand. The procedural kick fires from `ObserversNotifyMuzzleFlash` so observers see it.
**Phase 2 findings (2026-11-09):**

- **Data:** `RangedWeaponProfile.HoldStyle` (`WeaponHoldStyle` Rifle/Pistol, serialized; the taser prefab and `RangedWeaponProfile.Taser` are Pistol). `TwoHandedWeaponRules.IsPistolHold` reads the wielded gun and whether the other hand is full; the bridge packs both into snapshot bits 27–28, so observers pose from replicated state. Names and clips live in [`PistolStanceAnimation`](../../Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/PistolStanceAnimation.cs).
- **Legs:** `Ranged Locomotion` now plays `Ranged Hold Blend`, a 1D blend on the `PistolHold` float between the rifle tree and a new `Pistol Locomotion 2D`. The orchestrator moves `PistolHold` over 0.15 s, so swapping guns blends. The pack has no diagonals and one strafe speed, so each strafe sits on both rings. Calibrated world speed vs target (1.05 walk / 2.5 run m/s): walk forward and strafe right are capped at timeScale 0.5 and run 6 % and 2 % fast; every other child is on target. Directional blending between forward and strafe was not seen to slide in EditMode sampling; the owner's walk-around is the real check. `strafe (2)` is the right strafe.
- **Upper body:** `Pistol Aim Idle` (`Mix_PistolIdle`, mirrored on `MirrorUpperBody`) beside `Rifle Aim Idle`; every way into either is gated on `PistolHold` above or below 0.5, and the two cross-fade when the gun changes. The orchestrator skips the rifle `FireRifle`/`Reload` triggers for a pistol (Decision 7).
- **Off-hand (Decision 5):** layers `Off Hand Left` / `Off Hand Right` (indices 4, 5) with arm-only masks play `Mix_Idle`. The orchestrator weights the one opposite the gun when the off-hand is full. The plan said "keeps its normal hold pose", but the Upper Body `Hold Item` clip is a generic transform clip that poses nothing on the humanoid rig (the "hold pose" was always the locomotion arm), so the arm hangs relaxed at the side instead. Measured: hands 6.5 cm apart in the grip, 60 cm with a full off-hand.
- **Left hand (Decision 6) needed more than the upper-body mirror.** The pistol legs stand bladed (hips 53° off the root) and the torso twists back; mirroring only the upper body flipped that twist and put the grip 0.3 m to the side. `Ranged Locomotion` now mirrors on `MirrorUpperBody` too, and both Ranged trees read `RangedVelX` (VelX negated while mirrored) so strafes still step the right way. Two-handed rifles never set `MirrorUpperBody`, so the M4 is unchanged.
- **Aim and kick (Decision 7):** the existing spine/chest muzzle correction needed no change; on a live host the taser points 24–25° off the body's facing in either hand, as the M4 (19°) does under the same off-centre test aim point. `ObserversNotifyMuzzleFlash` calls `HumanoidIkController.PlayShotKick`: both gripping forearms pitch up 9° over 0.03 s and settle in ~0.4 s. Arm IK was not needed.
- **Owner hand check (2026-11-09):** the kick, the slowed walk and the observer view work; no changes asked.

- **Done when** the taser in Harm idles, walks, runs, strafes, aims and fires with a pose that reads as a pistol from an observer client, in either hand, with and without an off-hand item; the M4 is unchanged; feet do not slide at walk or run. Visual step plus an owner hand check.

### Phase 3 — throwing

- Throw mode on `R` per Decisions 9–10, as a rebindable gameplay action: toggle, reticle icon, clamped landing marker.
- A general throw on held items: range from weight (Decision 8). Move the minibomb's ballistic launch into it and remove its radial entry (Decision 11).
- Animation per Decision 12: the server fires `AnimationTriggerId.Throw` with the clip choice through the snapshot (pack the snapshot before bumping the trigger sequence; entities map Pitfalls). A release-frame table drives the server's launch. Cancel rules per Decision 13.
- Thrown items land with their material impact sound (audio §3; `ItemCollisionMaterial` exists).
- **Done when** an ordinary item and a minibomb can each be thrown at a point within range, standing and running; out-of-range targets show a clamped marker and land at max range; the arm motion matches the release on an observer client; a knockdown before release keeps the item in hand. Tests cover launch velocity reaching the target point, the weight-to-range bands, and the cancel rule.

## Out of scope

- Drag and pull (Decision 14).
- Thrown-item damage on hit.
- Pistol reload clips; kneeling as a stance.
- Ranged stun balance. The taser exists in code although combat §11 keeps ranged stuns out of this design pass; this effort only changes how it is held.

## Notes for the drag pass

For whoever picks up dragging in the feel pass:

- **Mixamo pull clips** (`Assets/Art/Animations/Pulling/`, and newer `(1)` exports in the owner's Downloads that differ byte-for-byte) show pulling something big and heavy, walking backwards. They fit furniture and machines, not bodies.
- **Body dragging** has an older SS3D clip: `Assets/Art/Animations/Probably Not/HumanDragBodySet(1).fbx` (also on upstream's `nat-shitcode` branch). Takes `DragBodySS` (frames 1–31, the dragger) and `DragBodySS_Pose` (31–61, the dragged body). It uses the old SS3D rig (`hips`, `thigh_l`…), so it needs retargeting to the Humanoid avatar.
- **Today's drag:** `SteelGirderUnbolted` is the only prefab with `Draggable`. `DragInteraction.Start` sets `HumanoidLivingController.IsDragging` on the server, while living movement belongs to the owner's client, so a remote client's controller probably never sees it. The animator declares `IsDragging` but nothing reads it.

## Open questions for the owner

- **Throw mode in the design docs.** Decision 9 fills a gap: `main-hud.md` §7 defines help/harm, disarm and grab but no throw control. Adding throw mode there is the owner's edit.
