using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector using <see cref="AdvancedConnector"/> for resolving shape and direction.
    /// Connection rules match <see cref="SimpleConnectionRule"/> (shared generic/specific type).
    /// </summary>
    public class AdvancedAdjacencyConnector : EngineDrivenHorizontalConnector
    {
        [SerializeField] private AdvancedConnector advancedAdjacency;

        protected override IMeshAndDirectionResolver AdjacencyResolver => advancedAdjacency;

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
