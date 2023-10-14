using System;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public struct DisposalPipeConnector
    {
        [Tooltip("Just set I shape for pipes")]
        public Mesh o;

        [Tooltip("Just set I shape for pipes")]
        public Mesh u;

        [Tooltip("A mesh where the South & south edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh l;
        [Tooltip("A mesh where the South, West, and west edges are connected")]
        public Mesh t;
        [Tooltip("A mesh where all edges are connected")]
        public Mesh x;
        [Tooltip("A mesh where the South & south edges are connected")]
        public Mesh verticalMesh;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap, bool vertical)
        {
            // Determine rotation and mesh specially for every single case.
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

            MeshDirectionInfo MeshAndDirection = new MeshDirectionInfo { Mesh = mesh, Rotation = rotation } ; 

            return MeshAndDirection;
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
