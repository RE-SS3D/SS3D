using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Resolves which tile should be used for area lookup on wall-mounted devices.
    /// </summary>
    public static class AreaDeviceTileResolver
    {
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
    }
}
