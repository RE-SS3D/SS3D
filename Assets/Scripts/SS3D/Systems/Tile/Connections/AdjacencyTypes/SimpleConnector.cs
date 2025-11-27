using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Adjacency type used for objects that do not require complex connections.
    /// </summary>
    [Serializable]
    public struct SimpleConnector : IMeshAndDirectionResolver
    {
        [FormerlySerializedAs("o")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh O;

        [FormerlySerializedAs("u")]
        [Tooltip("A mesh where the North edge is connected")]
        public Mesh U;

        [FormerlySerializedAs("i")]
        [Tooltip("A mesh where the North & South edges are connected")]
        public Mesh I;

        [FormerlySerializedAs("l")]
        [Tooltip("A mesh where the North & East edges are connected")]
        public Mesh L;

        [FormerlySerializedAs("t")]
        [Tooltip("A mesh where the South, East, and West edges are connected")]
        public Mesh T;

        [FormerlySerializedAs("x")]
        [Tooltip("A mesh where all edges are connected")]
        public Mesh X;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap)
        {
            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);
            switch (shape)
            {
                case AdjacencyShape.O:
                {
                    mesh = O;
                    break;
                }

                case AdjacencyShape.U:
                {
                    mesh = U;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleConnection());
                    break;
                }

                case AdjacencyShape.I:
                {
                    mesh = I;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.HasConnection(Direction.South) ? Direction.South : Direction.West);
                    break;
                }

                case AdjacencyShape.L:
                {
                    mesh = L;
                    rotation = TileHelper.AngleBetween(Direction.NorthEast, adjacencyMap.GetDirectionBetweenTwoConnections());
                    break;
                }

                case AdjacencyShape.T:
                {
                    mesh = T;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                }

                case AdjacencyShape.X:
                {
                    mesh = X;
                    break;
                }

                default:
                {
                    Debug.LogError($"Received unexpected shape from simple shape resolver: {shape}");
                    mesh = O;
                    break;
                }
            }

            return new MeshDirectionInfo { Mesh = mesh, Rotation = rotation };
        }
    }
}
