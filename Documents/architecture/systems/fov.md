> Code paths: Assets/Scripts/SS3D/Systems/Vision/, Assets/Scripts/SS3D/Rendering/URP/Vision*, Assets/Content/Resources/Vision/, Assets/Scripts/SS3D/Systems/Tile/Connections/VisionOccluder.cs
> Entry points: VisionSubSystem, VisionLineOfSight, VisionEntityCuller, VisionRendererFeature, VisionOccluderRecipe
> Status: partial
> Verified: 953fcbbdc — 2026-11-16

# Field of view

## Overview

Client fog of war per [fov.md](../../design/fov.md): what the local living body can see right now; everything else is solid black, nothing remembered. Built by [2026-11_fov-rework](../2026-11_fov-rework.md).

- **Sight blockers** are boxes on the `VisionOccluder` physics layer (14), which collides with nothing; only queries use it. Every wall-type tile that blocks sight (steel, reinforced, plated girder; not windows or bare girders) carries a `VisionOccluder`: a centre block and one arm per connected cardinal neighbour, as thick as its mesh at eye height (1.5 m). `TileAdjacencyView` switches the arms when it swaps the mesh. Each airlock leaf carries a box the size of its mesh, so an opening door reveals the room as it opens. Furniture, machines and people never block sight (fov.md §2).
- **Cast.** `VisionSubSystem` schedules one batch per frame in `LateUpdate`: 1080 level rays (3 per degree) from `Entity.ViewPoint` against that layer, filled and post-processed by Burst jobs (`VisionCastJobs.cs`), completed at `RenderPipelineManager.beginContextRendering`. It writes `_VisionMap` (R16, per angular bin the distance to the first blocker) and `_VisionTiles` (R8, one texel per tile around the eye, set where a ray hit a blocker).
- **Rule** (shader `VisionIsVisibleWorld`, CPU `IsVisible`): a point shows when the ray at its angle went at least that far, or when it is inside a wall tile a ray hit and above the body's feet + 0.08 m. Walls in view therefore draw whole, top and faces, and the black starts at their outline (fov.md §3); the floor of a door tile keeps the depth test, so nothing behind a closed leaf leaks.
- **People, items, wall mounts** (fov.md §4): `VisionEntityCuller` shows each whole or hides it whole (`forceRenderingOff`, so no shadow). Bodies and loose items show when the middle or either side across the line of sight is visible; held and worn items follow their holder; wall mounts follow the floor tile they face. Every renderer it tracks carries rendering layer `VisionEntity` (bit 2), and the mask pass shows those pixels, so a shown person is never cut by the edge. Lights they carry are untouched.
- **Render.** `VisionRendererFeature`: an R8 mask at full resolution (`VisionMaskRenderPass`), a separable blur at half resolution, and a composite of colour × mask × blur into a new camera colour target (`VisionCompositeRenderPass`). The edge fades inward only, over 0.5 % of screen height; hidden stays black (fov.md §3).
- **State.** `VisionSubSystem.State` is `Off` (no living body: lobby, ghost, server), `Suppressed` (map editor, via `SetSuppressed`) or `Active`. It alone publishes `VisionRenderContext.Enabled`. `IsVisible` answers true unless `Active`. Ghosts: `Entity.HasFieldOfView`.
- **Gameplay** asks `IsVisible` (examine, public action lines) for what is on screen, or `VisionLineOfSight.HasSight(from, to)` (one linecast on the layer, works without rendering) for the rule itself. No server consumer yet.

## Start here

- `Systems/Vision/VisionSubSystem.cs` — state, cast scheduling, globals, `IsVisible`, `IsWithinDepths`, `IsPartOfSeenWall`, `HitTile`
- `Systems/Vision/VisionCastJobs.cs` — `VisionRayCommandsJob`, `VisionHitsJob`
- `Systems/Vision/VisionEntityCuller.cs` — whole-or-nothing culling and the rendering-layer stamp
- `Systems/Vision/VisionLineOfSight.cs` — `HasSight`
- `Systems/Tile/Connections/VisionOccluder.cs` — layer, holder and arms; `TileAdjacencyView` drives it
- `Systems/Tile/Editor/VisionOccluderRecipe.cs` — builds occluders; `Drift()` for the audit
- `Systems/Tile/PlacedObjects/ClientWallMounts.cs` — wall mounts and the floor tile each faces
- `Rendering/URP/VisionRendererFeature.cs`, `VisionMaskRenderPass.cs`, `VisionCompositeRenderPass.cs`, `VisionRenderContext.cs`, `VisionRenderingLayers.cs`, `RendererHiding.cs`
- `Content/Resources/Vision/Vision.hlsl`, `VisionMask.shader`, `VisionComposite.shader`

(Script paths are under `Assets/Scripts/SS3D/`, content under `Assets/`.)

## Extension points

- **A new wall or door type:** give its `TileObjectSo` the `Wall` or `Door` generic type and the right `blocksVision`, then run **SS3D → Rendering → Run Content Prefab Recipes**. The recipe sizes arms from the prefab's straight (`i`) `AdvancedConnector` mesh; a prefab whose sight-blocking mesh is on a child lists it in `VisionOccluderRecipe.SightMeshChild`. `VisionOccluderAuditTests` fails until the recipe has run.
- **Something else that should block sight** (a future shutter): put a solid box on the `VisionOccluder` layer under a `VisionOccluder` child that moves with it. Never move an existing collider onto the layer: speech reads Default, combat cover Default + `Walls`.
- **Another owner of `forceRenderingOff`** on bodies or items: add a `RendererHiding.Reason` and set it through `RendererHiding.Set`, never the property directly.
- **Server line of sight:** `VisionLineOfSight.HasSight` from eye to eye. Moving action messages and examine to the server is a follow-up (effort Out of scope).

## Pitfalls

- **Raycasts that hit the occluder boxes:** every query using all layers but the ceiling (`StationBounds.WithoutCeiling(~0)`) can hit a `VisionOccluder` box. Each sits inside the Default collider of the same wall or leaf, so from outside the outer collider is hit first, and the hit's parent is the same object; a `RaycastAll` just gets an extra hit. Keep new boxes inside their object's own collider.
- **`GirderStageRecipe.KeepOnly` strips unknown children** of the plated girder; it keeps the `VisionOccluder` holder by name. Another recipe that rebuilds wall prefabs must do the same, or the audit fails.
- **The entity bit must be a named rendering layer under 8.** URP sizes the rendering-layers texture by the number of *named* layers (R8_UInt with three) and masks every renderer's bits with the named ones, so an unnamed bit such as 31 never reaches the texture. Found in the P0 spike.
- **`forceRenderingOff` has several owners:** `ClothesDisplayer` hides body parts under clothes, the culler hides whole people. Setting the property directly from one undoes the other: showing a culled person would also show the body parts under their jumpsuit. Both go through `RendererHiding`.
- **Inventory icon previews instantiate item prefabs** that register in `SceneRegistry<Item>`. The culler only touches spawned items (`IsSpawned`); otherwise a preview staged far off-station would be hidden as out of sight and its icon would render empty. Preview dummies never register as entities because registration is in `OnStartClient`.
- **`foreach` over an `IReadOnlyList` in a per-frame loop boxes its enumerator.** The culler's first version did that over three registries and showed a 36 ms GC spike under `Vision.Cull`; it uses index loops.
- **Point sampling, not bilinear:** `_VisionMap` is read with `LOAD` of the bin a pixel's angle falls in, which is what `IsWithinDepths` does. Bilinear interpolation across a depth jump would draw a smear past wall edges and disagree with gameplay. The inward blur hides the 1/3° steps.
- **Ghost and map editor:** a ghost body has no field of view (`Entity.HasFieldOfView`), so `State` is `Off` whatever the map editor does; closing the editor must not bring the mask back for a ghost, and respawning into a living body does. `LobbyLifecyclePilot.Sandbox` checks both.
- **Map uploads on the per-frame path:** no `new Color[]` / `SetPixels`; the jobs write native arrays and `SetPixelData` uploads them (an earlier `Color[]` path was a multi-MB-per-frame GC hotspot).
- **Ray hits are a frame old for moving doors:** colliders follow the leaf animation on the physics step (`Physics.autoSyncTransforms` is off), so an opening door reveals the room a fixed step late. Scenarios wait for a `WaitForFixedUpdate`.

## Tests

- EditMode `VisionFovTests` — `IsWithinDepths`, `HitTile`, `IsPartOfSeenWall`; AssetAudit `VisionOccluderAuditTests` — occluders match tiles and meshes, layer collides with nothing.
- PlayMode `VisionScenarios` (`scenarios:Vision`) — hugging a wall, a point behind a wall, an airlock opening, a person half behind a jamb.
- `VisionShots` (explicit, `scenarios:VisionShots`) — doorway, corner and person pictures at 10°/45°/80° with and without the FOV, plus `Vision.*` marker timings, into `artifacts/ui/vision-shots-<UTC>/`.

## Depends on / Used by

- **Depends on:** [tile](tile.md) (wall/door prefabs, `TileAdjacencyView`, `TileObjectSo.blocksVision`), [entities](entities.md) (`Entity.ViewPoint`, `LocalPlayerObjectChanged`, `HasFieldOfView`), [inventory](inventory.md) (`SceneRegistry<Item>`, `ClothesDisplayer`), [rendering](rendering.md) (URP renderer, Decal feature's rendering-layers prepass)
- **Used by:** [examine](examine.md) (`ExamineOverlaySubSystem` hides targets under the black), [action-messages](action-messages.md) (public lines need sight), map editor (`MapEditorLighting` → `SetSuppressed`)

## Related docs

- Design: [fov.md](../../design/fov.md)
- Effort: [2026-11_fov-rework.md](../2026-11_fov-rework.md)
- Milestone: [mvp1-nuke-ops.md](../../milestones/mvp1-nuke-ops.md) M13
