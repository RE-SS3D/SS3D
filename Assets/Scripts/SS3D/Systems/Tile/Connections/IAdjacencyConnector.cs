namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Interface that all adjacency connectors should use.
    /// </summary>
    public interface IAdjacencyConnector
    {
        void UpdateSingle(Direction direction, PlacedTileObject placedObject);
        void UpdateAll(PlacedTileObject[] neighbourObjects);
        void CleanAdjacencies();
    }
}