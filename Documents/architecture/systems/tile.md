> Code paths: Assets/Scripts/SS3D/Systems/Tile/
> Entry points: TileSubSystem, AdjacencyEngine, ConstructionService, TileQueryService
> Status: shipped

# Tile / construction

## Overview

Server-authoritative tilemap with adjacency-driven mesh visuals, construction placement, and FishNet HashGrid AOI replication. The adjacency engine queues recompute for walls, doors, pipes, cables, disposal, and furniture connectors. Tile identity sync uses a compact ushort asset catalog. Station template save/load delegates to [persistence](persistence.md) (`PersistenceSubSystem`) with legacy flat-JSON fallback. The TileMap Creator build menu includes client-only layer-group visibility controls for admin map editing.

## Start here

- `Assets/Scripts/SS3D/Systems/Tile/TileSubSystem.cs` — subsystem entry point
- `Assets/Scripts/SS3D/Systems/Tile/TileMap.cs` — tilemap data and mutation
- `Assets/Scripts/SS3D/Systems/Tile/Connections/AdjacencyEngine.cs` — queued adjacency recompute
- `Assets/Scripts/SS3D/Systems/Tile/Connections/TileAdjacencyView.cs` — local mesh/direction visuals
- `Assets/Scripts/SS3D/Systems/Tile/ConstructionService.cs` — server-authoritative placement
- `Assets/Scripts/SS3D/Systems/Tile/TileQueryService.cs` — read-only tile queries (`ITileQueryService`)
- `Assets/Scripts/SS3D/Systems/Tile/ITileMutationObserver.cs` — hook for systems reacting to tile changes
- `Assets/Scripts/SS3D/Systems/Tile/IDynamicTileOccupant.cs` — runtime open/closed state (doors) for occupancy recompute
- `Assets/Scripts/SS3D/Systems/Tile/TileOccupancyEvaluator.cs` — derives passability/vision flags from placed occupants
- `Assets/Scripts/SS3D/Systems/Tile/TileChunk.cs` — per-tile area-id array (`ushort[]`)
- `Assets/Scripts/SS3D/Systems/Tile/TileAssetCatalog.cs` — compact tile identity catalog
- `Assets/Scripts/SS3D/Systems/Tile/TileMapCreator/TileMapMenuSubSystem.cs` — in-game map editor menu (admin-gated)
- `Assets/Scripts/SS3D/Systems/Tile/TileMapCreator/TileMapBuildTab.cs` — build tab UI; activates layer visibility on open
- `Assets/Scripts/SS3D/Systems/Tile/TileMapCreator/TileLayerVisibilityService.cs` — client-only layer-group dim/restore (~5% opacity)
- `Assets/Scripts/SS3D/Systems/Tile/TileMapCreator/TileLayerCategory.cs` — shared build-menu category → `TileLayer` mapping

## Extension points

- New tile objects: create `TileObjectSo` assets and adjacency connectors implementing `IAdjacencyConnector`.
- React to placement: implement `ITileMutationObserver` (see [electricity](electricity.md), [area](area.md), [atmospherics](atmospherics.md)).
- Dynamic passability: implement `IDynamicTileOccupant` and call `TileSubSystem.NotifyTileStateChanged` when state changes (see [furniture](furniture.md) airlocks).
- HV cables (`CablesAdjacencyConnector`): underfloor Wire-layer runs link grid backbone devices only; see [electricity](electricity.md) `ElectricCableConnectivity`.
- TileMap Creator: `TileMapMenuSubSystem` (admin-gated RPCs via `TileMapEditorPermissions`). Build tab layer visibility is **client-only** — `TileLayerVisibilityService` dims non-selected layer groups locally via material swap; no RPCs or SyncVars. UI: multi-select dropdown cloned from the asset-category dropdown (`TileLayerVisibilityPanel`); categories shared with `AssetGrid` through `TileLayerCategoryMapping`. State resets when the menu closes. New spawns re-apply via `PlacedTileObject` / `PlacedItemObject` client hooks after AOI sync.
- Station templates: `TileSubSystem.Save` / `Load` / `Load(string)` → `PersistenceSubSystem` (`StationTemplates/`, legacy `Tilemaps/`); server boot also calls `LoadServerMeta`.

## Depends on / Used by

- **Depends on:** [networking-session](networking-session.md) (FishNet AOI), [permissions](permissions.md) (creator admin checks), [persistence](persistence.md) (station template I/O)
- **Used by:** [electricity](electricity.md), [area](area.md), [atmospherics](atmospherics.md), [furniture](furniture.md), [substances](substances.md), [persistence](persistence.md) (tilemap contributor)

## Related docs

- Design (read-only): [Documents/design/area.md](../../design/area.md)
- System map: [area](area.md)
- Plan: [persistence_architecture_design_2fe61864.plan.md](../../plans/persistence_architecture_design_2fe61864.plan.md)
