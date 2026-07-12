> Code paths: Assets/Scripts/SS3D/Rendering/
> Entry points: SelectionPickRendererFeature, AtmosRendererFeature
> Status: partial

# Rendering

## Overview

URP rendering extensions for this fork. The selection pick pass ([selection](selection.md)) is gameplay-critical for interaction targeting. The atmospherics pass ([atmospherics](atmospherics.md)) composites gas scatter, plasma glow, and heat distortion from sim GPU textures. Also includes edge detection and post-effect utilities.

## Start here

- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickRendererFeature.cs` — URP feature for shader-ID picking
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickContext.cs` — pick pass render context
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRendererFeature.cs` — gas scatter, glow, distortion passes
- `Assets/Scripts/SS3D/Rendering/URP/AtmosRenderContext.cs` — shared GPU snapshot for atmos shaders
- `Assets/Settings/URP/` — pipeline asset and Forward+ renderer

## Extension points

- New render features: add URP `ScriptableRendererFeature` under `Rendering/URP/`.

## Depends on / Used by

- **Used by:** [selection](selection.md), [atmospherics](atmospherics.md)

## Related docs

- [FORK_STATUS.md](../../FORK_STATUS.md) § URP migration
