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

        public MeshDirectionInfo[] GetMeshesAndDirections(AdjacencyMap adjacencyMap, bool vertical)
        {
            // Determine rotation and mesh specially for every single case.
            float[] rotation;
            Mesh[] mesh;

            AdjacencyShape shape = GetPipeShape(adjacencyMap, vertical);
            switch (shape)
            {
                case AdjacencyShape.Vertical:
                    mesh = new Mesh[adjacencyMap.CardinalConnectionCount];
                    rotation = new float[adjacencyMap.CardinalConnectionCount];
                    int j = 0;
                    foreach (Direction dir in TileHelper.CardinalDirections())
                    {
                        if (!adjacencyMap.HasConnection(dir)) continue;
                        mesh[j] = verticalMesh;
                        rotation[j] = TileHelper.AngleBetween(Direction.North, dir);
                        j++;
                    }
                    break;

                case AdjacencyShape.O:
                    mesh = new Mesh[] { o };
                    rotation = new float[] { 0 };
                    break;
                case AdjacencyShape.U:
                    mesh = new Mesh[] { u };
                    rotation = new float[1] { TileHelper.AngleBetween(Direction.North,
                        adjacencyMap.GetSingleConnection()) };
                    break;

                case AdjacencyShape.I:
                    mesh = new Mesh[] { i };
                    rotation = new float[1] { TileHelper.AngleBetween(Direction.North,
                        adjacencyMap.HasConnection(Direction.South) ? Direction.South : Direction.West) };
                    break;
                case AdjacencyShape.L:
                    mesh = new Mesh[] { l };
                    rotation = new float[] { TileHelper.AngleBetween(Direction.NorthEast,
                        adjacencyMap.GetDirectionBetweenTwoConnections()) };
                    break;
                case AdjacencyShape.T:
                    mesh = new Mesh[] { t };
                    rotation = new float[] { TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection()) };
                    break;
                case AdjacencyShape.X:
                    mesh = new Mesh[] { x };
                    rotation = new float[] { 0 };
                    break;
                default:
                    Debug.LogError($"Received unexpected shape from simple shape resolver: {shape}");
                    mesh = new Mesh[] { i };
                    rotation = new float[] { 0 };
                    break;
            }

            MeshDirectionInfo[] MeshesAndDirections = new MeshDirectionInfo[mesh.Length]; 

            for(int i=0; i< mesh.Length; i++)
            {
                MeshesAndDirections[i] = new MeshDirectionInfo { Mesh = mesh[i], Rotation = rotation[i] };
            }

            return MeshesAndDirections;
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
