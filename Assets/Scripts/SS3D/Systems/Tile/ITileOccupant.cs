using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Anything occupying a tile cell on the map grid.
    /// </summary>
    public interface ITileOccupant
    {
        Vector2Int WorldOrigin { get; }
        TileLayer Layer { get; }
        Direction Direction { get; }
        int MapId { get; }
    }
}
