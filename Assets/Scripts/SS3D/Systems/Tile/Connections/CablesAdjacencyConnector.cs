using System.Collections.Generic;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Underfloor HV cables link grid backbone devices (generators, SMES, APC) and only display visual stubs to other cables.
    /// </summary>
    public class CablesAdjacencyConnector : EngineDrivenHorizontalConnector
    {
        [SerializeField]
        private SimpleConnector _adjacencyResolver;

        protected override IMeshAndDirectionResolver AdjacencyResolver => _adjacencyResolver;

        public override IConnectionRule ConnectionRule => CableConnectionRule.Instance;

        public override List<PlacedTileObject> GetNeighbours()
        {
            Setup();
            return ElectricNeighbourLookup.GetNeighbours(PlacedObject);
        }
    }
}
