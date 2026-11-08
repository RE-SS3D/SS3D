using System;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// A tile object that <see cref="TileMap.ClearTileObject(Vector3, TileLayer, Direction)"/> removed,
    /// with enough to place it back. Collected through <see cref="TileMap.RecordRemovals"/>.
    /// </summary>
    public readonly struct RemovedTileRecord : IEquatable<RemovedTileRecord>
    {
        public RemovedTileRecord(TileObjectSo tile, Vector3 origin, Direction direction)
        {
            Tile = tile;
            Origin = origin;
            Direction = direction;
        }

        public TileObjectSo Tile { get; }

        /// <summary>Grid origin the object was placed at (its footprint starts here).</summary>
        public Vector3 Origin { get; }

        public Direction Direction { get; }

        public bool Equals(RemovedTileRecord other) =>
            Tile == other.Tile && Origin == other.Origin && Direction == other.Direction;

        public override bool Equals(object obj) => obj is RemovedTileRecord other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Tile, Origin, Direction);
    }
}
