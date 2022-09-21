using System.Collections;
using System.Collections.Generic;
using SS3D.Engine.Tile.TileRework;
using UnityEngine;

namespace SS3D.Engine.Tiles.Connections
{
    /// <summary>
    /// Interface that all adjacency connectors should use.
    /// </summary>
    public interface IAdjacencyConnector
    {
        void UpdateSingle(Direction direction, PlacedTileObject placedObject);
        void UpdateAll(PlacedTileObject[] neighbourObjects);
        void CleanAdjacencies();
    }
}