﻿using SS3D.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Helper class for the tilemap to deal with layers and rotations
    /// </summary>
    public static class TileHelper
    {
        private static TileLayer[] TileLayers;

        public static Direction GetNextDir(Direction dir)
        {
            switch (dir)
            {
                default:
                case Direction.South: return Direction.West;
                case Direction.West: return Direction.North;
                case Direction.North: return Direction.East;
                case Direction.East: return Direction.South;
            }
        }

        public static Direction GetNextCardinalDir(Direction dir)
        {
            switch (dir)
            {
                default:
                case Direction.South: return Direction.West;
                case Direction.SouthWest: return Direction.NorthWest;
                case Direction.West: return Direction.North;
                case Direction.NorthWest: return Direction.NorthEast;
                case Direction.North: return Direction.East;
                case Direction.NorthEast: return Direction.SouthEast;
                case Direction.East: return Direction.South;
                case Direction.SouthEast: return Direction.SouthWest;
            }
        }

        public static int GetRotationAngle(Direction dir)
        {
            return (int)dir * 45;
        }

        public static TileLayer[] GetTileLayers()
        {
            if (TileLayers == null)
            {
                TileLayers = (TileLayer[])Enum.GetValues(typeof(TileLayer));
            }
            return TileLayers;
        }

        public static Tuple<int, int> ToCardinalVector(Direction direction)
        {
            return new Tuple<int, int>(
                (direction > Direction.North && direction < Direction.South) ? 1 : (direction > Direction.South) ? -1 : 0,
                (direction > Direction.East && direction < Direction.West) ? -1 : (direction == Direction.East || direction == Direction.West) ? 0 : 1
            );
        }

        public static Vector3 GetClosestPosition(Vector3 worldPosition)
        {
            return new Vector3(Mathf.Round(worldPosition.x), 0, Mathf.Round(worldPosition.z));
        }

        public static Direction GetRelativeDirection(Direction to, Direction from)
        {
            return (Direction)((((int)to - (int)from) + 8) % 8);
        }

        public static int GetDirectionIndex(Direction dir)
        {
            return (int)dir / 2;
        }


        public static List<Direction> CardinalDirections()
        {
            return new List<Direction> { Direction.North, Direction.East, Direction.South, Direction.West };
        }

        public static List<Direction> DiagonalDirections()
        {
            return new List<Direction> { Direction.NorthEast, Direction.SouthEast, Direction.SouthWest, Direction.NorthWest };
        }

        public static Direction GetDiagonalBetweenTwoCardinals(Direction cardinal1, Direction cardinal2)
        {
            List<Direction> givenCardinals = new List<Direction> { cardinal1, cardinal2 };
            return givenCardinals.Contains(Direction.South) ?
                givenCardinals.Contains(Direction.East) ? Direction.SouthEast : Direction.SouthWest :
                givenCardinals.Contains(Direction.West) ? Direction.NorthWest : Direction.NorthEast;
        }

        public static Direction GetCardinalBetweenTwoDiagonals(Direction diagonal1, Direction diagonal2)
        {
            List<Direction> givenDiagonals = new List<Direction> { diagonal1, diagonal2 };
            return givenDiagonals.Contains(Direction.SouthEast) ?
                givenDiagonals.Contains(Direction.NorthEast) ? Direction.East : Direction.South :
                givenDiagonals.Contains(Direction.SouthWest) ? Direction.West : Direction.North;
        }

        public static float AngleBetween(Direction from, Direction to)
        {
            return ((int)to - (int)from) * 45.0f;
        }

        public static Direction GetOpposite(Direction direction)
        {
            return (Direction)(((int)direction + 4) % 8);
        }

        /// <summary>
        /// Return the difference in coordinates for a neighbour tile in front of another one facing
        /// a particular direction.
        /// e.g If the original one is facing north, return (0,1), because, the tile in front of the original
        /// one will be just north of the original one (hence plus one on the y axis).
        /// </summary>
        public static Vector2Int CoordinateDifferenceInFrontFacingDirection(Direction direction)
        {
            switch(direction)
            {
                case Direction.North:
                    return new Vector2Int(0, 1);

                case Direction.NorthEast:
                    return new Vector2Int(1, 1);

                case Direction.East:
                    return new Vector2Int(1, 0);

                case Direction.SouthEast:
                    return new Vector2Int(1, -1);

                case Direction.South:
                    return new Vector2Int(0, -1);

                case Direction.SouthWest:
                    return new Vector2Int(-1, -1);

                case Direction.West:
                    return new Vector2Int(-1, 0);

                case Direction.NorthWest:
                    return new Vector2Int(-1, 1);

                default:
                    Debug.LogError("direction not handled, returning (0,0)");
                    return new Vector2Int(0, 0);
            }
        }




    }
}