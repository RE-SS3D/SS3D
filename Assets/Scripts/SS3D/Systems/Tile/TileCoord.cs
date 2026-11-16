using System;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Identifies a tile on a specific map using world grid coordinates.
    /// </summary>
    public struct TileCoord : IEquatable<TileCoord>
    {
        public int MapId;
        public Vector2Int Grid;

        public TileCoord(int mapId, Vector2Int grid)
        {
            MapId = mapId;
            Grid = grid;
        }

        public TileCoord(int mapId, int x, int y) : this(mapId, new Vector2Int(x, y)) { }

        public bool Equals(TileCoord other) =>
            MapId == other.MapId && Grid.x == other.Grid.x && Grid.y == other.Grid.y;

        public override bool Equals(object obj) => obj is TileCoord other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = MapId;
                hash = (hash * 397) ^ Grid.x;
                hash = (hash * 397) ^ Grid.y;
                return hash;
            }
        }

        public override string ToString() => $"({MapId}:{Grid.x},{Grid.y})";

        public static bool operator ==(TileCoord left, TileCoord right) => left.Equals(right);

        public static bool operator !=(TileCoord left, TileCoord right) => !left.Equals(right);
    }
}
