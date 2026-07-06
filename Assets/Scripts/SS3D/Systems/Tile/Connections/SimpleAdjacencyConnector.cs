using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Basic connector using the simple connector struct for resolving shape and direction.
    /// Things do not need special connections in corners.
    /// The only condition to connect to a neighbour is that they share generic and specific type.
    /// </summary>
    public class SimpleAdjacencyConnector : EngineDrivenHorizontalConnector
    {
        [SerializeField] private SimpleConnector simpleAdjacency;

        protected override IMeshAndDirectionResolver AdjacencyResolver => simpleAdjacency;

        public override IConnectionRule ConnectionRule
        {
            get
            {
                Setup();
                return new SimpleConnectionRule(PlacedObject.GenericType, PlacedObject.SpecificType);
            }
        }
    }
}
