namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Visual adjacency for atmos pipes — same generic/specific type on the pipe layer.
    /// Shared surface for future atmos pipe network logic (#1464).
    /// </summary>
    public sealed class PipeConnectionRule : IConnectionRule
    {
        private readonly SimpleConnectionRule _inner;

        public PipeConnectionRule(TileObjectGenericType genericType, TileObjectSpecificType specificType)
        {
            _inner = new SimpleConnectionRule(genericType, specificType);
        }

        public bool IsConnected(PlacedTileObject self, PlacedTileObject neighbour) =>
            _inner.IsConnected(self, neighbour);
    }
}
