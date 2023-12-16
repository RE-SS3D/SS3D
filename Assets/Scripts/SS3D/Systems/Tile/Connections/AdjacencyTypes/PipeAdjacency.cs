using System;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Script helping disposal pipes to determine their shape, mesh and rotation. Used by disposal pipes
    /// and centered atmospheric pipe layers. Based on the Simple Adjacency script.
    /// Disposal pipes are a bit special because they can connect vertically with disposal furniture.
    /// </summary>
    [Serializable]
    public struct DisposalPipeConnector
    {
        /// <summary>
        /// If pipe set doesn't have 'o' mesh, set it to 'u'.
        /// If pipe set doesn't have 'o' and 'u' mesh, set them both to 'i'.
        /// </summary>
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the North edge is connected")]
        public Mesh u;
        [Tooltip("A mesh where the North & South edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the North & East edges are connected")]
        public Mesh l;
        [Tooltip("A mesh where the South, East, and West edges are connected")]
        public Mesh t;
        [Tooltip("A mesh where all edges are connected")]
        public Mesh x;
        [Tooltip("A mesh variant of 'u'; where the North & Vertical edges are connected")]
        public Mesh verticalMesh;

        /// <summary>
        /// Get all info needed to update correctly disposal pipes.
        /// </summary>
        /// <param name="adjacencyMap"> Disposal pipe connections with all horizontal neighbours </param>
        /// <param name="vertical"> Disposal pipe connections with its single potential
        /// vertical neighbour. True if it exists.</param>
        public Tuple<Mesh, float, AdjacencyShape> GetMeshRotationShape(AdjacencyMap adjacencyMap, bool vertical)
        {
            float rotation = 0;
            Mesh mesh;

            AdjacencyShape shape = GetPipeShape(adjacencyMap, vertical);
            switch (shape)
            {
                case AdjacencyShape.Vertical:
                    mesh = verticalMesh;
                    break;

                case AdjacencyShape.O:
                    mesh = o ;
                    break;
                case AdjacencyShape.U:
                    mesh = u ;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleConnection());
                    break;

                case AdjacencyShape.I:
                    mesh =  i ;
                    rotation = TileHelper.AngleBetween(Direction.North,
                        adjacencyMap.HasConnection(Direction.South) ? Direction.South : Direction.West);
                    break;
                case AdjacencyShape.L:
                    mesh = l ;
                    rotation = TileHelper.AngleBetween(Direction.NorthEast,
                        adjacencyMap.GetDirectionBetweenTwoConnections());
                    break;
                case AdjacencyShape.T:
                    mesh =  t ;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.X:
                    mesh = x ;
                    break;
                default:
                    Debug.LogError($"Received unexpected shape from simple shape resolver: {shape}");
                    mesh = i ;
                    break;
            }

            return new Tuple<Mesh, float, AdjacencyShape>(mesh, rotation, shape);
        }

        private AdjacencyShape GetPipeShape(AdjacencyMap adjacencyMap, bool vertical)
        {
            if (vertical) return AdjacencyShape.Vertical;

            int connectionCount = adjacencyMap.CardinalConnectionCount;

            switch (connectionCount)
            {
                case 0:
                    return AdjacencyShape.O;
                case 1:
                    return AdjacencyShape.U;
                //When two connections, checks if they're opposite or adjacent
                case 2:
                    return adjacencyMap.HasConnection(Direction.North)
                        == adjacencyMap.HasConnection(Direction.South) ?
                        AdjacencyShape.I : AdjacencyShape.L;
                case 3:
                    return AdjacencyShape.T;
                case 4:
                    return AdjacencyShape.X;
                default:
                    Debug.LogError($"Could not resolve Simple Adjacency Shape for given Adjacency Map - {adjacencyMap}");
                    return AdjacencyShape.I;
            }
        }
    }
}
