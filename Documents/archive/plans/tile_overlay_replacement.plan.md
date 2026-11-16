---
name: Tile Overlay Replacement
overview: Replace TileLayer.Overlays with Area floor stripes and sparse floor decals.
todos:
  - id: remove-overlays-layer
    content: Remove TileLayer.Overlays; renumber pipes; delete Corner* content
    status: completed
  - id: area-floor-stripes
    content: AreaFloorStripeView + client sync of tints and areaIds
    status: completed
  - id: sparse-floor-decals
    content: floorDecalIds + catalog + creator brush + FloorDecalView
    status: completed
  - id: tests-docs
    content: Edit-mode tests and architecture docs
    status: completed
isProject: false
---

# Tile overlay replacement (shipped)

See [Documents/architecture/2026-07_tile-overlay-replacement.md](../architecture/2026-07_tile-overlay-replacement.md).

## Implementation notes

- Branch was rebased by merging `develop` first (Area/tile visibility prerequisites).
- Overlay materials retained under `Assets/Content/WorldObjects/Structures/Floors/TileOverlays/`; textures under Art; stripe corner texture also copied to `Assets/Resources/FloorVisuals/StripeCorner.png`.
- Floor Decals build-menu category maps to empty `TileLayer[]` and loads `FloorDecalCatalog` definitions instead of tile objects.
