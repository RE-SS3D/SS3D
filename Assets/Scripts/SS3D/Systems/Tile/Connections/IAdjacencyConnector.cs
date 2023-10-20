namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Interface that all adjacency connectors should use.
    /// </summary>
    public interface IAdjacencyConnector
    {
        bool UpdateSingleConnection(Direction direction, PlacedTileObject neighbourObject, bool updateNeighbour);

        // TODO remove the neighbours object, should leave it to the conenctor to get them.
        void UpdateAllConnections(PlacedTileObject[] neighbourObjects);
        bool IsConnected(Direction dir, PlacedTileObject neighbourObject);
    }
}