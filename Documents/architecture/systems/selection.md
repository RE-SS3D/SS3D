> Code paths: Assets/Scripts/SS3D/Systems/Selection/, Assets/Scripts/SS3D/Rendering/URP/
> Entry points: SelectionSubSystem, SelectionController, SelectionPickRendererFeature
> Status: shipped
> Verified: a20853b1c — 2026-07-18

# Selection

## Overview

Shader-ID mesh picking replaces screen raycasts for interaction targeting. Each `Selectable` gets a unique render color; a URP offscreen pick pass plus `SelectionCamera` readback identifies the hover target. `InteractionController` routes client interaction targeting through this system and drives `InteractionOutlineView` from the current hover; the server validates using `NetworkObject` and interaction point.

Outline shells and other auxiliary meshes use `SelectionRenderingLayers.ExcludeFromSelectionPick` so they stay out of the ID pass (avoids hover flicker / z-fight and outline bleed into item icons). Clear outlines on inventory pickup so the green shell does not stick after Take.

`SelectionCamera` clears the current selectable while `InputInterface.IsPointerOverInterface()` is true, so examine/hover/outlines do not target world objects through registered UI Toolkit panels (Main HUD, machine UI, radial).

## Start here

- `Assets/Scripts/SS3D/Systems/Selection/SelectionSubSystem.cs` — subsystem entry point
- `Assets/Scripts/SS3D/Systems/Selection/SelectionController.cs` — per-frame hover/update logic
- `Assets/Scripts/SS3D/Systems/Selection/Selectable.cs` — component marking pickable meshes
- `Assets/Scripts/SS3D/Systems/Selection/SelectionCamera.cs` — pick buffer readback
- `Assets/Scripts/SS3D/Systems/Selection/SelectionTargetUtility.cs` — ray / closest-point interaction point for range checks
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickRendererFeature.cs` — URP render feature for ID pass
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickContext.cs` — render context for pick pass
- `Assets/Scripts/SS3D/Rendering/URP/SelectionRenderingLayers.cs` — pick-pass exclude bit (lives in Rendering.URP to avoid assembly cycles)

## Extension points

- Add `Selectable` **and** a collider (usually `BoxCollider` on wall mounts) to mesh roots that should be pickable *and* range-checked. Pick works from materials alone; [interactions-framework](interactions-framework.md) `RangeCheck` / drop normals need a resolved point from colliders (or transform fallback).
- Implement `IExaminable` on selectables for [examine](examine.md) integration.
- Auxiliary meshes (outlines, FX): set `SelectionRenderingLayers.ExcludeFromSelectionPick` on their rendering layer mask.
- `MachineInterfaceHost` disables `UIDocument` when closed to avoid interfering with the pick pass — follow this pattern for overlay UI.

## Pitfalls

- **`ClosestPoint` spam on hover:** ray-miss fallback must not call `Collider.ClosestPoint` on non-convex `MeshCollider` (or TerrainCollider). Unity warns every `LateUpdate`. Use `ClosestPointOnBounds` for unsupported shapes (`SelectionTargetUtility.GetClosestPoint`; same rule in [examine](examine.md) `ExamineRangeUtility`).
- **Pickable without collider breaks range:** shader ID pick does not need colliders; interaction-point resolution does. Wall mounts missing colliders left `Point` at default zero and (historically) made `RangeCheck` a no-op — see [interactions-framework](interactions-framework.md) smells #3–4. Light switch / air alarm now carry `BoxCollider`s; keep that requirement for new wall mounts.

## Depends on / Used by

- **Depends on:** [rendering](rendering.md) (URP pick pass), [inputs](inputs.md) (pointer-over-UI clears hover)
- **Used by:** [interactions-runtime](interactions-runtime.md), [examine](examine.md), [machine-interface](machine-interface.md)

## Related docs

- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
