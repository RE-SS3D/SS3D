> Implements: Documents/design/area.md §2–3 (area membership as source of truth for department belonging)
> Touches systems: tile, area
> Status: shipped

# Tile overlay replacement

## Goal

Replace `TileLayer.Overlays` prefab-per-tile department corners with:

1. Area-driven floor stripes from `AreaRecord.DepartmentalLightTint`
2. Sparse `ushort[] floorDecalIds` on chunks for non-area stickers
3. Removal of the Overlays enum slot (pipe layers renumbered)

## Shipped surface

- `AreaFloorStripeView` + `AreaFloorVisualCache` + ObserversRpc tint/area-id sync from `AreaSubSystem`
- `FloorDecalDefinition` / `FloorDecalCatalog` / `FloorDecalView` + TileMap Creator Floor Decals brush
- `TileChunk.floorDecalIds` persisted on `SavedTileChunk`
- Unknown/removed tile assets skipped on map load

## Notes

- Department stripes reuse light tint (no separate AreaRecord floor color field).
- Floor visuals are client mesh quads (not NetworkObjects / PlacedTileObject).
- Placeholder catalog entry is created at runtime when no Resources definitions exist.
