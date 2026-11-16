namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Engine-driven connectors that need cross-layer or multi-field adjacency recompute beyond a single horizontal byte.
    /// </summary>
    public interface ICustomAdjacencyRecompute : IEngineDrivenAdjacency
    {
        void RecomputeAdjacency(TileMap map);
    }
}
