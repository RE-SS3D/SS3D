using Coimbra;
using SS3D.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    [Serializable]
    public struct DirectionnalShapeResolver
    {
        [FormerlySerializedAs("o")]
        [Tooltip("A mesh where no edges are connected")]
        public Mesh O;

        [FormerlySerializedAs("uLeft")]
        [Tooltip("A mesh where the South edge is connected")]
        public Mesh ULeft;

        [FormerlySerializedAs("uRight")]
        [Tooltip("A mesh where the South edge is connected")]
        public Mesh URight;

        [FormerlySerializedAs("i")]
        [Tooltip("A mesh where the South & south edges are connected")]
        public Mesh I;

        [FormerlySerializedAs("lIn")]
        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh LIn;

        [FormerlySerializedAs("lOut")]
        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh LOut;

        [Tooltip("A mesh where the South, West, and west edges are connected")]

        public static AdjacencyShape GetSimpleShape(AdjacencyMap adjacencyMap, Direction dir, List<PlacedTileObject> neighbours, PlacedTileObject actual)
        {
            int cardinalConnectionCount = adjacencyMap.CardinalConnectionCount;
            switch (cardinalConnectionCount)
            {
                case 0:
                    return AdjacencyShape.O;
                case 1:
                {
                    Direction directionConnection = adjacencyMap.GetSingleConnection(true);
                    Direction relative = TileHelper.GetRelativeDirection(dir, directionConnection);

                    // check if the neighbour is oriented in the same direction or in a diagonal adjacent one.
                    if (!TileHelper.GetAdjacentAndMiddleDirection(dir).Contains(neighbours[0].Direction))
                    {
                        return AdjacencyShape.O;
                    }

                    return relative == Direction.East ? AdjacencyShape.ULeft : AdjacencyShape.URight;
                }

                // When two connections, checks if they're opposite or adjacent
                case 2:
                {
                    if (adjacencyMap.HasConnection(Direction.North) != adjacencyMap.HasConnection(Direction.South))
                    {
                        return IsInsideConfiguration(neighbours, adjacencyMap, dir, actual) ? AdjacencyShape.LIn : AdjacencyShape.LOut;
                    }

                    // To get a I, neighbours directionnal need to be oriented the same way as this directionnal, or
                    // in an adjacent direction (basically if they take a LIn or LOut shape)
                    if (TileHelper.GetAdjacentAndMiddleDirection(dir).Contains(neighbours[0].Direction) && TileHelper.GetAdjacentAndMiddleDirection(dir).Contains(neighbours[1].Direction))
                    {
                        return AdjacencyShape.I;
                    }

                    // TODO add cases for ULeft and URight
                    return AdjacencyShape.O;
                }

                default:
                {
                    Debug.LogError($"Could not resolve Simple Adjacency Shape for given Adjacency Map - {adjacencyMap}");
                    return AdjacencyShape.O;
                }
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="adjacencyMap"> The connection with this directionnal.</param>
        /// <param name="direction"> this Directionnal direction, toward what it looks.</param>
        /// <param name="NeighboursDirection">Neigthbours direction are only cardinal neighbours</param>
        /// <returns></returns>
        public Tuple<Direction, MeshDirectionInfo> GetMeshAndDirectionAndRotation(
            AdjacencyMap adjacencyMap,
            Direction direction,
            List<PlacedTileObject> neighbours,
            PlacedTileObject actual)
        {
            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = GetSimpleShape(adjacencyMap, direction, neighbours, actual);

            // TODO : should set LIN and LOut shape toward the middle direction between their two connections, such that they are always
            // in a diagonal direction.
            // TODO : All other shape should have their direction reset to an appropriate value. That is necessary for a shape going from
            // LIN LOut to a simple one connection shape.
            switch (shape)
            {
                case AdjacencyShape.O:
                {
                    mesh = O;
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                }

                case AdjacencyShape.ULeft:
                {
                    mesh = ULeft;
                    List<Direction> closestsOfNeighbour = TileHelper.ClosestCardinalAdjacentTo(neighbours[0].Direction);
                    List<Direction> closestsOfThis = TileHelper.ClosestCardinalAdjacentTo(direction);
                    IEnumerable<Direction> closest = closestsOfNeighbour.Intersect(closestsOfThis);
                    direction = closest.FirstOrDefault();
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                }

                case AdjacencyShape.URight:
                {
                    mesh = URight;
                    List<Direction> closestsOfNeighbour = TileHelper.ClosestCardinalAdjacentTo(neighbours[0].Direction);
                    List<Direction> closestsOfThis = TileHelper.ClosestCardinalAdjacentTo(direction);
                    IEnumerable<Direction> closest = closestsOfNeighbour.Intersect(closestsOfThis);
                    direction = closest.FirstOrDefault();
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                }

                case AdjacencyShape.I:
                {
                    mesh = I;
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                }

                case AdjacencyShape.LIn:
                {
                    mesh = LIn;

                    // TODO : neighbours can also be LIn or LOut so their directions can be diagonal as well.
                    rotation = LOutLinRotation(adjacencyMap);
                    direction = LInLOutDirection(neighbours[0].Direction, neighbours[1].Direction);
                    break;
                }

                case AdjacencyShape.LOut:
                {
                    mesh = LOut;
                    rotation = LOutLinRotation(adjacencyMap);
                    direction = LInLOutDirection(neighbours[0].Direction, neighbours[1].Direction);
                    break;
                }

                default:
                {
                    Debug.LogError($"Received unexpected shape from simple shape resolver: {shape}");
                    mesh = O;
                    break;
                }
            }

            return new(direction,  new MeshDirectionInfo { Mesh = mesh, Rotation = rotation });
        }

        public Mesh ShapeToMesh(AdjacencyShape shape)
        {
            switch (shape)
            {
                case AdjacencyShape.O: return O;
                case AdjacencyShape.ULeft: return ULeft;
                case AdjacencyShape.URight: return URight;
                case AdjacencyShape.LIn: return LIn;
                case AdjacencyShape.LOut: return LOut;
                case AdjacencyShape.I: return I;
                default:
                {
                    Debug.LogError("adjacency shape not found, returning mesh o");
                    return O;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="adjacencyMap"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static bool IsInsideConfiguration(List<PlacedTileObject> neighbours, AdjacencyMap adjacencyMap, Direction dir, PlacedTileObject actual)
        {
            List<Direction> adjacencies = adjacencyMap.GetAdjacencies(true);

            PlacedTileObject neighbourWithDifferentOrientation = neighbours.Find(x => x.Direction != dir);

            Direction different = neighbourWithDifferentOrientation.Direction;

            PlacedTileObject otherneighbour = neighbours.Find(x => x != neighbourWithDifferentOrientation);

            return TileHelper.GetAdjacentAndMiddleDirection(dir).Contains(otherneighbour.Direction) && actual.AtDirectionOf(otherneighbour, different);
        }

        /// <summary>
        /// Simply find the 0 rotation of the LOut and LIn models, and depending on the adjacencies position of connected stuff,
        /// rotate accordingly.
        /// </summary>
        private static float LOutLinRotation(AdjacencyMap connections)
        {
            List<Direction> adjacencies = connections.GetAdjacencies(true);
            if (adjacencies.Contains(Direction.South) && adjacencies.Contains(Direction.East))
            {
                return 90f;
            }

            if (adjacencies.Contains(Direction.South) && adjacencies.Contains(Direction.West))
            {
                return 180f;
            }

            if (adjacencies.Contains(Direction.North) && adjacencies.Contains(Direction.West))
            {
                return 270f;
            }

            return 0f;
        }

        private static Direction LInLOutDirection(Direction firstNeighbour, Direction secondNeighbour)
        {
            if ((TileHelper.GetAdjacentAndMiddleDirection(Direction.East).Contains(firstNeighbour)
                && TileHelper.GetAdjacentAndMiddleDirection(Direction.North).Contains(secondNeighbour)) ||
                (TileHelper.GetAdjacentAndMiddleDirection(Direction.East).Contains(secondNeighbour)
                && TileHelper.GetAdjacentAndMiddleDirection(Direction.North).Contains(firstNeighbour)))
            {
                return Direction.NorthEast;
            }
            else if ((TileHelper.GetAdjacentAndMiddleDirection(Direction.East).Contains(firstNeighbour)
                && TileHelper.GetAdjacentAndMiddleDirection(Direction.South).Contains(secondNeighbour)) ||
                (TileHelper.GetAdjacentAndMiddleDirection(Direction.East).Contains(secondNeighbour)
                && TileHelper.GetAdjacentAndMiddleDirection(Direction.South).Contains(firstNeighbour)))
            {
                return Direction.SouthEast;
            }
            else if ((TileHelper.GetAdjacentAndMiddleDirection(Direction.West).Contains(firstNeighbour)
                && TileHelper.GetAdjacentAndMiddleDirection(Direction.South).Contains(secondNeighbour)) ||
                (TileHelper.GetAdjacentAndMiddleDirection(Direction.West).Contains(secondNeighbour)
                && TileHelper.GetAdjacentAndMiddleDirection(Direction.South).Contains(firstNeighbour)))
            {
                return Direction.SouthWest;
            }
            else
            {
                return Direction.NorthWest;
            }
        }
    }
}
