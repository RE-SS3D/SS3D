namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Connects neighbours that share generic and specific type (or either is None).
    /// </summary>
    public sealed class SimpleConnectionRule : IConnectionRule
    {
        private readonly TileObjectGenericType _genericType;
        private readonly TileObjectSpecificType _specificType;

        public SimpleConnectionRule(TileObjectGenericType genericType, TileObjectSpecificType specificType)
        {
            _genericType = genericType;
            _specificType = specificType;
        }

        public bool IsConnected(PlacedTileObject self, PlacedTileObject neighbour)
        {
            if (neighbour == null || !neighbour.HasAdjacencyConnector)
                return false;

            bool connected = neighbour.GenericType == _genericType || _genericType == TileObjectGenericType.None;
            connected &= neighbour.SpecificType == _specificType || _specificType == TileObjectSpecificType.None;
            return connected;
        }
    }
}
