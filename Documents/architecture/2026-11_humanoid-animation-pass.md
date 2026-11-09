> Implements: Documents/design/combat.md §3 (ranged, two-handed rule, thrown travel time), §5 (knockdown and standing back up); Documents/design/explosives-destruction.md §6 (grenades reuse the general throw); Documents/design/audio.md §3 (thrown items sound like their material)
> Touches systems: entities, combat, inventory, interactions-framework, structural-destruction, player-control
> Status: in-progress

**Progress:** Planned and grilled with the owner on 2026-11-08. The owner put it ahead of MVP1 M0: the pistol stance and throwing serve [mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M1c (Stechkin, security pistol, minibomb), and get-up shows on every knockdown in a round. Drag was cut to the post-playtest feel pass, so M1m closed without it. Tests follow the [testing philosophy](2026-11_testing-philosophy.md). Phase 1 (get-up) was implemented on `claude/humanoid-getup` on 2026-11-08 and passes its EditMode and Environment scenario tests; the owner hand check is still to do. The owner raised the get-up cap from ~1.2 s to ~2.2 s (Decision 2). The owner hand-checked the get-up on 2026-11-09 and found it good, but asked for less time lying down after a wind knockdown: it went from 3 s to 1.5 s, with a 1 s hold while the gust keeps blowing (`Ragdoll.Wind.cs`). Phase 2 (pistol stance) was implemented on `claude/humanoid-pistol` (stacked on the get-up branch) on 2026-11-09 and passes EditMode and the new `scenarios:Combat`. The owner hand-checked it on 2026-11-09 and approved it. Phase 3 (throwing) was implemented on `claude/humanoid-throw` on 2026-11-09 and passes EditMode, `scenarios:Combat` (new `ThrowScenarios`) and `scenarios:Explosives`. The owner hand-checked it and asked for two changes, both made: the feet froze when throwing mid-walk, and the indicator was to be lighter. It is in PR #199. Phase 4 (interaction animations) was added on 2026-11-09 and grilled with the owner the same day (Decisions 15–26), then implemented on `claude/humanoid-interactions`. It passes EditMode and `scenarios:Combat` (new `InteractionAnimationScenarios`); the owner hand check is still to do.

# Humanoid animation pass: get-up, pistol stance, throwing (Nov 2026)

Three things a playtester sees every round look wrong today (Phases 1–3); Phase 4, added later, covers everyday interactions. Standing up from a ragdoll passes through a T-pose. Every one-handed gun borrows the rifle's pose. Throwing is an item teleporting out of the hand with no body motion, and only the minibomb can do it. The owner supplied Mixamo clips for all three. This effort imports them and wires each into the existing body state machine and animator.

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
| 3 Throw | `X Bot@Throw.fbx` (overhand, ordinary items), `X Bot@Run And Throw Grenade.fbx` (explosives). `X Bot@Grenade Throw.fbx` was not kept (Phase 3 findings) | `Assets/Art/Animations/Throw/` |
| 4 Interactions | `X Bot@Opening.fbx`, `X Bot@Picking Up.fbx`, `X Bot@Pulling Lever.fbx` (owner); Mixamo *Picking Up An Object*, *Putting Down An Object*, *Attempting To Open A Door*, *Man Working On Device* | `Assets/Art/Animations/Interact/` |

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
10. **Throw feedback:** in throw mode a marker shows where the item will land, clamped to max range, so a throw that will fall short is visible before release (combat §1). *Revised at the Phase 3 hand check:* no reticle icon or text; a dashed flight arc and a landing ring (main-hud §7).
11. **The minibomb loses its radial "Throw minibomb" entry.** It becomes an ordinary throwable that arms at release. Arming it in hand stays a separate action.
12. **The throw is an upper-body oneshot over locomotion.** The body yaw-snaps toward the target first; while moving, the spine/chest correction from the rifle keeps the arm on target. The item leaves the hand at the clip's release frame, timed the way `MeleeSwingAnimation` times a strike.
13. **An interrupted throw keeps the item.** A knockdown, stun or hand swap before the release frame cancels the throw; the item stays in hand, and a minibomb stays unarmed (combat §2: an interrupted windup does nothing).
14. **Drag is cut** to the post-playtest feel pass, next to body dragging ([mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) Explicitly deferred). See **Notes for the drag pass**.

Phase 4, settled with the owner on 2026-11-09:

15. **Interaction effects stay instant; the motion is decoration.** Unlike the throw, nothing waits for a contact frame, so no interaction gets slower and no cancel rule is needed. Clips are trimmed to start partway into the reach so the hand arrives soon after the effect. *Revised at the first hand check (2026-11-09):* items jumping into and out of the hand, and locker doors moving before the hand reached them, read oddly, so pickup, drop (also the drop hotkey and dragging from the inventory into the world), locker open and close, and lock and unlock now wait for the clip's contact frame. Doors stay instant. Each clip is trimmed so contact lands at 0.45 s or sooner. The owner plays the clip at the click and the server applies the effect at contact, as for the throw's release. At contact the server re-checks the interaction (range, the item still there, the locker unlocked); a knockdown, stun or hand swap before then cancels it (combat §2). One pending hand action at a time: a click while one is reaching is dropped.
16. **Playtest 1 tier:** pickup, drop, opening and closing lockers, the door reach (opening airlocks, ID swipes, access denied, and attaching C4 to a wall), and one kneel loop shared by C4 defuse and CPR. Arming C4 in hand plays nothing. Medical treatment on others (all instant, so it would need a short kneel oneshot), search and take-from-character, drinking, levers and other toggles, and the shove reaction come later. *Revised at the first hand check:* the owner moved light switches into this tier. They wait for contact like the lockers (Decision 15).
17. **The kneel is full-body and rooted.** It plays for as long as a `DelayedInteraction` runs; movement input cancels the interaction (the existing `CancelOnMove`), and the body blends back to standing.
18. **The active hand reaches,** mirrored for the left hand as the swings are. Two-handed clips (floor pickup, put-down, kneel) ignore the hand.
19. **Pickup height:** an item less than 0.5 m above the feet plays the floor clip, anything higher the table clip. A drop onto the floor plays the put-down bend; a drop onto a table plays the table clip reversed.
20. **Network:** oneshots go out as an observers RPC (`PlayInteraction`, table index and hand); a late joiner does not need a reach that is already over. The kneel is state, one snapshot bit (`InteractingKneel`), so anyone joining mid-defuse sees it. This keeps `ActiveTrigger` as it is.
21. **Facing follows the throw:** standing, the body yaw-snaps toward the target over about 0.1 s; moving, only the torso twists, so the legs never stall.
22. **A floor pickup while moving** falls back to the table reach. The full bend plays only when standing still.
23. **Priority:** a melee swing, a throw, the kneel and an aimed two-handed weapon all win over an interaction clip, which is then skipped (the effect still lands, Decision 15). A new interaction clip during another one restarts it with a short crossfade.
24. **Lockers:** open plays the `Opening` take's first reach (frames 15–132), close its second (153–300). Lock and unlock play only the reach (15–45 and back).
25. **One PR** for Phase 4, carrying the draft commit and the main-hud §7 throw-mode entry. If the kneel stalls, the oneshots ship and the kneel is recorded here as open.
26. **Tests:** EditMode covers the table and its imports, plus the contract that a skipped clip still applies its effect (at its contact time since the Decision 15 revision). One scenario covers a locker (instant), one a defuse (the kneel bit is set, then cleared on cancel and on completion). The owner checks the look by hand.

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
**Phase 3 findings (2026-11-09):**

- **Clips** ([`ThrowAnimation`](../../Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/ThrowAnimation.cs) is the table). Release frames were measured from the right hand's path (peak forward speed, hand in front of the head). Overhand `Mix_Throw` is trimmed to frames 8–56 and releases at 25, 0.57 s after the click. The explosive lob is `Mix_RunAndThrowGrenade`, trimmed to 36–64 with its release at 51 (0.5 s). Only the Upper Body plays, so the run in its legs never shows. `Grenade Throw` was dropped: it opens with a pin pull and walks 2.7 m, so its release comes about 2 s after the click (0.8 s even from the backswing). Both states replace the old Upper Body `Throw` state, whose baked `HumanThrow` posed nothing. They are mirrored on `MirrorUpperBody` and leave for the hold pose by `ArmHold`. `AttackVariant` picks the clip.
- **Layer weight:** Peaceful keeps the Upper Body layer at 0, so `AnimationOrchestrator` raises it at 12/s for the length of a throw, on the owner (predicted) and on observers (trigger consumed).
- **Throw mode:** `R` is `Hotkeys/Throw Mode`, rebindable in settings. Clicks route through `CombatInteractionNetwork.TryRunThrowPrimary` before armed interactions and intent, so throw mode works in Help and Harm alike. The overlay (`ThrowModeOverlayView`) has no reticle or text, at the owner's request after the first hand check. It is a dashed flight arc and a ring on the floor where the item will land, drawn with `Painter2D` from the same launch the server uses and cut at the first collider in the way. Green means the item lands at the cursor, rust means the cursor is past its range, and a red ring with no arc means it cannot be thrown. `Esc` also opens the game menu, as it already does in armed mode. The first hand check also found that the armed-interaction stylesheet never loaded: the prefab pointed at a stale local file id (interactions-runtime Pitfalls).
- **Rules** ([`ThrowRules`](../../Assets/Scripts/SS3D/Systems/Inventory/Items/ThrowRules.cs)): range is 10 m up to 0.5 kg and falls linearly to 3 m at 5 kg. Above 6 kg (the hardsuit) an item cannot be thrown. The weight counts the stack and anything stored inside. Out-of-range aims land at max range on the same heading, on the floor. Flight takes `distance / 8 m/s`, between 0.3 and 1.2 s, as the old minibomb throw did.
- **Server** (`CombatInteractionNetwork.Throw.cs`, a partial, so `Human.prefab` is untouched): `CmdThrow` checks the hand, schedules the launch at the clip's release time, and at release checks again. Decision 13 cancels the throw if the item is no longer in the active hand, the hand cannot be used (knockdown, arms disabled), or the body is staggered. The item leaves from shoulder height in front of the throwing hand, pulled back from walls.
- **Minibomb:** the radial entry is gone. `TimedCharge` implements `IThrowReleaseHandler`: every explosive uses the lob, and a minibomb arms in `ServerOnThrown`, so an already running fuse keeps its deadline.
- **Facing (Decision 12):** standing, or in combat, `HumanoidController.HoldFacing` snaps the owner's body to the target until 0.15 s after release. Walking in Peaceful, the body keeps facing the walk and `HumanoidIkController.ApplyThrowTwist` turns spine and chest toward the target, up to 70°, from the throw yaw the owner sends as aim. The first build held the facing while walking too, and the owner saw the feet freeze: Peaceful's strafe clips sit at ±1 on VelX, so a sideways walk of 0.3 blended mostly into idle. Measured after the fix: the walk cycle runs through a 90° throw while the head turns about 90° toward the target.
- **Measured** in `ThrowScenarios` on a live host: a wrench leaves 0.61 s after the click and first lands 0.11 m from the aim point; an oxygen tank aimed 8 m out lands at 2.84 m (range 3 m); a knockdown 0.1 s into a minibomb throw keeps it in hand and unarmed. Impact sounds needed no work: `NoisyCollision` already plays on every item collision above 1 m/s.

- **Done when** an ordinary item and a minibomb can each be thrown at a point within range, standing and running; out-of-range targets show a clamped marker and land at max range; the arm motion matches the release on an observer client; a knockdown before release keeps the item in hand. Tests cover launch velocity reaching the target point, the weight-to-range bands, and the cancel rule.

### Phase 4 — interaction animations

Every interaction today happens with no body motion: a locker opens, an item jumps into the hand, a light changes while the player stands still. Phase 4 gives the common ones a short upper-body or full-body motion, driven from a table as the swings and the throw are.

**Owner-supplied clips** (`~/Downloads`, 2026-11-09), measured with `AnimationMode` sampling (Phase 1 findings). All three are slow single takes with long holds, so each needs a trim and a playback rate, as the get-up clips did:

| Clip | Length | Motion (source frames, 30 fps) | Use |
|---|---|---|---|
| `X Bot@Opening.fbx` | 11.0 s | Two takes. The open: reach to a handle at shoulder height (frames 15–45), sweep it aside (60–96), arm back at the side by 132. Frames 153–300 are a second reach (a close) | Opening, closing, locking and unlocking lockers; probably crates and other hinged storage |
| `X Bot@Picking Up.fbx` | 9.6 s | A waist-height pick: the right hand bottoms out at 0.60 m around frame 66 and comes back to the chest by 190; the hips stay up | Picking up from tables and counters |
| `X Bot@Pulling Lever.fbx` | 6.3 s | Reach up to 1.3 m (24–42), slow pull down (42–105), hand back at the side by 126 | Light switches (`LightSwitchController`), the boombox and atmos port toggles (`GenericToggleInteractionTarget`) |

**Mixamo shortlist** (`Tools/mixamo/mixamo.py`, previewed 2026-11-09). The game's existing interactions, each matched to a catalogue clip:

| Interaction (generic name) | Clip, Mixamo id | Notes |
|---|---|---|
| `Pickup` from the floor | Picking Up An Object, `c9c6d7aa` (4.2 s) | Two-handed bend to the floor. The owner's clip covers table height; choose by the item's height above the feet |
| `Drop` | Putting Down An Object, `c9c6d86d` | The same bend, in reverse |
| `OpenAirlock`, ID swipes, access denied | Attempting To Open A Door, `c9c84f3e` | One arm reaches forward at chest height and holds |
| `DefuseC4` and `Perform CPR` (Playtest 1); later `Search`, `TakeFromCharacter` and medical treatment on others | Man Working On Device, `c9c6cf65` | Down on one knee, both hands working low. One clip for every "hands busy at floor level" interaction; it loops while a delayed interaction runs. Arming and attaching C4 are instant, so they do not kneel (Decision 16) |
| `Perform CPR` | Administering Cpr To A Victim On The Ground, `c9c959ad` | Kneeling compressions; loops for the CPR delay |
| `PourSubstance` into yourself (drinking) | Male Drinking, `c9c63255` | Hand to mouth and back |
| Disarm (deferred, combat §11) | Standing Reaction To Shove, `c9c687a7` | Plays on the victim; disarm itself is not built yet |

Rejected: *Kneeling And Inspecting Element With Hands* holds one hand up beside the head. *Rocking Back And Forth As If Dizzy* is too subtle to read at game distance. *Opening Cabinet Door* is covered by the owner's `Opening`. No catalogue clip presses a wall button (*Attempting To Open A Door* is the closest), and typing exists only seated.

**Deferred past Playtest 1:** *Administering Cpr*, *Male Drinking* and *Standing Reaction To Shove* stay on the shortlist; until then CPR uses the kneel (Decision 16).

**Build:**

- Import the owner's three clips and the four Mixamo clips (Clips table). Measure trims and rates and record them in the findings.
- An `InteractionAnimation` table, like `ThrowAnimation`: generic interaction name → clip, trim, rate, mirrored or two-handed, Upper Body or full body, oneshot or loop. The rebuild reads it.
- Oneshots on the Upper Body over locomotion, raised the way the throw raises the layer in Peaceful; facing, height choice, fallback and priority per Decisions 18–23. The server sends `PlayInteraction` when the interaction runs; the owner plays it locally too.
- The kneel as a full-body loop on `InteractingKneel`, set by the `DelayedInteraction` tracker for defuse and CPR and cleared on completion, cancel or interrupt.

**Phase 4 findings (2026-11-09):**

- **Clips** ([`InteractionAnimation`](../../Assets/Scripts/SS3D/Systems/Entities/Humanoid/Body/InteractionAnimation.cs) is the table; files in `Assets/Art/Animations/Interact/`). The Mixamo *Picking Up An Object* download is the same take as the owner's `Picking Up`, frame for frame, and neither is a table-height pick: it is a one-handed bend with the hand down to about 0.3 m. It is now the floor pickup. The table pickup and the table drop use a short cut of *Attempting To Open A Door*, whose hand goes forward at about table height. *Putting Down An Object* is one-handed too. Decision 19's height rule stands; only the clip assignment changed. Measured trims at 30 fps, with contact frames and times after the Decision 15 revision: reach 12–44 at 1.75× (contact 24, 0.23 s), door reach 10–66 at 1.5× (instant), locker open 24–126 at 2.25× (54, 0.44 s), close 162–294 at 2.25× (192, 0.44 s), tap 22–60 at 1.75× (40, 0.34 s), floor pickup 36–186 at 2.5× (66, 0.40 s), put-down 114–246 at 2.75× (150, 0.44 s), kneel loop 60–132 at 1×, light switch (the owner's `Pulling Lever`) 27–66 at 1.75× (42, 0.29 s). The lever take holds the hand up for two seconds after the press, so the exit blend brings the arm down. A light switch's interaction is the shared `Toggle` (the boombox and valves use it too), so the dispatch picks the clip only when the target is a `LightSwitchController`. One FBX can carry several clips (the locker takes), so these import through their own `ConfigureInteractionImports`. `Pulling Lever` is the light switch.
- **Animator:** five Upper Body reach states, mirrored on `MirrorUpperBody`, entered from Any State on `Interact` + `InteractClip` (self-transitions on, so a new reach restarts), leaving for the hold pose by `ArmHold`. On the Base Layer, two floor bends, also mirrored, leave at their exit time or as soon as `Speed` passes 0.1, and `Interact Kneel` loops while the `InteractKneel` bool is set. The orchestrator raises the Upper Body layer for a reach (as for a throw), and drops it and the Off Hand layers to 0 for a bend or the kneel. The ranged aim correction stands aside while one plays.
- **Network (Decision 20, revised Decision 15):** `InteractionController.Animation.cs` (a partial, so `Human.prefab` is unchanged). Every client dispatch site calls `BeginOwnerInteraction` before its command: it plays the clip, holds the facing until contact plus 0.3 s, and drops the click while a waiting action is still reaching. Both server dispatch paths go through `ServerRunWithAnimation`. It reads the clip before the interaction runs (so a locker is read before its door toggles), sends `RpcPlayInteractionAnimation(clip, yaw)` to the other peers (`ExcludeOwner`), and either runs the interaction at once (door reach, kneel) or holds it as the one pending hand action. `TickPendingHandAction` runs it at the contact time if the body can act, the active hand is the same and the interaction's `CanInteract` and gameplay gates pass again; otherwise it rejects the interaction to the owner. Each peer applies the priority and moving rules itself (`PlayInteraction`, `ResolvePlayback`), and twists the torso through the throw's `ApplyThrowTwist`, which now also covers the reach states. A moving owner plays the standing reach while the server keeps the floor bend's contact time. The kneel bit is snapshot bit 29, set when a kneeling `DelayedInteraction` is tracked and cleared on cancel or end. The drop hotkey (`Hand.CmdDropHeldItem`) and dragging an item from the inventory into the world (`HumanInventory.CmdPlaceItemInWorld`) bypass the interaction pipeline, so they call `BeginOwnerHandAction` and `ServerRunHandAction` themselves.
- **Ending a reach (owner hand check, light switch):** the arm snapped back. Every reach exited into `Hold Default`, which is the combat guard (hand raised at chest height), and in Peaceful the layer faded out in under 0.1 s while the arm blended toward that guard. Now every Upper Body reach ends on its take's own lowering, with the hand back at the side (0.78–0.80 m on the Human rig). The lockers' takes already end there. The others chain a return segment from the same FBX after a 0.15 s blend, with the poses within 5 cm at each seam: door take 108–132 after the table and door reaches, lever take 105–132 after the light switch, and the second locker take 264–306 after the lock tap. The layer then fades over 0.33 s, ending as the arm reaches the side, so nothing blends toward the guard in Peaceful. In combat stances the exit into the hold pose at 0.95 is unchanged.
- **Access denied plays nothing.** A refused airlock never runs its interaction, so no clip goes out. The motion appears only when the door accepts the click.
- **Measured** in `InteractionAnimationScenarios` on a live host: before the revision a floor wrench was in the hand 0.05 s after the click. Now it stays on the floor while the bend plays and lands in the hand 0.45 s after the click (contact 0.40 s), and the body turns to face it. A knockdown 0.1 s into the pickup leaves the wrench on the floor, and a second click during the reach is dropped. A reach during a throw's wind-up is skipped and plays once the throw ends. A defuse kneels within 0.5 s, stays down for the 3 s delay, and stands on completion and when the body moves 0.5 m (the charge stays armed). The room has no locker, so the instant-interaction scenario is the pickup, not a locker (Decision 26). Locker and light switch states are covered by EditMode only, until the owner's hand check (the room has neither).

- **Done when** the Playtest 1 tier (Decision 16) plays on the owner and on observers, faces its target, and never delays its effect; defuse and CPR kneel for their full delay and stand up on cancel and on completion. Tests per Decision 26.

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

- **Throw mode in the design docs.** Resolved 2026-11-09: at the owner's request, `main-hud.md` §7 now has throw mode.
- **Phase 4** needs a grilling before it is built (its open questions above).
