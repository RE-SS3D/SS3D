using UnityEngine;

namespace SS3D.Engine.Tile.TileRework.Connections.AdjacencyTypes
{
    /// <summary>
    /// Struct for storing which mesh and rotation to use. Used by the adjacency connectors.
    /// </summary>
    public struct MeshDirectionInfo
    {
        public Mesh Mesh;
        public float Rotation;
    }

    /// <summary>
    /// Stores the type of adjacency as well as if it exists or not. Used by Adjacency Map.
    /// </summary>
    public struct AdjacencyData
    {
        public AdjacencyData(string genericType, string specificType, bool exists)
        {
            GenericType = genericType;
            SpecificType = specificType;
            Exists = exists;
        }

        public string GenericType;
        public string SpecificType;
        public bool Exists;

        public override bool Equals(object other)
        {
            return other is AdjacencyData otherData && GenericType == otherData.GenericType && SpecificType == otherData.SpecificType &&
                   Exists == otherData.Exists;
        }
    }
}