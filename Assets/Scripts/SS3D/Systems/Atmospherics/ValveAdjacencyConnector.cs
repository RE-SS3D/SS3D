using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    public class ValveAdjacencyConnector : AbstractHorizontalConnector
    {
        protected override IMeshAndDirectionResolver AdjacencyResolver { get; }

        public override bool IsConnected(PlacedTileObject neighbourObject)
        {
            bool isConnected = false;

            if (neighbourObject != null)
            {
                isConnected = neighbourObject && neighbourObject.HasAdjacencyConnector;
                isConnected &= neighbourObject.GenericType == TileObjectGenericType.Pipe;
                isConnected &= neighbourObject.WorldOrigin == GetComponent<PlacedTileObject>().WorldOrigin + new Vector2Int((int)gameObject.transform.forward.x, (int)gameObject.transform.forward.z) || neighbourObject.WorldOrigin == GetComponent<PlacedTileObject>().WorldOrigin - new Vector2Int((int)gameObject.transform.forward.x, (int)gameObject.transform.forward.z);
            }

            return isConnected;
        }
    }
}
