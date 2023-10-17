using Coimbra;
using System;
using System.Collections;
using System.Collections.Generic;
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
        /// <param name="adjacencyMap"></param>
        /// <param name="direction"></param>
        /// <param name="NeighboursDirection">Neigthbours direction are only cardinal neighbours</param>
        /// <returns></returns>
        public Tuple<Direction, MeshDirectionInfo> GetMeshAndDirectionAndRotation(AdjacencyMap adjacencyMap, Direction direction, List<Direction> neighboursDirection)
        {
            // Determine rotation and mesh specially for every single case.
            float rotation = 0.0f;
            Mesh mesh;

            AdjacencyShape shape = GetSimpleShape(adjacencyMap, direction, neighboursDirection);


            switch (shape)
            {
                case AdjacencyShape.O:
                    mesh = o;
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                case AdjacencyShape.ULeft:
                    mesh = uLeft;
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                case AdjacencyShape.URight:
                    mesh = uRight;
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                case AdjacencyShape.I:
                    mesh = i;
                    rotation = TileHelper.AngleBetween(Direction.North, direction);
                    break;
                case AdjacencyShape.LIn:
                    mesh = lIn;
                    rotation = LOutLinRotation(adjacencyMap);
                    break;
                case AdjacencyShape.LOut:
                    mesh = lOut;
                    rotation = LOutLinRotation(adjacencyMap);
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
                    var directionConnection = adjacencyMap.GetSingleConnection(true);
                    var relative =TileHelper.GetRelativeDirection(dir, directionConnection);
                    if(!NeighboursDirection.Contains(dir)) return AdjacencyShape.O;
                    if (relative == Direction.East)
                        return AdjacencyShape.ULeft;
                    else return AdjacencyShape.URight;
                //When two connections, checks if they're opposite or adjacent
                case 2:

                    // TODO : should not connect in I if two sofas are facing away, except if those two sofas are taking LIN or LOut shape.
                     if (adjacencyMap.HasConnection(Direction.North) == adjacencyMap.HasConnection(Direction.South)) 
                        return AdjacencyShape.I;

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
                    return adjacencies.Contains(Direction.North) &&
                        (adjacencies.Contains(Direction.East) || adjacencies.Contains(Direction.West));
                case Direction.East:
                    return adjacencies.Contains(Direction.East) &&
                        (adjacencies.Contains(Direction.North) || adjacencies.Contains(Direction.South));
                case Direction.South:
                    return adjacencies.Contains(Direction.South) &&
                        (adjacencies.Contains(Direction.East) || adjacencies.Contains(Direction.West));
                case Direction.West:
                    return adjacencies.Contains(Direction.West) &&
                        (adjacencies.Contains(Direction.South) || adjacencies.Contains(Direction.North));
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
    }
}
