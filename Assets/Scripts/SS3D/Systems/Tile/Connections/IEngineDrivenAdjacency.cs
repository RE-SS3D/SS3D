namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connectors using <see cref="AdjacencyEngine"/> for server updates and connector SyncVars for replication.
    /// </summary>
    public interface IEngineDrivenAdjacency
    {
        IConnectionRule ConnectionRule { get; }

        void SetAdjacencyConnections(byte horizontalConnections);
    }
}
