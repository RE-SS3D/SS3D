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
    /// <summary>
    /// Basic connector using the Advanced connector struct for resolving shape and direction.
    /// </summary>
    public class AdvancedAdjacencyConnector : AbstractHorizontalConnector
    {
        [FormerlySerializedAs("advancedAdjacency")]
        [SerializeField]
        private AdvancedConnector _advancedAdjacency;

        protected override IMeshAndDirectionResolver AdjacencyResolver => _advancedAdjacency;

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            if (!neighbourObject)
            {
                return false;
            }

            bool isConnected = neighbourObject && neighbourObject.HasAdjacencyConnector;
            isConnected &= neighbourObject.GenericType == GenericType || GenericType == TileObjectGenericType.None;
            isConnected &= neighbourObject.SpecificType == SpecificType || SpecificType == TileObjectSpecificType.None;
            return isConnected;
        }
    }
}
