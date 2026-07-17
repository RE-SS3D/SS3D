> Code paths: Assets/Scripts/SS3D/Rendering/, Assets/Content/Resources/Simple Toon/
> Entry points: SelectionPickRendererFeature, AtmosRendererFeature
> Status: partial

# Rendering

## Overview

URP rendering extensions for this fork. The selection pick pass ([selection](selection.md)) is gameplay-critical for interaction targeting. The atmospherics pass ([atmospherics](atmospherics.md)) composites gas scatter, plasma glow, and heat distortion from sim GPU textures via `AtmosRenderContext`. **Atmos snapshot is server-built only** — clients render when a snapshot is present; multiplayer client sync is not implemented yet.

Station materials use the **Simple Toon** shader stack (`STDefault` / `STTransparent`). Palette emission must sample `_EmissionMap` (same UV swatch pattern as albedo) — a flat `_EmissionColor` alone washes shared `PaletteEmission` materials white.

## Start here

- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickRendererFeature.cs` — URP feature for shader-ID picking
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickContext.cs` — pick pass render context
- `Assets/Scripts/SS3D/Rendering/URP/SelectionRenderingLayers.cs` — layer bit to exclude outline shells from the pick pass
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRendererFeature.cs` — gas scatter, glow, distortion passes
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRenderContext.cs` — shared GPU snapshot for atmos shaders
- `Assets/Content/Resources/Simple Toon/Shaders/STLighting.hlsl` — half-toon lighting + palette emission sample
- `Assets/Settings/URP/` — pipeline asset and Forward+ renderer

## Extension points

- New render features: add URP `ScriptableRendererFeature` under `Rendering/URP/`.
- Outline / auxiliary meshes that must not participate in pick: set rendering layer `SelectionRenderingLayers.ExcludeFromSelectionPick`.

## Depends on / Used by

- **Used by:** [selection](selection.md), [atmospherics](atmospherics.md), [screen-effects](screen-effects.md) (Volume stack; not a custom feature)

## Related docs

- [FORK_STATUS.md](../../FORK_STATUS.md) § URP migration
- Plan: [urp_lighting_look_plan_d42c32f5.plan.md](../../plans/urp_lighting_look_plan_d42c32f5.plan.md)
- Effort (planned): [2026-07_atmos-client-visualization-sync.md](../2026-07_atmos-client-visualization-sync.md)
