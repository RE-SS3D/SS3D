using SS3D.Systems.Tile;

namespace SS3D.Systems.Area
{
    public static class AreaBoundaryEvaluator
    {
        public static bool BlocksAreaExpansion(
            TileCoord from,
            TileCoord to,
            ITileQueryService query,
            AreaId expandingAreaId,
            ushort existingTargetAreaId)
        {
            if (from.MapId != to.MapId)
                return true;

            if (IsTurfWall(query, from) || IsTurfWall(query, to))
                return true;

            if (IsTurfDoor(query, from) || IsTurfDoor(query, to))
                return true;

            if (existingTargetAreaId != AreaId.None && existingTargetAreaId != expandingAreaId.Value)
                return true;

            return !IsWalkable(query, to);
        }

        public static bool IsWalkable(ITileQueryService query, TileCoord coord)
        {
            if (!query.TryGetOccupancy(coord, out TileOccupancy occupancy) || !occupancy.HasPlenum)
                return false;

            return !IsTurfWall(query, coord);
        }

        public static bool IsTurfDoor(ITileQueryService query, TileCoord coord)
        {
            return TryGetTurfGenericType(query, coord, out TileObjectGenericType genericType)
                && genericType == TileObjectGenericType.Door;
        }

        public static bool IsTurfWall(ITileQueryService query, TileCoord coord)
        {
            return TryGetTurfGenericType(query, coord, out TileObjectGenericType genericType)
                && genericType == TileObjectGenericType.Wall;
        }

        private static bool TryGetTurfGenericType(
            ITileQueryService query,
            TileCoord coord,
            out TileObjectGenericType genericType)
        {
            genericType = TileObjectGenericType.None;

            if (!query.TryGetOccupant(coord, TileLayer.Turf, Direction.North, out ITileOccupant occupant))
                return false;

            if (occupant is not PlacedTileObject placed)
                return false;

            genericType = placed.GenericType;
            return true;
        }
    }
}
