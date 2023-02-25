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
    }
}