using SS3D.Systems.Area;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Read-only tilemap queries without side effects such as creating empty chunks.
    /// </summary>
    public interface ITileQueryService
    {
        bool TryGetOccupant(TileCoord coord, TileLayer layer, Direction dir, out ITileOccupant occupant);

        bool TryGetOccupancy(TileCoord coord, out TileOccupancy occupancy);

        IReadOnlyList<ITileOccupant> GetNeighbours(TileCoord coord, TileLayer layer);

        bool TryFindFreeTile(TileCoord near, TileLayer layer, out TileCoord free);

        TileCoord WorldToTile(Vector3 world, int mapId = 0);

        Vector3 TileToWorld(TileCoord coord);

        bool TryGetAreaId(TileCoord coord, out AreaId areaId);
    }
}
