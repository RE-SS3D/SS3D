using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Logging;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Basic connector using the simple connector struct for resolving shape and direction.
    /// </summary>
    public class SimpleAdjacencyConnector : AbstractHorizontalConnector, IAdjacencyConnector
    {
        [SerializeField] private SimpleConnector simpleAdjacency;
        protected override IMeshAndDirectionResolver AdjacencyResolver => simpleAdjacency;

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            bool isConnected = false;
            if (neighbourObject != null)
            {
                isConnected = (neighbourObject && neighbourObject.HasAdjacencyConnector);
                isConnected &= neighbourObject.GenericType == _genericType || _genericType == TileObjectGenericType.None;
                isConnected &= neighbourObject.SpecificType == _specificType || _specificType == TileObjectSpecificType.None;
            }
            return isConnected;
        }
    }
}