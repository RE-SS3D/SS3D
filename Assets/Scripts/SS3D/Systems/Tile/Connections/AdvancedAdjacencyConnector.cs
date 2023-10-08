using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Logging;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.Connections
{
    public class AdvancedAdjacencyConnector : AbstractHorizontalConnector, IAdjacencyConnector
    {
        [SerializeField] private AdvancedConnector advancedAdjacency;
        protected override IMeshAndDirectionResolver AdjacencyResolver => advancedAdjacency;

        public override bool IsConnected(Direction dir, PlacedTileObject neighbourObject)
        {
            bool isConnected = false;
            if (neighbourObject)
            {
                isConnected = (neighbourObject && neighbourObject.HasAdjacencyConnector);
                isConnected &= neighbourObject.GenericType == _genericType || _genericType == TileObjectGenericType.None;
                isConnected &= neighbourObject.SpecificType == _specificType || _specificType == TileObjectSpecificType.None;
            }
            return isConnected;
        }
    }
}
