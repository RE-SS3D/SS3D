---
name: Item orientation
overview: Give held, floor and icon item rotations one explicit contract each (a calibrated hand socket, a Rest pose and an Icon pose), one choke point per context, an authoring tool and tests that catch regressions, instead of per-item and per-call-site fixes
todos:
  - id: a-socket-runtime
    content: "A1: GripSocket on each hand display + HeldItemGrip math; ContainerItemDisplay uses the socket when calibrated (legacy path stays as fallback)"
    status: completed
  - id: a-socket-recipe
    content: "A2: HandGripSocketCalibration recipe (SS3D → Inventory → Calibrate Hand Grip Sockets, also in Run All Human Prefab Recipes) bakes the sockets from the idle pose"
    status: completed
  - id: a-tests
    content: "A3: EditMode HeldItemGripTests (math) + HandGripSocketContentTests (Human.prefab displays calibrated)"
    status: completed
  - id: a-run-and-verify
    content: "A4: run the recipe in the Editor, commit the Human.prefab diff, validate.sh --steps compile,editmode,scenarios:Combat; pick up M4, taser, boombox, wrench in idle/walk/Harm and at spawn"
    status: completed
  - id: a-remove-legacy
    content: "A5: once A4 is green, delete the legacy character-root grip and the #158 pending-grip LateUpdate"
    status: completed
  - id: d-contact-sheet
    content: "D1: SS3D → Inventory → Render Item Pose Contact Sheet: icon, held (idle) and floor pose of every Item prefab in one PNG"
    status: completed
  - id: b-pose-fields
    content: "B1: Item.RestRotation / IconRotation fields, migrated from the root rotation by a recipe; Icon defaults to Rest"
    status: pending
  - id: b-choke-point
    content: "B2: one ItemPlacement.WorldRotation(item, yaw) used by every spawn/drop/place/throw path; SpawnItem's identity special case goes"
    status: pending
  - id: b-icon-camera
    content: "B3: icon generator renders from the Icon pose with a fixed camera (not object-relative); fix the taser via its Icon pose"
    status: pending
  - id: c-pose-inspector
    content: "C1: Item Pose inspector: hand (reference pose), floor and icon previews side by side, rotate handles and 90° snaps"
    status: pending
  - id: d-audit
    content: "D2: AssetAudit: every pickable Item has an AttachmentPoint; Rest pose lies flat unless flagged upright"
    status: pending
  - id: docs-sync
    content: "update-system-docs per phase: inventory map Start here, Pitfalls (#158, grip pitfall), Verified stamp"
    status: pending
isProject: false
---

# Item orientation

No design doc governs this: it is presentation plumbing under [inventory](../architecture/systems/inventory.md). Touches inventory (hold, spawn, place, icons), [tile](../architecture/systems/tile.md) (map-editor placement, holograms) and the Human prefab recipes ([entities-prefab-recipes](../architecture/systems/entities-prefab-recipes.md)). No milestone gate depends on it.

## Problem

Held, spawned, placed and icon rotations keep going wrong item by item, and each fix is local. Three causes:

1. **The grip depends on the animation frame at pickup.** `ContainerItemDisplay` sets the grip pivot's world rotation to `characterRoot.rotation * AttachmentPoint.localRotation`, but the pivot is a child of the animated hand bone. The item's rotation relative to the hand is frozen at whatever pose the hand had when the item attached. Loadout items get the bind pose (#158, patched by a 60-frame `LateUpdate` re-grip). Pickups mid-walk, mid-swing or in the Harm stance get that frame's angle (the "rare ~90° off" pitfall).
2. **The floor pose is implicit.** The rest pose is the prefab root's rotation (M4 `-90° X`). Every spawn path must compose it (`GetWorldFacing`); some pass `Quaternion.identity` and rely on `SpawnItem` mapping identity to rest. A new path that sets rotation directly breaks it again.
3. **Icons inherit the floor pose.** `RuntimePreviewGenerator` points the camera at `previewObject.rotation * direction` with world up. The taser rests at `+90° X`, the M4 at `-90° X`. The taser lies on its other side, so its icon is upside down. Most items are right by luck of how they were rested.

## Approach

One contract per context, each with a single owner:

| Context | Contract | Owner |
|---|---|---|
| Held | `AttachmentPoint` is the item root's rotation in **character space, in the reference (idle) pose**. A calibrated socket on each hand turns that into a constant hand-local rotation. | `GripSocket`, `HeldItemGrip`, `ContainerItemDisplay` |
| Floor | `Item.RestRotation` (Phase B), composed with yaw | `ItemPlacement.WorldRotation` |
| Icon | `Item.IconRotation` (Phase B), defaults to Rest, fixed camera | `Item.GenerateIcon` |

### Phase A: calibrated hand sockets

- `GripSocket` sits on each `ContainerItemDisplay` display transform (hands, torso). It stores `ReferenceRotation`: the rotation from the display to the character root while the rig plays the Animator's default state (idle).
- The grip pivot's **local** rotation is `ReferenceRotation * AttachmentPoint.localRotation`. It does not read the character root or the hand's current pose, so it is the same at spawn, mid-walk and in any stance, and the `LateUpdate` re-grip is not needed.
- In the reference pose the result equals the legacy formula, so the 87 authored `AttachmentPoint`s keep their meaning: no item is re-authored. An item that looked right when picked up standing still looks the same; one that came out wrong when picked up mid-animation now comes out right.
- The recipe poses an instance of `Human.prefab` with `Animator.Rebind()` + `Update(0)`, measures every display, and writes the sockets into the prefab. It refuses to write if a hand did not move from the bind pose (the Animator did not pose the rig).
- Until the recipe had run, `ContainerItemDisplay` fell back to the legacy path. A5 removed that fallback: an uncalibrated display now logs a warning once and uses an identity reference.
- `AttachmentPointAlt` (left hand) is unchanged in A. A mirrored socket could make it optional; left for a later pass once the contact sheet shows how many items need it.

### Phase B: explicit Rest and Icon poses

- `Item.RestRotation` and `Item.IconRotation`, filled by a recipe from today's root rotation; `WorldRestRotation` reads the field.
- `ItemPlacement.WorldRotation(item, yaw)` is the only world-rotation helper; `SpawnItem`, `DropInteraction`, `ItemUtility.Place`, `PlacedItemObject`, `ConstructionHologram`, `DisposalOutlet`, throws and console spawns go through it.
- The icon camera stops following the object: the preview applies `IconRotation` and renders from a fixed direction with world up. The taser is fixed by setting its `IconRotation`, not by code.
- Considered: moving the rest rotation onto a child visual so every root stays at zero rotation, which makes identity spawns correct with no helper. Rejected for now: it moves colliders and `AttachmentPoint`s on 90 prefabs, and B gives the same single owner with a recipe-sized migration.

### Phase C: make fixes a prefab edit

An Item Pose inspector on `Item` previews the three contexts (item in a Human hand in the reference pose, on a floor tile, the icon) with rotate handles and 90° snaps that write `AttachmentPoint`, `RestRotation` and `IconRotation`. A play-mode variant on the F1 debug menu adjusts the held item live and writes it back to the prefab in the Editor.

### Phase D: catch regressions

- **Contact sheet** (D1, do it first after A): one PNG with icon, held and floor views of every Item prefab, for a one-minute visual review. It catches upside-down cases no assertion can judge.
- **AssetAudit** (D2): every pickable Item has an `AttachmentPoint`; the Rest pose lies flat (longest bounds axis horizontal) unless the item is flagged upright.

## Verification

- A: `Testing/ui/validate.sh --steps compile,editmode,scenarios:Combat`. `PistolHoldScenarios` covers the two-handed grip and muzzle direction in each hand. Manual: spawn with a loadout, pick up M4, taser, boombox and wrench while idle, walking and in Harm; drop and re-pick.
- B: contact sheet before/after; the taser icon is upright.

## Implementation notes

- A1–A3 landed without a Unity Editor in the session. The runtime code falls back to the legacy grip until A4 runs the recipe, so the build behaves as before until then. `HandGripSocketContentTests` fails until the recipe has been run and `Human.prefab` committed: that failure is the reminder.
- A4 (2026-11-11): recipe run in the Editor after merging develop; it calibrated `hold.r`, `hold.l` and `IDHold` and added three `GripSocket`s to `Human.prefab`. `validate.sh --steps compile,editmode,scenarios:Combat` passed (EditMode 4407/4407, Combat 20/20). Owner hand test passed the same day: held items look right; the taser icon is still upside down, as expected until B3.
- A5 (2026-11-11): removed the legacy grip, `PendingGrip` and the `LateUpdate` re-grip. `TestHuman.prefab` nests the same hands without sockets; it has no InteractionController and nothing spawns it, so it was left uncalibrated.
- D1 (2026-11-11): `ItemPoseContactSheet` renders 107 Item prefabs (Assets/Content) as icon, right hand, left hand and floor cells into `artifacts/item-poses/item-pose-contact-sheet.png`, with a CSV index (flags, whether `AttachmentPointAlt` is set). It reuses the real paths: `Item.GenerateIcon`, `ContainerItemDisplay.PlaceAtDisplay` (extracted from the attach handler) on `Human.prefab` posed by `HandGripSocketCalibration.WithReferencePose`. `RuntimePreviewGenerator.BoundsTarget` frames the held item while the whole body renders. First sheet: 3 prefabs lack an `AttachmentPoint` (HumanEarRight, HumanTorso, HumanBrain); 91 of 107 have an `AttachmentPointAlt`. Body-part items frame the whole body (their skinned bounds span it).
