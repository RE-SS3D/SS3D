using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework.Connections
{
    [RequireComponent(typeof(MeshFilter))]
    public class AbstractAdjacencyConnector : MonoBehaviour, IAdjacencyConnector
    {
        

        public void UpdateAll(PlacedTileObject[] neighbourObjects)
        {
            throw new System.NotImplementedException();
        }

        public void UpdateSingle(TileObjectSO.Dir direction, PlacedTileObject placedObject)
        {
            throw new System.NotImplementedException();
        }
    }
}