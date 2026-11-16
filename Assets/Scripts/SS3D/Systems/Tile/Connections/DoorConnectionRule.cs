namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Doors connect to adjacent walls for wall-cap placement.
    /// </summary>
    public sealed class DoorConnectionRule : IConnectionRule
    {
        public bool IsConnected(PlacedTileObject self, PlacedTileObject neighbour)
        {
            return neighbour != null
                && neighbour.HasAdjacencyConnector
                && neighbour.GenericType == TileObjectGenericType.Wall;
        }
    }
}
