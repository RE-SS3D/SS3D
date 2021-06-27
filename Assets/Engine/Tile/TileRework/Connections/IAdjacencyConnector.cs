using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework.Connections
{
    public interface IAdjacencyConnector
    {
        void UpdateSingle(TileObjectSO.Dir direction, PlacedTileObject placedObject);
        void UpdateAll(PlacedTileObject[] neighbourObjects);
    }
}