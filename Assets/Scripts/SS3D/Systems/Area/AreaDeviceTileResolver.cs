using SS3D.Systems.Tile;
using SS3D.Systems.Electricity;
using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Resolves which tile should be used for area lookup on wall-mounted devices.
    /// </summary>
    public static class AreaDeviceTileResolver
    {
        public static TileCoord GetApcRoomTile(IAreaApcOrigin apc, ITileQueryService query)
        {
            // Actual wall mounts always use their facing tile, even while their wall is absent.
            if (apc is IElectricDevice device && device.TileObject != null &&
                device.TileObject.Layer is TileLayer.WallMountHigh or TileLayer.WallMountLow)
                return GetTileInFront(device.TileObject);
            TileCoord origin = apc.OriginTile;
            if (AreaBoundaryEvaluator.IsWalkable(query, origin)) return origin;
            Vector2Int offset = TileHelper.CoordinateDifferenceInFrontFacingDirection(apc.FacingDirection);
            return new TileCoord(origin.MapId, origin.Grid + offset);
        }

        public static TileCoord GetOriginTile(PlacedTileObject tileObject)
        {
            return new TileCoord(tileObject.MapId, tileObject.WorldOrigin);
        }

        public static TileCoord GetTileInFront(PlacedTileObject tileObject)
        {
            TileCoord origin = GetOriginTile(tileObject);
            Vector2Int offset = TileHelper.CoordinateDifferenceInFrontFacingDirection(tileObject.Direction);
            return new TileCoord(origin.MapId, origin.Grid.x + offset.x, origin.Grid.y + offset.y);
        }

        /// <summary>
        /// Resolves area membership from a client floor-cache snapshot using the same tile
        /// selection rules as <see cref="AreaSubSystem.TryGetAreaForDevice"/>.
        /// </summary>
        public static bool TryResolveAreaIdFromFloorCache(
            AreaFloorVisualCache cache,
            PlacedTileObject tileObject,
            out AreaId areaId)
        {
            areaId = default;
            if (cache == null || tileObject == null)
            {
                return false;
            }

            if (tileObject.Layer == TileLayer.WallMountHigh || tileObject.Layer == TileLayer.WallMountLow)
            {
                return TryGetAreaId(cache, GetTileInFront(tileObject).Grid, out areaId);
            }

            if (TryGetAreaId(cache, GetOriginTile(tileObject).Grid, out areaId))
            {
                return true;
            }

            return TryGetAreaId(cache, GetTileInFront(tileObject).Grid, out areaId);
        }

        private static bool TryGetAreaId(AreaFloorVisualCache cache, Vector2Int worldGrid, out AreaId areaId)
        {
            areaId = default;
            if (!cache.TryGetAreaIdForWorldGrid(worldGrid, out ushort raw))
            {
                return false;
            }

            areaId = new AreaId(raw);
            return true;
        }
    }
}
