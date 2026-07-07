namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Cables only display connections to other cables; electric devices share the circuit without a visual stub.
    /// </summary>
    public sealed class CableConnectionRule : IConnectionRule
    {
        public static readonly CableConnectionRule Instance = new();

        public bool IsConnected(PlacedTileObject self, PlacedTileObject neighbour)
        {
            return neighbour != null && neighbour.Connector is CablesAdjacencyConnector;
        }
    }
}
