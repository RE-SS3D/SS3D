using System;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Adjacency type used for objects that do require complex connections.
    /// </summary>
    [Serializable]
    public struct AdvancedConnector
    {
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;

        [Tooltip("A mesh where South connects to same type")]
        public Mesh u;

        [Tooltip("A mesh where South and south edges are connected")]
        public Mesh i;

        [Tooltip("A mesh where the South and West edges are connected, no corners")]
        public Mesh lNone;
        [Tooltip("A mesh where the South and West edges are connected, and NE is a corner")]
        public Mesh lSingle;

        [Tooltip("A mesh where the South, West, and East edges are connected, no corners")]
        public Mesh tNone;
        [Tooltip("A mesh where the South, West, and East edges are connected, NW is a corner")]
        public Mesh tSingleRight;
        [Tooltip("A mesh where the South, West, and East edges are connected, NE is a corner")]
        public Mesh tSingleLeft;
        [Tooltip("A mesh where South, West, and East edges are connected, NW & NE are corners")]
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
                    rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleConnection());
                    break;
                case AdjacencyShape.I:
                    mesh = i;
                    rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.HasConnection(Direction.South) ? Direction.South : Direction.West);
                    break;
                case AdjacencyShape.LNone:
                    mesh = lNone;
                    rotation = TileHelper.AngleBetween(Direction.SouthWest, adjacencyMap.GetDirectionBetweenTwoConnections());
                    break;
                case AdjacencyShape.LSingle:
                    mesh = lSingle;
                    rotation = TileHelper.AngleBetween(Direction.SouthWest, adjacencyMap.GetDirectionBetweenTwoConnections());
                    break;
                case AdjacencyShape.TNone:
                    mesh = tNone;
                    rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.TSingleLeft:
                    mesh = tSingleLeft;
                    rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.TSingleRight:
                    mesh = tSingleRight;
                    rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.TDouble:
                    mesh = tDouble;
                    rotation = TileHelper.AngleBetween(Direction.South, adjacencyMap.GetSingleNonConnection());
                    break;
                case AdjacencyShape.XNone:
                    mesh = xNone;
                    break;
                case AdjacencyShape.XSingle:
                    mesh = xSingle;
                    Direction connectingDiagonal = adjacencyMap.GetSingleConnection(false);
                    rotation = connectingDiagonal == Direction.SouthWest ? 0f :
                        connectingDiagonal == Direction.SouthWest ? 90f :
                        connectingDiagonal == Direction.SouthEast ? 180f : -90f;
                    break;
                case AdjacencyShape.XOpposite:
                    mesh = xOpposite;
                    rotation = adjacencyMap.HasConnection(Direction.SouthWest) ? 0f : 90f;
                    break;
                case AdjacencyShape.XSide:
                    mesh = xSide;
                    rotation = TileHelper.AngleBetween(Direction.SouthEast, adjacencyMap.GetDirectionBetweenTwoConnections(false)) - 45f;
                    break;
                case AdjacencyShape.XTriple:
                    mesh = xTriple;
                    Direction nonConnectingDiagonal = adjacencyMap.GetSingleNonConnection(false);
                    rotation = nonConnectingDiagonal == Direction.SouthWest ? -90f :
                        nonConnectingDiagonal == Direction.SouthWest ? 0f :
                        nonConnectingDiagonal == Direction.SouthEast ? 90f : 180f;
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