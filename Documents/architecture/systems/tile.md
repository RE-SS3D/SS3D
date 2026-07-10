> Code paths: Assets/Scripts/SS3D/Systems/Tile/
> Entry points: TileSubSystem, AdjacencyEngine, ConstructionService, TileQueryService
> Status: shipped

# Tile / construction

## Overview

Server-authoritative tilemap with adjacency-driven mesh visuals, construction placement, and FishNet HashGrid AOI replication. The adjacency engine queues recompute for walls, doors, pipes, cables, disposal, and furniture connectors. Tile identity sync uses a compact ushort asset catalog.

## Start here

- `Assets/Scripts/SS3D/Systems/Tile/TileSubSystem.cs` — subsystem entry point
- `Assets/Scripts/SS3D/Systems/Tile/TileMap.cs` — tilemap data and mutation
- `Assets/Scripts/SS3D/Systems/Tile/Connections/AdjacencyEngine.cs` — queued adjacency recompute
- `Assets/Scripts/SS3D/Systems/Tile/Connections/TileAdjacencyView.cs` — local mesh/direction visuals
- `Assets/Scripts/SS3D/Systems/Tile/ConstructionService.cs` — server-authoritative placement
- `Assets/Scripts/SS3D/Systems/Tile/TileQueryService.cs` — read-only tile queries (`ITileQueryService`)
- `Assets/Scripts/SS3D/Systems/Tile/ITileMutationObserver.cs` — hook for systems reacting to tile changes
- `Assets/Scripts/SS3D/Systems/Tile/TileChunk.cs` — per-tile area-id array (`ushort[]`)
- `Assets/Scripts/SS3D/Systems/Tile/TileAssetCatalog.cs` — compact tile identity catalog

## Extension points

- New tile objects: create `TileObjectSo` assets and adjacency connectors implementing `IAdjacencyConnector`.
- React to placement: implement `ITileMutationObserver` (see [electricity](electricity.md), [area](area.md)).
- TileMap Creator: `TileMapMenuSubSystem` (admin-gated RPCs).

## Depends on / Used by

- **Depends on:** [networking-session](networking-session.md) (FishNet AOI), [permissions](permissions.md) (creator admin checks)
- **Used by:** [electricity](electricity.md), [area](area.md), [furniture](furniture.md), [substances](substances.md)

## Related docs

- Design (read-only): [Documents/design/area.md](../../design/area.md)
- System map: [area](area.md)
