using System;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Engine.Tile.TileRework.Connections.AdjacencyTypes
{
    /// <summary>
    /// Adjacency type used for objects that do require complex connections.
    /// </summary>
    [Serializable]
    public struct AdvancedConnector
    {
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;

        [Tooltip("A mesh where north connects to same type")]
        public Mesh u;

        [Tooltip("A mesh where north and south edges are connected")]
        public Mesh i;

        [Tooltip("A mesh where the north and east edges are connected, no corners")]
        public Mesh lNone;
        [Tooltip("A mesh where the north and east edges are connected, and NE is a corner")]
        public Mesh lSingle;

        [Tooltip("A mesh where the north, east, and west edges are connected, no corners")]
        public Mesh tNone;
        [Tooltip("A mesh where the north, east, and west edges are connected, NW is a corner")]
        public Mesh tSingleRight;
        [Tooltip("A mesh where the north, east, and west edges are connected, NE is a corner")]
        public Mesh tSingleLeft;
        [Tooltip("A mesh where north, east, and west edges are connected, NW & NE are corners")]
        public Mesh tDouble;

        [Tooltip("A mesh where all edges are connected, no corners")]
        public Mesh xNone;
        [Tooltip("A mesh where all edges are connected, SW is a corner")]
        public Mesh xSingle;
        [Tooltip("A mesh where all edges are connected, SW & SW are corners")]
        public Mesh xSide;
        [Tooltip("A mesh where all edges are connected, NW & SE are corners")]
        public Mesh xOpposite;
        [Tooltip("A mesh where all edges are connected, all but NE are corners")]
        public Mesh xTriple;
        [Tooltip("A mesh where all edges are connected, all corners")]
        public Mesh xQuad;

        public GameObject[] viewObstacles;
        public bool opaque;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap)
        {
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);
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
                case AdjacencyShape.LNone:
                    mesh = lNone;
                    rotation = TileHelper.AngleBetween(Direction.NorthEast, adjacencyMap.GetDirectionBetweenTwoConnections());
                    break;
                case AdjacencyShape.LSingle:
                    mesh = lSingle;
                    rotation = TileHelper.AngleBetween(Direction.NorthEast, adjacencyMap.GetDirectionBetweenTwoConnections());
                    break;
                case AdjacencyShape.TNone:
                    mesh = tNone;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.TSingleLeft:
                    mesh = tSingleLeft;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.TSingleRight:
                    mesh = tSingleRight;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.TDouble:
                    mesh = tDouble;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.XNone:
                    mesh = xNone;
                    break;
                case AdjacencyShape.XSingle:
                    mesh = xSingle;
                    Direction connectingDiagonal = adjacencyMap.GetSingleConnection(false);
                    rotation = connectingDiagonal == Direction.NorthEast ? 0f :
                        connectingDiagonal == Direction.SouthEast ? 90f :
                        connectingDiagonal == Direction.SouthWest ? 180f : -90f;
                    break;
                case AdjacencyShape.XOpposite:
                    mesh = xOpposite;
                    rotation = adjacencyMap.HasConnection(Direction.NorthEast) ? 0f : 90f;
                    break;
                case AdjacencyShape.XSide:
                    mesh = xSide;
                    rotation = TileHelper.AngleBetween(Direction.NorthWest, adjacencyMap.GetDirectionBetweenTwoConnections(false)) - 45f;
                    break;
                case AdjacencyShape.XTriple:
                    mesh = xTriple;
                    Direction nonConnectingDiagonal = adjacencyMap.GetSingleNonConnection(false);
                    rotation = nonConnectingDiagonal == Direction.NorthEast ? -90f :
                        nonConnectingDiagonal == Direction.SouthEast ? 0f :
                        nonConnectingDiagonal == Direction.SouthWest ? 90f : 180f;
                    break;
                case AdjacencyShape.XQuad:
                    mesh = xQuad;
                    break;
                default:
                    Debug.LogError($"Received unexpected shape from advanced shape resolver: {shape}");
                    mesh = o;
                    break;
            }

            //If someone knows of a more elegant way to do the same without switching the same variable twice, I'd like to hear it :)
            if (opaque)
            {
                switch (shape)
                {
                    case AdjacencyShape.U:
                        viewObstacles[0].SetActive(false);
                        viewObstacles[1].SetActive(false);
                        viewObstacles[2].SetActive(true);
                        viewObstacles[3].SetActive(false);
                        break;
                    case AdjacencyShape.I:
                        viewObstacles[0].SetActive(false);
                        viewObstacles[1].SetActive(false);
                        viewObstacles[2].SetActive(true);
                        viewObstacles[3].SetActive(true);
                        break;
                    case AdjacencyShape.LNone:
                    case AdjacencyShape.LSingle:
                        viewObstacles[0].SetActive(false);
                        viewObstacles[1].SetActive(true);
                        viewObstacles[2].SetActive(true);
                        viewObstacles[3].SetActive(false);
                        break;
                    case AdjacencyShape.TNone:
                    case AdjacencyShape.TSingleLeft:
                    case AdjacencyShape.TSingleRight:
                        viewObstacles[0].SetActive(true);
                        viewObstacles[1].SetActive(true);
                        viewObstacles[2].SetActive(false);
                        viewObstacles[3].SetActive(true);
                        break;
                    case AdjacencyShape.TDouble:
                    case AdjacencyShape.XNone:
                    case AdjacencyShape.XSingle:
                    case AdjacencyShape.XOpposite:
                    case AdjacencyShape.XSide:
                    case AdjacencyShape.XTriple:
                    case AdjacencyShape.XQuad:
                        viewObstacles[0].SetActive(true);
                        viewObstacles[1].SetActive(true);
                        viewObstacles[2].SetActive(true);
                        viewObstacles[3].SetActive(true);
                        break;
                }
            }

            return new MeshDirectionInfo { Mesh = mesh, Rotation = rotation };
        }
    }
}