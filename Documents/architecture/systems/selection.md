> Code paths: Assets/Scripts/SS3D/Systems/Selection/, Assets/Scripts/SS3D/Rendering/URP/
> Entry points: SelectionSubSystem, SelectionController, SelectionPickRendererFeature
> Status: shipped

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
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickRendererFeature.cs` — URP render feature for ID pass
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickContext.cs` — render context for pick pass
- `Assets/Scripts/SS3D/Rendering/URP/SelectionRenderingLayers.cs` — pick-pass exclude bit (lives in Rendering.URP to avoid assembly cycles)

## Extension points

- Add `Selectable` to mesh renderers on new interactable objects.
- Implement `IExaminable` on selectables for [examine](examine.md) integration.
- Auxiliary meshes (outlines, FX): set `SelectionRenderingLayers.ExcludeFromSelectionPick` on their rendering layer mask.
- `MachineInterfaceHost` disables `UIDocument` when closed to avoid interfering with the pick pass — follow this pattern for overlay UI.

## Depends on / Used by

- **Depends on:** [rendering](rendering.md) (URP pick pass), [inputs](inputs.md) (pointer-over-UI clears hover)
- **Used by:** [interactions-runtime](interactions-runtime.md), [examine](examine.md), [machine-interface](machine-interface.md)

## Related docs

- Design (read-only): [Documents/design/main-hud.md](../../design/main-hud.md)
