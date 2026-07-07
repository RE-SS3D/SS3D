using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Identifies a tile on a specific map using world grid coordinates.
    /// </summary>
    public struct TileCoord
    {
        public int MapId;
        public Vector2Int Grid;

        public TileCoord(int mapId, Vector2Int grid)
        {
            MapId = mapId;
            Grid = grid;
        }

        public TileCoord(int mapId, int x, int y) : this(mapId, new Vector2Int(x, y)) { }
    }
}
