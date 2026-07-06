using System;

namespace SS3D.Systems.Tile.Connections
{
    /// <summary>
    /// Unified network payload for tile adjacency visual state.
    /// Horizontal connectors use <see cref="HorizontalConnections"/> only.
    /// Disposal pipes pack all fields into a single <see cref="uint"/> SyncVar.
    /// </summary>
    public struct AdjacencyPayload : IEquatable<AdjacencyPayload>
    {
        public byte HorizontalConnections;
        public bool VerticalConnection;
        public Direction Facing;

        public static AdjacencyPayload FromByte(byte value) => new() { HorizontalConnections = value };

        public static AdjacencyPayload ForDisposal(byte connections, bool vertical, Direction facing) => new()
        {
            HorizontalConnections = connections,
            VerticalConnection = vertical,
            Facing = facing,
        };

        public byte ToByte() => HorizontalConnections;

        public uint PackDisposal() =>
            (uint)HorizontalConnections
            | (VerticalConnection ? 1u << 8 : 0)
            | ((uint)Facing << 9);

        public static AdjacencyPayload UnpackDisposal(uint packed) => new()
        {
            HorizontalConnections = (byte)(packed & 0xFF),
            VerticalConnection = (packed & (1u << 8)) != 0,
            Facing = (Direction)((packed >> 9) & 0x7),
        };

        public bool Equals(AdjacencyPayload other) => PackDisposal() == other.PackDisposal();

        public override bool Equals(object obj) => obj is AdjacencyPayload other && Equals(other);

        public override int GetHashCode() => (int)PackDisposal();
    }
}
