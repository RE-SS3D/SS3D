> Code paths: Assets/Scripts/SS3D/Systems/Selection/, Assets/Scripts/SS3D/Rendering/URP/
> Entry points: SelectionSubSystem, SelectionController, SelectionPickRendererFeature
> Status: shipped
> Verified: f481dc3b1 — 2026-11-05

# Selection

## Overview

Shader-ID mesh picking replaces screen raycasts for interaction targeting. Each `Selectable` gets a unique render color and registers as a `SelectionPickContext.ISelectionPickSource`; the URP pick pass draws ID colours with `DrawMesh` + a **transient** MaterialPropertyBlock (MeshRenderers stay MPB-free for SRP Batcher). Skinned meshes keep a permanent selection MPB (few characters). Sources are indexed in a **4 m XZ pick grid**; with a live cursor ray, `CollectPickDraws` only walks cells along the ray (±1 Moore pad) plus a small always-scan list for skinned/mobile sources — not every registered Selectable. Per-source collect still skips HashGrid-AOI-disabled renderers, frustum misses, and bounds the cursor ray does not hit (`Bounds.IntersectRay`, slight expand). Floors/walls stay examinable under the cursor without redrawing the view. `SelectionCamera` skips the whole pick request while the pointer is over UI. Readback identifies the hover target. `InteractionController` routes client interaction targeting through this system and drives `InteractionOutlineView` from the current hover; the server validates using `NetworkObject` and interaction point.

Outline shells and other auxiliary meshes use `SelectionRenderingLayers.ExcludeFromSelectionPick` so they stay out of the ID pass (avoids hover flicker / z-fight and outline bleed into item icons). The pick pass depth-tests against the scene depth buffer, so auxiliary meshes must also not **write depth** in front of their owner (outline hull: `ZWrite Off`, see [interactions-runtime](interactions-runtime.md) Pitfalls). Clear outlines on inventory pickup so the green shell does not stick after Take.

`SelectionCamera` clears the current selectable while `InputInterface.IsPointerOverInterface()` is true, so examine/hover/outlines do not target world objects through registered UI Toolkit panels (Main HUD, machine UI, radial).

## Start here

- `Assets/Scripts/SS3D/Systems/Selection/SelectionSubSystem.cs` — subsystem entry point
- `Assets/Scripts/SS3D/Systems/Selection/SelectionController.cs` — per-frame hover/update logic
- `Assets/Scripts/SS3D/Systems/Selection/Selectable.cs` — component marking pickable meshes; `PickWorldCenter` / `PreferAlwaysScan`
- `Assets/Scripts/SS3D/Systems/Selection/SelectionCamera.cs` — async pick buffer readback
- `Assets/Scripts/SS3D/Systems/Selection/SelectionTargetUtility.cs` — ray / closest-point interaction point for range checks
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickRendererFeature.cs` — URP render feature for ID pass
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickContext.cs` — pick context, spatial index, ray/frustum cull
- `Assets/Scripts/SS3D/Rendering/URP/SelectionRenderingLayers.cs` — pick-pass exclude bit (lives in Rendering.URP to avoid assembly cycles)

## Extension points

- Add `Selectable` **and** a collider (usually `BoxCollider` on wall mounts) to mesh roots that should be pickable *and* range-checked. Pick works from materials alone; [interactions-framework](interactions-framework.md) `RangeCheck` / drop normals need a resolved point from colliders (or transform fallback).
- Implement `IExaminable` on selectables for [examine](examine.md) integration.
- Auxiliary meshes (outlines, FX): set `SelectionRenderingLayers.ExcludeFromSelectionPick` on their rendering layer mask.
- `MachineInterfaceHost` disables `UIDocument` when closed to avoid interfering with the pick pass — follow this pattern for overlay UI.
- Moving / skinned pick sources: set `PreferAlwaysScan` (Selectable does this when it has skinned meshes) so the spatial index still invokes them after they leave their register cell.

## Pitfalls

- **Pick readback must stay async:** a synchronous `ReadPixels` of even one pixel waits for the GPU, so the CPU and GPU stop overlapping and the frame costs their sum: 5 ms of a 7 ms frame at 1440p. `SelectionCamera` uses `AsyncGPUReadback`, so hover trails the cursor by a few frames, and falls back to `ReadPixels` only where async is unsupported, such as headless players. Results load into `_readbackTexture` in the target's own format because picks match by exact `Color32`; no Y flip is needed (1,081 comparisons on Vulkan, 0 mismatches). Hit 2026-11-05.
- **Do not write `_SelectionColor` onto MeshRenderer MPBs:** permanent selection MPBs break SRP Batcher / GPU Instancing on every tile. Register via `SelectionPickContext` and let the pick pass supply colour per `DrawMesh`. SkinnedMeshRenderer is the exception (DrawRenderer needs the block). Hit 2026-07-28 (Metastation).
- **`DrawMesh` pick is not frustum-culled by Unity:** AOI/underfloor only disables off-observer `Selectable`/MeshRenderers. Without `SelectionPickContext.IsInPickFrustum` in `CollectPickDraws`, every AOI-visible tile is `DrawMesh`'d (Metastation ~5 ms/frame). Keep the pick-camera plane test; do not confuse it with HashGrid AOI. Hit 2026-07-28.
- **Inline ray test alone does not cut CPU:** `IsNearPickCursor` inside `Selectable.CollectPickDraws` still walks every registered source (~47k on Metastation) and pays Unity null-check marshalling. Keep the **4 m pick-grid spatial index** in `SelectionPickContext.CollectPickDraws` (ray DDA + Moore pad); the IntersectRay test is the final filter only. Hit 2026-07-29 (deep profile).
- **Full-frustum pick was still ~300–1300 draws:** floors/walls emit one `DrawMesh` per submesh. World-lateral and screen-AABB culls still fail open or over-include (huge renderer bounds / missing mouse → draw everything). Require `Bounds.IntersectRay(cursorRay)` and **fail closed** when a pick request exists without a mouse ray. Pointer-over-UI must `ClearRequest`. Hit 2026-07-29 (Frame Debugger).
- **Outline bleed into item icons:** `Item.GenerateIcon` clones the live item (including hover shells). Strip with `DestroyImmediate` before bake — deferred `Destroy` leaves enabled outline meshes in the same-frame preview pass. Hit 2026-07-26.
- **`SelectionCamera` must not `Get` Selection in Start.** It lives on `PlayerCamera` in Game, which loads before `NetworkSystemsHub` Online. Use `TryGet` and resolve lazily in the pick readback.
- **`ClosestPoint` spam on hover:** ray-miss fallback must not call `Collider.ClosestPoint` on non-convex `MeshCollider` (or TerrainCollider). Unity warns every `LateUpdate`. Use `ClosestPointOnBounds` for unsupported shapes (`SelectionTargetUtility.GetClosestPoint`; same rule in [examine](examine.md) `ExamineRangeUtility`).
- **Pickable without collider breaks range:** shader ID pick does not need colliders; interaction-point resolution does. Wall mounts missing colliders left `Point` at default zero and (historically) made `RangeCheck` a no-op — see [interactions-framework](interactions-framework.md) smells #3–4. Light switch / air alarm now carry `BoxCollider`s; keep that requirement for new wall mounts.

## Depends on / Used by

- **Depends on:** [rendering](rendering.md) (URP pick pass), [inputs](inputs.md) (pointer-over-UI clears hover)
- **Used by:** [interactions-runtime](interactions-runtime.md), [examine](examine.md), [machine-interface](machine-interface.md)

## Related docs

- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
- Effort: [2026-07_srp-batcher-gpu-instancing.md](../2026-07_srp-batcher-gpu-instancing.md) (pick without MeshRenderer MPBs)
- Effort: [2026-07_unity-framedebug-ai-tooling.md](../2026-07_unity-framedebug-ai-tooling.md) — export pick-pass draw spam for agents
