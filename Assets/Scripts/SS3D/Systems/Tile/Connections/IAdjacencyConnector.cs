namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Interface that all adjacency connectors should use.
    /// </summary>
    public interface IAdjacencyConnector
    {
        bool UpdateSingle(Direction direction, PlacedTileObject neighbourObject, bool updateNeighbour);
        void UpdateAll(PlacedTileObject[] neighbourObjects);
    }
}