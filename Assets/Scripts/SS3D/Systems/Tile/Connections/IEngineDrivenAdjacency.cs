using SS3D.Systems.Tile.Connections.AdjacencyTypes;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connectors using <see cref="AdjacencyEngine"/> for server updates and <see cref="TileAdjacencyView"/> for sync.
    /// </summary>
    public interface IEngineDrivenAdjacency
    {
        IConnectionRule ConnectionRule { get; }

        TileAdjacencyView AdjacencyView { get; }

        IMeshAndDirectionResolver MeshResolver { get; }
    }
}
