using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Uniquely identifies one gas pipe segment on the tile grid.
    /// </summary>
    public readonly struct GasPipeSegmentKey : IEquatable<GasPipeSegmentKey>
    {
        public readonly TileCoord Coord;
        public readonly TileLayer Layer;
        public readonly TileObjectSpecificType SpecificType;

        public GasPipeSegmentKey(TileCoord coord, TileLayer layer, TileObjectSpecificType specificType)
        {
            Coord = coord;
            Layer = layer;
            SpecificType = specificType;
        }

        public static GasPipeSegmentKey From(PlacedTileObject segment)
        {
            return new GasPipeSegmentKey(
                new TileCoord(segment.MapId, segment.WorldOrigin),
                segment.Layer,
                segment.SpecificType);
        }

        public bool Equals(GasPipeSegmentKey other) =>
            Coord.MapId == other.Coord.MapId
            && Coord.Grid == other.Coord.Grid
            && Layer == other.Layer
            && SpecificType == other.SpecificType;

        public override bool Equals(object obj) => obj is GasPipeSegmentKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Coord.MapId;
                hash = (hash * 397) ^ Coord.Grid.GetHashCode();
                hash = (hash * 397) ^ (int)Layer;
                hash = (hash * 397) ^ (int)SpecificType;
                return hash;
            }
        }

        public override string ToString() => $"{Coord.Grid}@{Layer}/{SpecificType}";
    }
}
