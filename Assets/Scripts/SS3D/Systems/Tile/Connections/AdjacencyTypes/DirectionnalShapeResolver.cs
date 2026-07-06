using System;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Prefab-serialized mesh assets for directional connectors.
    /// Geometry is resolved by <see cref="DirectionalConfigurationEvaluator"/>.
    /// </summary>
    [Serializable]
    public struct DirectionnalShapeResolver
    {
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the South edge is connected")]
        public Mesh uLeft;
        [Tooltip("A mesh where the South edge is connected")]
        public Mesh uRight;
        [Tooltip("A mesh where the South & south edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh lIn;
        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh lOut;

        public Mesh ShapeToMesh(AdjacencyShape shape)
        {
            switch (shape)
            {
                case AdjacencyShape.O: return o;
                case AdjacencyShape.ULeft: return uLeft;
                case AdjacencyShape.URight: return uRight;
                // Mesh asset names are inverted relative to the LIn/LOut configuration names.
                case AdjacencyShape.LIn: return lOut;
                case AdjacencyShape.LOut: return lIn;
                case AdjacencyShape.I: return i;
                default:
                    Debug.LogError("adjacency shape not found, returning mesh o");
                    return o;
            }
        }
    }
}
