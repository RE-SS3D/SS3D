using UnityEngine;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Struct for storing which mesh and rotation to use. Used by the adjacency connectors.
    /// </summary>
    public struct MeshDirectionInfo
    {
        public Mesh Mesh;
        public float Rotation;
        public AdjacencyShape Shape;
    }
}