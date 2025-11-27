using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    /// <summary>
    /// Adjacency type used for objects that are not centred on a tile.
    /// Used by the non-centered pipe layers.
    /// </summary>
    [Serializable]
    public struct OffsetPipeConnector
    {
        [Tooltip("A mesh where no edges are connected")]
        public Mesh O;

        [Tooltip("A mesh where the North edge is connected, can be rotated to the East")]
        public Mesh UNorth;

        [Tooltip("A mesh where the South edge is connected, can be rotated to the West")]
        public Mesh USouth;

        [Tooltip("A mesh where North & South edges are connected")]
        public Mesh I;

        [Tooltip("A mesh where North & South edges are connected")]
        public Mesh INorthMachinery;

        [Tooltip("A mesh where North & South edges are connected")]
        public Mesh ISouthMachinery;

        [Tooltip("A mesh where the North & East edges are connected")]
        public Mesh LNe;

        [Tooltip("A mesh where the North & West edges are connected")]
        public Mesh LNw;

        [Tooltip("A mesh where the South & East edges are connected")]
        public Mesh LSe;

        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh LSW;

        [Tooltip("A mesh where the South, West, & East edges are connected")]
        public Mesh TSwe;

        [Tooltip("A mesh where the North, East, & West edges are connected")]
        public Mesh TNew;

        [Tooltip("A mesh where the North, South, & West edges are connected")]
        public Mesh TNsw;

        [Tooltip("A mesh where the North, South, & East edges are connected")]
        public Mesh TNse;

        [Tooltip("A mesh where all edges are connected")]
        public Mesh X;

        public MeshDirectionInfo GetMeshAndDirection(AdjacencyMap pipeConnections, bool connectedToMachinery = false, Direction machineryDirection = Direction.North)
        {
            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = GetPipeOffsetShape(pipeConnections, connectedToMachinery, machineryDirection);
            switch (shape)
            {
                case AdjacencyShape.O:
                {
                    mesh = O;
                    break;
                }

                case AdjacencyShape.UNorth:
                {
                    mesh = UNorth;
                    rotation = TileHelper.AngleBetween(Direction.North, pipeConnections.GetSingleConnection());
                    break;
                }

                case AdjacencyShape.USouth:
                {
                    mesh = USouth;
                    rotation = TileHelper.AngleBetween(Direction.South, pipeConnections.GetSingleConnection());
                    break;
                }

                case AdjacencyShape.I:
                {
                    mesh = I;
                    rotation = TileHelper.AngleBetween(Direction.North, pipeConnections.HasConnection(Direction.North) ? Direction.North : Direction.East);
                    break;
                }

                case AdjacencyShape.INorthMachinery:
                {
                    mesh = INorthMachinery;
                    rotation = machineryDirection == Direction.North ? 180 : -90;
                    break;
                }

                case AdjacencyShape.ISouthMachinery:
                {
                    mesh = ISouthMachinery;
                    rotation = machineryDirection == Direction.South ? 0 : 90;
                    break;
                }

                case AdjacencyShape.LNorthWest:
                {
                    mesh = LNw;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.LNorthEast:
                {
                    mesh = LNe;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.LSouthEast:
                {
                    mesh = LSe;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.LSouthWest:
                {
                    mesh = LSW;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.TNorthSouthEast:
                {
                    mesh = TNse;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.TSouthWestEast:
                {
                    mesh = TSwe;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.TNorthSouthWest:
                {
                    mesh = TNsw;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.TNorthEastWest:
                {
                    mesh = TNew;
                    rotation = 90;
                    break;
                }

                case AdjacencyShape.X:
                {
                    mesh = X;
                    rotation = 90;
                    break;
                }

                default:
                {
                    Debug.LogError($"Received unexpected shape from offset shape resolver: {shape}");
                    mesh = O;
                    break;
                }
            }

            return new MeshDirectionInfo { Mesh = mesh, Rotation = rotation };
        }

        private static AdjacencyShape GetPipeOffsetShape(AdjacencyMap pipeConnections, bool connectedToMachinery, Direction machineryDirection)
        {
            int pipeConnectionCount = pipeConnections.CardinalConnectionCount;

            if (connectedToMachinery)
            {
                return machineryDirection switch
                {
                    Direction.North => AdjacencyShape.INorthMachinery,
                    Direction.South => AdjacencyShape.ISouthMachinery,
                    Direction.West => AdjacencyShape.ISouthMachinery,
                    _ => AdjacencyShape.INorthMachinery,
                };
            }

            if (pipeConnectionCount == 0)
            {
                return AdjacencyShape.O;
            }

            if (pipeConnectionCount == 1)
            {
                return pipeConnections.HasConnection(Direction.North) || pipeConnections.HasConnection(Direction.East) ?
                    AdjacencyShape.UNorth : AdjacencyShape.USouth;
            }

            // When two connections to pipes and they're opposite
            if (pipeConnectionCount == 2 && pipeConnections.HasConnection(Direction.North) == pipeConnections.HasConnection(Direction.South))
            {
                return AdjacencyShape.I;
            }

            // When two connections to pipes and they're adjacent
            if (pipeConnectionCount == 2 && pipeConnections.HasConnection(Direction.North) != pipeConnections.HasConnection(Direction.South))
            {
                Direction diagonal = pipeConnections.GetDirectionBetweenTwoConnections();
                return diagonal switch
                {
                    Direction.NorthEast => AdjacencyShape.LNorthWest,
                    Direction.SouthEast => AdjacencyShape.LNorthEast,
                    Direction.SouthWest => AdjacencyShape.LSouthEast,
                    _ => AdjacencyShape.LSouthWest,
                };
            }

            if (pipeConnectionCount == 3)
            {
                Direction missingConnection = pipeConnections.GetSingleNonConnection();
                return missingConnection switch
                {
                    Direction.North => AdjacencyShape.TNorthSouthEast,
                    Direction.East => AdjacencyShape.TSouthWestEast,
                    Direction.South => AdjacencyShape.TNorthSouthWest,
                    _ => AdjacencyShape.TNorthEastWest,
                };
            }

            if (pipeConnectionCount == 4)
            {
                return AdjacencyShape.X;
            }

            Debug.LogError(
                $"Could not resolve Offset Adjacency Shape for given Adjacency Map - {pipeConnections}");
            return AdjacencyShape.X;
        }
    }
}
