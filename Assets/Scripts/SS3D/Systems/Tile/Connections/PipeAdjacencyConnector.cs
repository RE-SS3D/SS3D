using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connector for pipes with a possible offset, such as atmos pipes.
    /// </summary>
    public class PipeAdjacencyConnector : EngineDrivenHorizontalConnector
    {
        [SerializeField] private OffsetConnector _connector;

        protected override IMeshAndDirectionResolver AdjacencyResolver => _connector;

        public override IConnectionRule ConnectionRule
        {
            get
            {
                Setup();
                return new PipeConnectionRule(PlacedObject.GenericType, PlacedObject.SpecificType);
            }
        }
    }
}
