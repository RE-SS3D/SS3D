namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Unified network payload for tile adjacency visual state.
    /// Horizontal connectors use <see cref="HorizontalConnections"/> (8-bit adjacency map).
    /// </summary>
    public struct AdjacencyPayload
    {
        public byte HorizontalConnections;

        public static AdjacencyPayload FromByte(byte value) => new() { HorizontalConnections = value };

        public byte ToByte() => HorizontalConnections;
    }
}
