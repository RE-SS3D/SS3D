> Implements: Documents/design/fov.md §2 (what blocks sight), §3 (what hidden looks like), §4 (people, things and what they give away), §5 (sight from the eyes), §6 (one line of sight)
> Touches systems: rendering, tile, entities, examine, action-messages, map-editor-creative-hooks
> Status: in-progress

**Progress:** P0–P4 built 2026-11-16 on `feature/jolly-newton-n5itdr` (findings under each phase). Left: the owner's look at the 10° shots and a Metastation check of the cast budget; then archive. The map is now [fov](systems/fov.md). Grilled with the owner on 2026-11-16, after an architecture review of the FOV and playtest feedback that the black "shadows don't match the walls". The same grill produced [fov.md](../design/fov.md). Serves [mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M12.

# Field of view rework: walls drawn whole, sharp edges, one raycast batch (Nov 2026)

The FOV mask splits walls down the middle, breaks its edges where they climb a wall, stair-steps at corners, and at low camera tilts reads as a black shadow that doesn't match any wall. Its rays re-cast up to 64 times a frame on the main thread to skip furniture. This effort keeps the black, memoryless fog ([fov.md](../design/fov.md) §1) and Seteron's polar raycast idea, and replaces how rays find occluders and how wall pixels are judged.

## Starting state (2026-11-16, `e8905cebb`)

Read directly from code and prefabs; the [rendering](systems/rendering.md) map is stamped `f481dc3b1`.

- **Producer.** `VisionSubSystem` (process-wide, `SystemsBootstrap` `AddComponent`, so every `[SerializeField]` is its field default) casts `3 × 360` rays from `Entity.ViewPoint` against every layer except a few, in waves: each wave is `RaycastCommand.ScheduleBatch(...).Complete()` on the main thread, up to `MaxOccluderSkips = 64`, advancing rays past non-occluders.
- **Occluders.** `ClassifyVisionOccluder` decides per collider: `Walls` layer, or a `PlacedTileObject` Door or non-window Wall, minus `TileObjectSo.blocksVision == false`. Results are cached by collider instance id (cleared at 4096). On a pure client a door leaf hit mid-spawn returns false and the cache keeps it; not reproduced.
- **Wall colliders.** `SteelWall` and `SteelWallReinforced` have a 1.0 × 2.59 × 1.0 root box on Default, so rays stop at the tile edge. Each also carries four `*ViewObstacle` arms (0.35 m thick, half a tile long, `Walls` layer) that are inactive and toggled by nothing, here or upstream. The wall mesh is about 0.44 m thick at eye height (faces 0.22 m from the centre). Airlock leaves are boxes on Default that slide apart; the lintel is `StationCeiling`. Windows and girders are full boxes on Default. `AbstractHorizontalConnector.cs:206` swaps the wall mesh and rotation when neighbours change.
- **Slack.** Each occluder hit is extended by `0.4 / cos` (capped 1.1 m) plus `ShaderDepthSlack` 0.05, with `OcclusionNormal`/`ThinOccluder` special-casing airlock jamb caps. The constants and `VisionFovTests` are fitted to `SteelWallI` and airlock face distances (#145).
- **Map.** R16 polar texture, bilinear. Two hole-filling passes: C# `DilateSimilarDepthPixels` (1.75 m, ±1 bin) and `VisionSampleWallDepth` in `Vision.hlsl` (2.5 m, ±2 texels). `ShaderDepthSlack` and the 1 m near bubble are duplicated in HLSL, kept equal by comment.
- **Render.** `VisionRendererFeature`: a mask pass into a full-resolution texture in the camera's colour format, a composite pass named "Blur" that blurs nothing (upstream's depth-aware blur was dropped in the port) into a temp, then a copy back. The shader tests each pixel's world XZ against the polar map regardless of height, which splits wall tops and breaks edges where they change height.
- **State.** `VisionRenderContext.Enabled` and `.Suppressed` (statics in the rendering assembly) plus `VisionSubSystem._suppressed`; `MapEditorLighting` writes all three. `IsVisible` reads the render static.
- **Consumers.** `ExamineOverlaySubSystem.IsHoverPointHiddenByVision` and `ActionMessageSubSystem.IsVisibleToViewer` (client side, in a `TargetRpc`) call `VisionSubSystem.IsVisible`. `TileOccupancy.BlocksVision` is computed and unread outside tests. Speech occlusion uses Default only; combat cover Default + `Walls`.
- **Dead code.** `SpacePlane.shader` and `Spaceplane.mat` are referenced nowhere; `ViewCastInfo` fields other than `Point`, `DirectionFromAngle` (editor gizmo only) and `transform.position = target.position` do nothing for the game.
- **Upstream** (RE-SS3D, ported in its #1491): one 360-ray batch against a layer mask, no slack, a depth-aware 5 × 25-sample edge blur; per-frame `Color[]` and `TempJob` allocations.

## Decisions

| # | Decision | Why |
|---|---|---|
| D1 | **Rays for edges, tiles for wall pixels.** Rays find the first occluder; floor and other non-wall pixels keep the polar depth test (sharp radial edges). A pixel inside an occluder tile that any ray hit is visible, whole tile, all faces and top. No slack | fov.md §3 (a wall is seen whole); fixes the split wall tops and the black on wall faces in plain view. Per-tile visibility for floors was rejected: diagonal stair steps |
| D2 | **`VisionOccluder` layer** (a free layer index). Only sight blockers live on it: wall arms and core, closed-door leaves. One `RaycastCommand` batch per frame against it; no waves, no `ClassifyVisionOccluder`, no instance-id cache | fov.md §2; removes the 64-wave loop, the component lookups and the stale-cache risk |
| D3 | **Occluder shape: box arms plus a centre block.** Arms 0.44 m thick toward each connected neighbour, a centre block so a lone pillar blocks, sized to the mesh at eye height. Built by a tier-B recipe (`VisionOccluderRecipe`, registered on **SS3D/Rendering/Run Content Prefab Recipes**); the connector switches arms where it swaps the mesh. Airlock leaves move to the layer. An `AssetAudit` test checks occluders exist exactly where `TileObjectSo.blocksVision` is true on a wall-type tile, and that arm extents match the mesh bounds at eye height | Walls are box-shaped at eye height. Boxes are cheap and work on a headless server; a mesh collider would need Read/Write on every wall mesh and may lose mesh data in dedicated-server builds. The audit test catches art drift |
| D4 | **Hit tile per bin.** Tile coord = the tile containing `hit.point − hit.normal × 0.01`, no component lookup. The CPU marks seen occluder tiles in an R8 texture covering the view range around the player; the shader maps world XZ to that texture with tile-grid globals | Links rays to the tile grid without classification; one channel suffices because an unseen occluder pixel already fails the depth test |
| D5 | **No hole filling.** Delete `DilateSimilarDepthPixels` and the shader's neighbour fill. If angular stripes remain on floors after D1 and D7, restore one fill in the shader only, mirrored in `IsWithinDepths` | Both fills existed for stripes on flat walls, which D1 removes; two fills with different thresholds made CPU and GPU disagree |
| D6 | **One owner for vision state.** `VisionSubSystem` holds Off / Suppressed / Active. The rendering assembly reads a per-frame context the subsystem writes; `MapEditorLighting` calls `SetSuppressed` only; `IsVisible` reads the subsystem's own state | Three flags with two writers |
| D7 | **Inward-only soft edge.** The mask pass writes an R8 visibility mask; a separable blur at half resolution; the composite multiplies colour by `mask × blur(mask)` and replaces the camera colour handle instead of copying back. Width about 0.5 % of screen height | fov.md §3 (edges soften inward only); one mechanism for radial, side and outline edges; hides the 1/3° steps |
| D8 | **Shared constants are shader globals** (`_VisionDepthEpsilon`, `_VisionNearBubble`), set by the subsystem | Removes "keep these equal" comments |
| D9 | **People, items and wall mounts are culled whole.** `VisionEntityCuller` turns their renderers off (`forceRenderingOff`, which hides their shadows) when none of three sample points is visible: the footprint centre and both sides across the line of sight. Wall mounts sample the floor tile they face. Their renderers carry a rendering-layer bit (`VisionRenderingLayers.Entity`, next to `DecalRenderingLayers`) that the mask pass skips, so a visible person is never cut by the edge. Fallback if P0 finds the rendering-layers texture unreadable: culling only | fov.md §4. Lights they carry are untouched (fov.md §4, light still travels) |
| D10 | **Cast off the main thread.** Schedule the batch in `LateUpdate` after the view point settles; complete it in `RenderPipelineManager.beginContextRendering`. Budget: under 0.5 ms main-thread time on the Metastation under the `Vision.ViewPoints` and `Vision.VisionMap` markers | Today's cost is the blocking waves; one batch overlaps other late work |
| D11 | **Line-of-sight query for later server use.** `VisionLineOfSight.HasSight(from, to)`: one raycast on `VisionOccluder`, usable without rendering. No server consumer in this effort | fov.md §6; moving action messages and examine to the server is a follow-up |
| D12 | **Own system map**, `systems/fov.md`; `rendering.md` keeps a pointer for the render feature | The domain now owns a layer, a recipe, culling and a query |
| D13 | **Versioning.** No `ProtocolVersion` or `PersistenceEnvelope` bump: nothing new crosses the network or the save, and occluder children carry no `NetworkBehaviour`. The commit that completes P3 carries `Version-Bump: minor` | [game-versioning](2026-10_game-versioning.md) |

## Deviations from design

- **rendering-lighting.md §5, combat.md §3, comms.md §3 (one shared occlusion raycast):** sight gets its own `VisionOccluder` layer; speech (Default) and combat cover (Default + `Walls`) keep their masks. Unifying them is a follow-up grill. Sight stays the most permissive of the three, so "what you can't see, you can't hit" holds ([fov.md](../design/fov.md) §8).

## Phases

### P0: spike

- [x] Confirm a custom URP 17.3 RenderGraph pass can read the rendering-layers texture produced by the Decal Layers depth-normals prepass (`UniversalResourceData.renderingLayersTexture` or equivalent), and that adding a bit to character renderers doesn't change their lighting or decal receipt. Record the answer under D9
- [x] Baseline `Vision.ViewPoints` and `Vision.VisionMap` (Profiler markdown export, `.cursor/skills/analyze-unity-perf`)
- [x] Before screenshots: the playtester's corner at 10°, 45° and 80° tilt, and the doorway from the first owner screenshot
- Validation: none (no code change)

**Findings (2026-11-16):**

- **Rendering layers: yes, with a named layer.** `UniversalResourceData.renderingLayersTexture` is public and written by the depth-normals prepass when the Decal feature has `decalLayers: 1` (ours does); a pass reads it with `UseTexture` and `LOAD_TEXTURE2D_X(_CameraRenderingLayersTexture)` (`DeclareRenderingLayerTexture.hlsl`). Its format follows the number of *named* rendering layers: two today, so `R8_UInt`, and URP masks every renderer's bits with the defined layers (`RenderingLayerUtils.ToValidRenderingLayers`). The entity bit must therefore be a named layer under 8: `VisionEntity` at index 2 in `TagManager` `m_RenderingLayers`, not bit 31 like `SelectionRenderingLayers`. Lights and decal projectors test for overlap with their own masks (Default, `ReceiveWorldDecals`), so an extra bit changes neither.
- **Baseline** on Classic MiniStation (the Metastation template isn't in the repo), editor Play Mode on tomnas, standing at the nearest airlock, 120 frames: `Vision.ViewPoints` 1.91 ms mean / 7.08 ms max, `Vision.VisionMap` 0.21 / 0.67 ms.
- **Shots.** The owner's and playtester's exact spots aren't in the repo, so the doorway and the wall corner beside the airlock nearest spawn on Classic MiniStation stand in for them. `VisionShots` (PlayMode, explicit, `validate.sh --steps scenarios:VisionShots`) writes them with the timings to `artifacts/ui/vision-shots-<UTC>/`; the P0 set is `vision-shots-20261116-134149`.
- **Deviation:** the engine-driven wall connectors swap the mesh in `TileAdjacencyView.UpdateMeshAndDirection`, not `AbstractHorizontalConnector` (its path is only the legacy SyncVar one). P1 hooks the arms there.

### P1: occluders

- [x] `VisionOccluder` layer in `TagManager`
- [x] `VisionOccluderRecipe`: arms and centre block on every wall-type prefab whose tile blocks sight (steel, reinforced, plated girder); remove the inactive `*ViewObstacle` children; airlock leaves to the layer
- [x] Connector switches arms where it swaps the mesh (`AbstractHorizontalConnector`)
- [x] `AssetAudit`: occluders exist exactly where `blocksVision` is true on wall-type tiles; arm extents match mesh bounds at eye height
- [x] Nothing reads the layer yet; the arms sit inside today's 1 × 1 box, so the first hit doesn't change
- Validation: `compile,editmode`

**As built (2026-11-16):**

- **Layer 14 `VisionOccluder`** collides with nothing in the physics matrix; only queries see it. The audit checks that.
- **`VisionOccluder`** (`Systems/Tile/Connections`, not `Systems/Vision`, so Tile doesn't gain a cycle with Vision) sits on the wall root and holds a `VisionOccluder` child with `Centre` plus `North`/`East`/`South`/`West` arm boxes. `TileAdjacencyView` calls `ApplyConnections` after it turns the root, and the holder is reset to grid-aligned, so arm names are world directions. The plated girder's occluder is on its root, above the two connector children, and `GirderStageRecipe.KeepOnly` now keeps that child.
- **Sizes** come from the straight (`i`) mesh sliced at `VisionOccluder.EyeHeight` (1.5 m, the Human `ViewPoint`): steel and reinforced walls 0.432 m, the plated girder's sheets 0.398 m (its frame is see-through); height is the mesh's top. Arms run to the tile edge.
- **Airlock leaves (deviation):** the leaves keep their Default box, because speech (Default) and combat cover (Default + `Walls`) read it; each leaf gets a `VisionOccluder` child box the size of its mesh (0.342 m thick against the 0.8 m collider, 1.93 m tall). Leaves are found as direct children with a solid box and a mesh, on every `Door` tile's prefab.
- **`VisionOccluderRecipe`** lives in `Systems/Tile/Editor` (Systems assembly) so `VisionOccluderAuditTests` can call its `Drift()`; it is registered on **SS3D/Rendering/Run Content Prefab Recipes**.
- **Other queries:** many gameplay casts use every layer but the ceiling (`StationBounds.WithoutCeiling(~0)`), so they can now hit an occluder box. Each sits inside the Default collider it belongs to, so the outer one is hit first from outside; a `RaycastAll` gets one extra hit whose parent is the same wall or door.

### P2: rays and mask (one PR)

- [x] One batch on `VisionOccluder` (D2), scheduled and completed per D10
- [x] Hit tile per bin and the seen-occluder-tile texture (D4)
- [x] Shader: D1 wall rule, D7 mask, blur and composite; constants as globals (D8)
- [x] Delete waves, `ClassifyVisionOccluder`, the instance-id cache, `OccluderSlack`, `OcclusionNormal`, `ThinOccluder`, both hole fills (D5), the copy-back blit
- [x] One vision state owner (D6); `MapEditorLighting` through `SetSuppressed` only
- [x] `VisionLineOfSight.HasSight` (D11); `IsVisible` mirrors D1 for points inside a seen occluder tile
- [x] Profile against the P0 baseline; after screenshots at the same spots
- Validation: `compile,editmode,scenarios:Vision,visual`

**As built (2026-11-16):**

- **Cast.** `LateUpdate` schedules three chained jobs: `VisionRayCommandsJob` (Burst, one ray per bin centre), `RaycastCommand.ScheduleBatch` against `VisionOccluder.Mask`, and `VisionHitsJob` (Burst: depth per bin, R16 upload values, hit points, seen tiles). `RenderPipelineManager.beginContextRendering` completes them and uploads; a frame that never renders is completed by the next `LateUpdate`. The managed copies `IsVisible` reads are taken at completion.
- **Cost** on the P0 spot, editor Play Mode: `Vision.ViewPoints` 0.035 ms mean / 0.29 max, `Vision.VisionMap` 0.047 / 0.094 (baseline 1.91 / 7.08 and 0.21 / 0.67). The first run after a compile is slower while Burst compiles.
- **Map and rule.** `_VisionMap` is point-sampled, so the shader and `IsWithinDepths` read the same bin. `_VisionTiles` is an R8 grid of `2 × (range + 1) + 1` tiles around the eye, origin in `_VisionTileOrigin`. The wall rule needs the point above `_VisionWallMinY`, the body's feet plus `VisionSubSystem.WallMinHeight` (0.08 m): a seen door tile's floor keeps the depth test, so the strip behind a closed leaf stays black. Height is from the body because the station has one plane but the body can stand on a raised floor.
- **Constants as globals:** `_VisionDepthEpsilon` (0.02 m, rounding only), `_VisionNearBubble` (1 m), `_VisionWallMinY`. No constant is duplicated in HLSL.
- **Mask and composite.** `VisionMaskRenderPass` writes an R8 mask at full resolution. `VisionBlurRenderPass` blurs it horizontally into half resolution, then vertically (nine taps, sigma two taps, eight steps across `EdgeWidth` = 0.5 % of screen height), and composites `colour × mask × blur` into a new colour texture that replaces `resourceData.cameraColor`; no copy back. The composite skips when the camera writes straight to the back buffer.
- **State.** `VisionSubSystem.State` is `Off` / `Suppressed` / `Active`; it alone calls `VisionRenderContext.Publish`. `VisionRenderContext.Suppressed` is gone.
- **Shots.** `VisionShots` now also saves each view with the FOV suppressed (`*-open.png`) and logs what a ray toward the door hits; the P2 set is `vision-shots-20261116-141536`. Walls beside the player draw whole at every tilt, and the black keeps to what the eye can't reach past a door jamb or through a half-open door.
- **Tests.** `VisionFovTests` lost the slack cases and gained `HitTile` and `IsPartOfSeenWall` cases. `VisionScenarios`: hugging a wall (ray stops on the drawn face; far face and top shown; next room's floor black), a point behind a wall, and an airlock opening revealing the room.

### P3: people, items and wall mounts

- [x] `VisionRenderingLayers.Entity` bit stamped by recipe on character, item and wall-mount renderers (fallback per D9 if P0 said no)
- [x] Mask pass skips the bit
- [x] `VisionEntityCuller`: three-point check, wall mounts on their facing tile, `forceRenderingOff`
- Validation: `compile,editmode,scenarios:Vision,visual`

**As built (2026-11-16):**

- **Stamped at runtime, not by recipe (deviation).** The culler ORs `VisionRenderingLayers.Entity` (named layer 2, `VisionEntity`) onto every renderer it tracks when it reads the renderer list, the way `PlacedTileObject` already stamps `ReceiveWorldDecals`. A recipe would have meant editing `Human.prefab` and every item prefab, and would miss clothes and items that join a body later.
- **What is tracked.** Spawned entities (`SceneRegistry<Entity>`, registered in `Entity.OnStartClient`, so preview dummies never are), spawned items (`SceneRegistry<Item>` filtered by `IsSpawned`, so inventory icon previews keep drawing) and wall mounts (`ClientWallMounts`, filled from `PlacedTileObject.OnStartClient` for the `WallMountHigh`/`WallMountLow` layers). The local body is tracked for the bit but always shows.
- **Rule.** Bodies and items: the bounds centre plus both sides across the line of sight at the bounds' horizontal radius (0.1–0.6 m), via `IsVisible`. Wall mounts: the floor tile in front (`ClientWallMounts.FacingFloor`, the wall tile plus the mount's `Direction`) 1 m up. Worn and held items, anything under an `Entity`, show or hide with their holder: a renderer belongs to exactly one tracked owner, so a holder and the item it drops never undo each other. Renderer lists are re-read every 30–37 frames, and at once when an item changes parent.
- **`RendererHiding`** (rendering assembly) shares `forceRenderingOff` between reasons: `ClothesDisplayer` now hides covered body parts through it (`CoveredByClothes`), and the culler uses `OutOfSight`. Without it, showing a person would have un-hidden body parts under their clothes.
- **Mask.** `VisionMask.shader` reads `_CameraRenderingLayersTexture` at the pixel and returns visible for the bit; the pass declares `renderingLayersTexture` and passes `_VisionUseRenderingLayers = 0` when it is missing (culling-only fallback).
- **Checked:** the `person` shots (`vision-shots-20261116-144904`): a dummy standing with its middle just past the jamb of an open airlock draws identically with the FOV on and off, and the black stops at its outline.
- **Cost:** `Vision.Cull` 0.34–0.39 ms mean in editor Play Mode for 3 entities, 32 items and 88 wall mounts (editor Mono; per-target interop and three `IsVisible` calls). The ray markers stayed at 0.04–0.13 ms each; they vary run to run on tomnas. If the Metastation's counts make the cull expensive, the per-target test is the part to move into a Burst job.

### P4: cleanup and docs

- [x] Rename `VisionBlurRenderPass` and `VisionMaskBlur` to composite; delete `SpacePlane.shader`, `Spaceplane.mat`, unused `ViewCastInfo` fields, `transform.position` write, dead `[SerializeField]`s; `.editorconfig` naming on public members; ghost check through an `Entity` capability rather than `HumanoidGhostController`
- [x] Rewrite `systems/fov.md`; move the vision pitfalls out of `rendering.md`, dropping the slack pitfall; `FORK_STATUS.md` Vision section
- [ ] Owner looks at the 10° screenshots; decide whether to raise the camera's minimum tilt
- [x] `update-system-docs`; M12 status
- Validation: `compile,editmode`

**As built (2026-11-16):** `VisionCompositeRenderPass` and `Vision/VisionComposite` (the shader keeps its GUID, so the renderer asset's field was renamed in place to `_visionCompositeShader`). `ViewCastInfo`, `DirectionFromAngle` and the `transform.position` write went with P2. View range, cone width and rays per degree are constants (`VisionSubSystem.ViewRange` is public); only the `Show Debug` gizmo toggle stays serialized. Public members are `Target`, `StepCount`, `ViewPoints`, `ShowDebug`. `Entity.HasFieldOfView` replaces the ghost component check. `systems/fov.md` is the new map; `rendering.md` keeps a pointer.

## Tests

- **Delete** the slack tests in `VisionFovTests` (they check the old constants, not behaviour). Keep the `IsWithinDepths` tests.
- **EditMode:** tile from hit point and normal; the D1 rule as the CPU mirror of the shader; the three-point entity check; wall-mount facing tile; occluder `AssetAudit` (D3).
- **Scenarios** (`[Category("Vision")]`, `scenarios:Vision`), each with a `Guards:` line per [testing philosophy](2026-11_testing-philosophy.md):
  - hugging a wall: the next room hidden, the wall beside a door drawn whole (rewrite of the current scenario);
  - an airlock opening reveals the room behind it;
  - a person half behind a jamb is shown, fully behind it hidden, with no shadow.
- **Visual:** the playtester's corner at 10°, 45° and 80°, and the owner's doorway, before (P0) and after (P2, P3).

## Out of scope

- Moving action-message and examine filtering to the server (D11 makes it possible).
- Unifying the speech, combat and sight occluder sets (see Deviations).
- Anything in [fov.md](../design/fov.md) §9.

## Open questions for the owner

- After P2: does the 10° tilt still look wrong, and should the camera's minimum tilt go up? Shots: `doorway-10`, `corner-10` and `person-*` in the latest `artifacts/ui/vision-shots-*` (regenerate with `validate.sh --steps scenarios:VisionShots`).

## Related docs

- Design: [fov.md](../design/fov.md); [examine.md](../design/examine.md) §2; [action-messages.md](../design/action-messages.md); [combat.md](../design/combat.md) §3; [comms.md](../design/comms.md) §3; [construction.md](../design/construction.md) §6–§7
- Maps: [fov](systems/fov.md); [rendering](systems/rendering.md) (URP feature registration); [tile](systems/tile.md); [examine](systems/examine.md); [action-messages](systems/action-messages.md)
- Milestone: [mvp1-nuke-ops.md](../milestones/mvp1-nuke-ops.md) M12
- Policy: [agent-first composition](2026-07_agent-first-composition.md) (tier-B recipes); [editor tooling tiers](2026-07_editor-tooling-tiers.md); [testing philosophy](2026-11_testing-philosophy.md)
