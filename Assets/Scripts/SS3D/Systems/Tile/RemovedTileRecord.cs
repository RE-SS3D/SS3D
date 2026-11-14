using SS3D.Data.Persistence;
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
        public RemovedTileRecord(TileObjectSo tile, Vector3 origin, Direction direction, SavedSetupEntry[] setup = null)
        {
            Tile = tile;
            Origin = origin;
            Direction = direction;
            Setup = setup;
        }

        public TileObjectSo Tile { get; }

        /// <summary>Grid origin the object was placed at (its footprint starts here).</summary>
        public Vector3 Origin { get; }

        public Direction Direction { get; }

        /// <summary>The object's setup when it was removed, so undo puts it back configured; null or empty at defaults.</summary>
        public SavedSetupEntry[] Setup { get; }

        public RemovedTileRecord WithSetup(SavedSetupEntry[] setup) => new(Tile, Origin, Direction, setup);

        /// <summary>Identity is the slot: the setup describes the object, it doesn't tell two removals apart.</summary>
        public bool Equals(RemovedTileRecord other) =>
            Tile == other.Tile && Origin == other.Origin && Direction == other.Direction;

        public override bool Equals(object obj) => obj is RemovedTileRecord other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Tile, Origin, Direction);
    }
}
