> Code paths: Assets/Scripts/SS3D/Rendering/
> Entry points: SelectionPickRendererFeature
> Status: partial

# Rendering

## Overview

URP rendering extensions for this fork. The selection pick pass ([selection](selection.md)) is the primary gameplay-critical feature. Also includes edge detection and post-effect utilities.

## Start here

- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickRendererFeature.cs` — URP feature for shader-ID picking
- `Assets/Scripts/SS3D/Rendering/URP/SelectionPickContext.cs` — pick pass render context
- `Assets/Settings/URP/` — pipeline asset and Forward+ renderer

## Extension points

- New render features: add URP `ScriptableRendererFeature` under `Rendering/URP/`.

## Depends on / Used by

- **Used by:** [selection](selection.md)

## Related docs

- [FORK_STATUS.md](../../FORK_STATUS.md) § URP migration
