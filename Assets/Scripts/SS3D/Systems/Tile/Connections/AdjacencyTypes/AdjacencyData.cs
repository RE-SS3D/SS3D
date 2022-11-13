using System;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Stores the type of adjacency as well as if it exists or not. Used by Adjacency Map.
    /// </summary>
    public readonly struct AdjacencyData
    {
        public AdjacencyData(TileObjectGenericType genericType, TileObjectSpecificType specificType, bool exists)
        {
            GenericType = genericType;
            SpecificType = specificType;
            Exists = exists;
        }

        public readonly TileObjectGenericType GenericType;
        public readonly TileObjectSpecificType SpecificType;
        public readonly bool Exists;

        public override bool Equals(object other)
        {
            return other is AdjacencyData otherData && GenericType == otherData.GenericType && SpecificType == otherData.SpecificType &&
                   Exists == otherData.Exists;
        }

        public bool Equals(AdjacencyData other)
        {
            return GenericType == other.GenericType && SpecificType == other.SpecificType && Exists == other.Exists;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)GenericType, (int)SpecificType, Exists);
        }
    }
}