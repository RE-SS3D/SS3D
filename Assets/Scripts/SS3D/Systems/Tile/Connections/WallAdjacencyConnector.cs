using SS3D.Core;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Walls are mostly simply working like advanced connectors, but there is a few exceptions,
    /// in particular with the way they connect with doors, hence why they need their own connector
    /// script.
    /// </summary>
    public class WallAdjacencyConnector : EngineDrivenHorizontalConnector
    {
        [SerializeField] private AdvancedConnector _advancedAdjacency;

        protected override IMeshAndDirectionResolver AdjacencyResolver => _advancedAdjacency;

        public override IConnectionRule ConnectionRule
        {
            get
            {
                TileMap map = SubSystems.Get<TileSubSystem>()?.CurrentMap;
                return new WallConnectionRule(map);
            }
        }
    }
}
