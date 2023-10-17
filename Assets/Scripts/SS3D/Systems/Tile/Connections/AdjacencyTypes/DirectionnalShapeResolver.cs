using Coimbra;
using SS3D.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile.Connections.AdjacencyTypes
{
    [Serializable]
    public struct DirectionnalShapeResolver
    {
        [Tooltip("A mesh where no edges are connected")]
        public Mesh o;
        [Tooltip("A mesh where the South edge is connected")]
        public Mesh uLeft;
        [Tooltip("A mesh where the South edge is connected")]
        public Mesh uRight;
        [Tooltip("A mesh where the South & south edges are connected")]
        public Mesh i;
        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh lIn;
        [Tooltip("A mesh where the South & West edges are connected")]
        public Mesh lOut;
        [Tooltip("A mesh where the South, West, and west edges are connected")]

        /// <summary>
        /// 
        /// </summary>
        /// <param name="adjacencyMap"> The connection with this directionnal.</param>
        /// <param name="direction"> this Directionnal direction, toward what it looks.</param>
        /// <param name="NeighboursDirection">Neigthbours direction are only cardinal neighbours</param>
        /// <returns></returns>
        public Tuple<Direction, MeshDirectionInfo> GetMeshAndDirectionAndRotation(AdjacencyMap adjacencyMap, Direction direction, List<Direction> neighboursDirection)
        {
            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = GetSimpleShape(adjacencyMap, direction, neighboursDirection);

            // TODO : should set LIN and LOut shape toward the middle direction between their two connections, such that they are always
            // in a diagonal direction.
            // TODO : All other shape should have their direction reset to an appropriate value. That is necessary for a shape going from
            // LIN LOut to a simple one connection shape.
            switch (shape)
            {
                case AdjacencyShape.O:
                    mesh = o;
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                case AdjacencyShape.ULeft:
                    mesh = uLeft;
                    var closestsOfNeighbour = TileHelper.ClosestCardinalAdjacentTo(neighboursDirection[0]);
                    var closestsOfThis = TileHelper.ClosestCardinalAdjacentTo(direction);
                    var closest = closestsOfNeighbour.Intersect(closestsOfThis);
                    direction = closest.FirstOrDefault();
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                case AdjacencyShape.URight:
                    mesh = uRight;
                    closestsOfNeighbour = TileHelper.ClosestCardinalAdjacentTo(neighboursDirection[0]);
                    closestsOfThis = TileHelper.ClosestCardinalAdjacentTo(direction);
                    closest = closestsOfNeighbour.Intersect(closestsOfThis);
                    direction = closest.FirstOrDefault();
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                case AdjacencyShape.I:
                    mesh = i;
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                case AdjacencyShape.LIn:
                    mesh = lIn;
                    // TODO : neighbours can also be LIn or LOut so their directions can be diagonal as well.
                    rotation = LOutLinRotation(adjacencyMap);
                    direction = LInLOutDirection(neighboursDirection[0], neighboursDirection[1]);
                    break;
                case AdjacencyShape.LOut:
                    mesh = lOut;
                    rotation = LOutLinRotation(adjacencyMap);
                    direction = LInLOutDirection(neighboursDirection[0], neighboursDirection[1]);
                    break;
                default:
                    Debug.LogError($"Received unexpected shape from simple shape resolver: {shape}");
                    mesh = o;
                    break;
            }

            return new Tuple<Direction, MeshDirectionInfo> (direction,  new MeshDirectionInfo { Mesh = mesh, Rotation = rotation });
        }

        public static AdjacencyShape GetSimpleShape(AdjacencyMap adjacencyMap, Direction dir, List<Direction> NeighboursDirection)
        {
            int cardinalConnectionCount = adjacencyMap.CardinalConnectionCount;
            switch (cardinalConnectionCount)
            {
                case 0:
                    return AdjacencyShape.O;
                case 1:
                    // TODO should check if the single neighbour connection is adjacent, not only the same.
                    // currently prevents correct U connection with LIn LOut shapes.
                    var directionConnection = adjacencyMap.GetSingleConnection(true);
                    var relative = TileHelper.GetRelativeDirection(dir, directionConnection);
                    if(!TileHelper.GetAdjacentAndMiddleDirection(dir).Contains(NeighboursDirection[0])) 
                        return AdjacencyShape.O;
                    if (relative == Direction.East)
                        return AdjacencyShape.ULeft;
                    else return AdjacencyShape.URight;
                //When two connections, checks if they're opposite or adjacent
                case 2:

                     if (adjacencyMap.HasConnection(Direction.North) == adjacencyMap.HasConnection(Direction.South))
                     {
                        // To get a I, neighbours directionnal need to be oriented the same way as this directionnal, or
                        // in an adjacent direction (basically if they take a LIn or LOut shape)
                        if (TileHelper.GetAdjacentAndMiddleDirection(dir).Contains(NeighboursDirection[0]) &&
                            TileHelper.GetAdjacentAndMiddleDirection(dir).Contains(NeighboursDirection[1]))
                            return AdjacencyShape.I;
                        else return AdjacencyShape.O;
                     } 

                    if (IsInsideConfiguration(adjacencyMap, dir))
                        return AdjacencyShape.LIn;
                    else
                        return AdjacencyShape.LOut;
                     
                default:
                    Debug.LogError($"Could not resolve Simple Adjacency Shape for given Adjacency Map - {adjacencyMap}");
                    return AdjacencyShape.O;
            }
        }

        /// <summary>
        /// It's useful to draw the 8 different configuration of LIn shape
        /// with 3 couch, one in the middle and two other in cardinal positions. Consider the middle couch can point towards any cardinal
        /// direction. We don't care about the directions of the two other couches, what matters is their position relative to the middle one.
        /// This help understand the code below.
        /// </summary>
        /// <param name="adjacencyMap"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static bool IsInsideConfiguration(AdjacencyMap adjacencyMap, Direction dir)
        {
            var adjacencies =  adjacencyMap.GetAdjacencies(true);

            switch (dir)
            {
                case Direction.North:
                    return adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.North)) &&
                        ( adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.East)) ||
                        adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.West)));
                case Direction.East:
                    return adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.East)) &&
                        (adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.North)) ||
                        adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.South)));
                case Direction.South:
                    return adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.South)) &&
                        (adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.East)) ||
                        adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.West)));
                case Direction.West:
                    return adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.West)) &&
                        (adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.South)) ||
                        adjacencies.ContainsAny(TileHelper.GetAdjacentAndMiddleDirection(Direction.North)));
                default:
                    return false;
            }
        }

        /// <summary>
        /// Simply find the 0 rotation of the LOut and LIn models, and depending on the adjacencies position of connected stuff,
        /// rotate accordingly.
        /// </summary>
        private static float LOutLinRotation(AdjacencyMap connections)
        {
            var adjacencies = connections.GetAdjacencies(true);
            if(adjacencies.Contains(Direction.South) && adjacencies.Contains(Direction.East))
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
            if((TileHelper.GetAdjacentAndMiddleDirection(Direction.East).Contains(firstNeighbour) 
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
