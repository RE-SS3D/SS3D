using System;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Engine.Tile.TileRework.Connections.AdjacencyTypes
{
    /// <summary>
    /// Adjacency type used for objects that do not require complex connections.
    /// </summary>
    [Serializable]
    public struct SimpleConnector
    {
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the north edge is connected")]
        public Mesh u;
        [Tooltip("A mesh where the north & south edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the north & east edges are connected")]
        public Mesh l;
        [Tooltip("A mesh where the north, east, and west edges are connected")]
        public Mesh t;
        [Tooltip("A mesh where all edges are connected")]
        public Mesh x;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap)
        {
            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = AdjacencyShapeResolver.GetSimpleShape(adjacencyMap);
            switch (shape)
            {
                case AdjacencyShape.O:
                    mesh = o;
                    break;
                case AdjacencyShape.U:
                    mesh = u;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleConnection());
                    break;
                case AdjacencyShape.I:
                    mesh = i;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.HasConnection(Direction.North) ? Direction.North : Direction.East);
                    break;
                case AdjacencyShape.L:
                    mesh = l;
                    rotation = TileHelper.AngleBetween(Direction.NorthEast, adjacencyMap.GetDirectionBetweenTwoConnections());
                    break;
                case AdjacencyShape.T:
                    mesh = t;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.X:
                    mesh = x;
                    break;
                default:
                    Debug.LogError($"Received unexpected shape from simple shape resolver: {shape}");
                    mesh = o;
                    break;
            }

            return new MeshDirectionInfo { Mesh = mesh, Rotation = rotation };
        }
    }
}