using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Adjacency type used for objects that do require complex connections.
    /// </summary>
    [Serializable]
    public struct AdvancedConnector : IMeshAndDirectionResolver
    {
        [FormerlySerializedAs("o")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh O;

        [FormerlySerializedAs("u")]
        [Tooltip("A mesh where North connects to same type")]
        public Mesh U;

        [FormerlySerializedAs("i")]
        [Tooltip("A mesh where North and South edges are connected")]
        public Mesh I;

        [FormerlySerializedAs("lNone")]
        [Tooltip("A mesh where North and East edges are connected, no corners")]
        public Mesh LNone;

        [FormerlySerializedAs("lSingle")]
        [Tooltip("A mesh where North and East edges are connected, and NE is a corner")]
        public Mesh LSingle;

        [FormerlySerializedAs("tNone")]
        [Tooltip("A mesh where South, West, and East edges are connected, no corners")]
        public Mesh TNone;

        [FormerlySerializedAs("tSingleRight")]
        [Tooltip("A mesh where South, West, and East edges are connected, SE is a corner")]
        public Mesh TSingleRight;

        [FormerlySerializedAs("tSingleLeft")]
        [Tooltip("A mesh where South, West, and East edges are connected, SW is a corner")]
        public Mesh TSingleLeft;

        [FormerlySerializedAs("tDouble")]
        [Tooltip("A mesh where South, West, and East edges are connected, SE & NW are corners")]
        public Mesh TDouble;

        [FormerlySerializedAs("xNone")]
        [Tooltip("A mesh where all edges are connected, no corners")]
        public Mesh XNone;

        [FormerlySerializedAs("xSingle")]
        [Tooltip("A mesh where all edges are connected, NE is a corner")]
        public Mesh XSingle;

        [FormerlySerializedAs("xSide")]
        [Tooltip("A mesh where all edges are connected, NE & NW are corners")]
        public Mesh XSide;

        [FormerlySerializedAs("xOpposite")]
        [Tooltip("A mesh where all edges are connected, NE & SW are corners")]
        public Mesh XOpposite;

        [FormerlySerializedAs("xTriple")]
        [Tooltip("A mesh where all edges are connected, NE, NW, & SW are corners")]
        public Mesh XTriple;

        [FormerlySerializedAs("xQuad")]
        [Tooltip("A mesh where all edges are connected, all corners")]
        public Mesh XQuad;

        [FormerlySerializedAs("viewObstacles")]
        public GameObject[] ViewObstacles;

        [FormerlySerializedAs("opaque")]
        public bool Opaque;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap adjacencyMap)
        {
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = AdjacencyShapeResolver.GetAdvancedShape(adjacencyMap);
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
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.HasConnection(Direction.North) ? Direction.North : Direction.East);
                    break;
                }

                case AdjacencyShape.LNone:
                {
                    mesh = LNone;
                    rotation = TileHelper.AngleBetween(Direction.NorthEast, adjacencyMap.GetDirectionBetweenTwoConnections());
                    break;
                }

                case AdjacencyShape.LSingle:
                {
                    mesh = LSingle;
                    rotation = TileHelper.AngleBetween(Direction.NorthEast, adjacencyMap.GetDirectionBetweenTwoConnections());
                    break;
                }

                case AdjacencyShape.TNone:
                {
                    mesh = TNone;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                }

                case AdjacencyShape.TSingleLeft:
                {
                    mesh = TSingleLeft;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                }

                case AdjacencyShape.TSingleRight:
                {
                    mesh = TSingleRight;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                }

                case AdjacencyShape.TDouble:
                {
                    mesh = TDouble;
                    rotation = TileHelper.AngleBetween(Direction.North, adjacencyMap.GetSingleNonConnection());
                    break;
                }

                case AdjacencyShape.XNone:
                {
                    mesh = XNone;
                    break;
                }

                case AdjacencyShape.XSingle:
                {
                    mesh = XSingle;
                    Direction connectingDiagonal = adjacencyMap.GetSingleConnection(false);
                    rotation = connectingDiagonal switch
                    {
                        Direction.NorthEast => 0f,
                        Direction.SouthEast => 90f,
                        Direction.SouthWest => 180f,
                        _ => 270f,
                    };
                    break;
                }

                case AdjacencyShape.XOpposite:
                {
                    mesh = XOpposite;
                    rotation = adjacencyMap.HasConnection(Direction.NorthEast) ? 0f : 90f;
                    break;
                }

                case AdjacencyShape.XSide:
                {
                    mesh = XSide;
                    rotation = TileHelper.AngleBetween(Direction.NorthWest, adjacencyMap.GetDirectionBetweenTwoConnections(false)) - 45f;
                    break;
                }

                case AdjacencyShape.XTriple:
                {
                    mesh = XTriple;
                    Direction nonConnectingDiagonal = adjacencyMap.GetSingleNonConnection(false);
                    rotation = nonConnectingDiagonal switch
                    {
                        Direction.NorthEast => 270f,
                        Direction.SouthEast => 0f,
                        Direction.SouthWest => 90f,
                        _ => 180f,
                    };
                    break;
                }

                case AdjacencyShape.XQuad:
                {
                    mesh = XQuad;
                    break;
                }

                default:
                {
                    Debug.LogError($"Received unexpected shape from advanced shape resolver: {shape}");
                    mesh = O;
                    break;
                }
            }

            // If someone knows of a more elegant way to do the same without switching the same variable twice, I'd like to hear it :)
            if (!Opaque)
            {
                return new MeshDirectionInfo
                {
                    Mesh = mesh, Rotation = rotation, Shape = shape,
                };
            }

            switch (shape)
            {
                case AdjacencyShape.U:
                {
                    ViewObstacles[0].SetActive(false);
                    ViewObstacles[1].SetActive(false);
                    ViewObstacles[2].SetActive(true);
                    ViewObstacles[3].SetActive(false);
                    break;
                }

                case AdjacencyShape.I:
                {
                    ViewObstacles[0].SetActive(false);
                    ViewObstacles[1].SetActive(false);
                    ViewObstacles[2].SetActive(true);
                    ViewObstacles[3].SetActive(true);
                    break;
                }

                case AdjacencyShape.LNone:
                case AdjacencyShape.LSingle:
                {
                    ViewObstacles[0].SetActive(false);
                    ViewObstacles[1].SetActive(true);
                    ViewObstacles[2].SetActive(true);
                    ViewObstacles[3].SetActive(false);
                    break;
                }

                case AdjacencyShape.TNone:
                case AdjacencyShape.TSingleLeft:
                case AdjacencyShape.TSingleRight:
                {
                    ViewObstacles[0].SetActive(true);
                    ViewObstacles[1].SetActive(true);
                    ViewObstacles[2].SetActive(false);
                    ViewObstacles[3].SetActive(true);
                    break;
                }

                case AdjacencyShape.TDouble:
                case AdjacencyShape.XNone:
                case AdjacencyShape.XSingle:
                case AdjacencyShape.XOpposite:
                case AdjacencyShape.XSide:
                case AdjacencyShape.XTriple:
                case AdjacencyShape.XQuad:
                {
                    ViewObstacles[0].SetActive(true);
                    ViewObstacles[1].SetActive(true);
                    ViewObstacles[2].SetActive(true);
                    ViewObstacles[3].SetActive(true);
                    break;
                }
            }

            return new MeshDirectionInfo { Mesh = mesh, Rotation = rotation, Shape = shape };
        }
    }
}
