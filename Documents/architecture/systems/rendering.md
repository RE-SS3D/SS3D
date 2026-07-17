> Code paths: Assets/Scripts/SS3D/Rendering/, Assets/Content/Resources/Simple Toon/, Assets/Scripts/SS3D/Systems/Vision/, Assets/Content/Resources/Vision/
> Entry points: SelectionPickRendererFeature, AtmosRendererFeature, VisionRendererFeature
> Status: partial

# Rendering

## Overview

URP rendering extensions for this fork. The selection pick pass ([selection](selection.md)) is gameplay-critical for interaction targeting. The atmospherics pass ([atmospherics](atmospherics.md)) composites gas scatter, plasma glow, and heat distortion from sim GPU textures via `AtmosRenderContext`. **Atmos snapshot is server-built only** — clients render when a snapshot is present; multiplayer client sync is not implemented yet.

Station materials use the **Simple Toon** shader stack (`STDefault` / `STTransparent`). Palette emission must sample `_EmissionMap` (same UV swatch pattern as albedo) — a flat `_EmissionColor` alone washes shared `PaletteEmission` materials white.

Client FOV / fog-of-war is a hard black mask driven by physics raycasts from `Entity.ViewPoint` (`VisionSubSystem` → `_VisionMap`) and composited by `VisionRendererFeature`. Unseen areas are fully opaque black, not soft fog. Each ray iteratively skips furniture/props until the nearest wall/door (non-window); a capped multi-hit batch previously filled with props and leaked vision through walls. Triggers and inventory preview cameras are ignored.

## Start here

- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickRendererFeature.cs` — URP feature for shader-ID picking
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickContext.cs` — pick pass render context
- `Assets/Scripts/SS3D/Rendering/URP/SelectionRenderingLayers.cs` — layer bit to exclude outline shells from the pick pass
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRendererFeature.cs` — gas scatter, glow, distortion passes
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRenderContext.cs` — shared GPU snapshot for atmos shaders
- `Assets/Scripts/SS3D/Rendering/URP/VisionRendererFeature.cs` — FOV mask + hard black composite
- `Assets/Scripts/SS3D/Systems/Vision/VisionSubSystem.cs` — client `RaycastCommand` batch → `_VisionMap`
- `Assets/Content/Resources/Simple Toon/Shaders/STLighting.hlsl` — half-toon lighting + palette emission sample
- `Assets/Settings/URP/` — pipeline asset and Forward+ renderer

## Extension points

- New render features: add URP `ScriptableRendererFeature` under `Rendering/URP/`.
- Outline / auxiliary meshes that must not participate in pick: set rendering layer `SelectionRenderingLayers.ExcludeFromSelectionPick`.

## Depends on / Used by

- **Used by:** [selection](selection.md), [atmospherics](atmospherics.md), [screen-effects](screen-effects.md) (Volume stack; not a custom feature)
- **Vision FOV depends on:** `PlacedTileObject` Wall/Door (or `Walls` layer) colliders; cast origin from [entities](entities.md) `Entity.ViewPoint` when present

## Related docs

- [FORK_STATUS.md](../../FORK_STATUS.md) § URP migration
- Plan: [urp_lighting_look_plan_d42c32f5.plan.md](../../plans/urp_lighting_look_plan_d42c32f5.plan.md)
- Effort (planned): [2026-07_atmos-client-visualization-sync.md](../2026-07_atmos-client-visualization-sync.md)
