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
    /// Good for stuff that connects on the same layer only, horizontally, of the same specific and generic type.
    /// </summary>
    public class SimpleAdjacencyConnector : AbstractHorizontalConnector, IAdjacencyConnector
    {
        [SerializeField] private SimpleConnector simpleAdjacency;
        protected override IMeshAndDirectionResolver AdjacencyResolver => simpleAdjacency;

        public override bool IsConnected(Direction dir, PlacedTileObject neighbourObject)
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