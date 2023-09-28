using SS3D.Logging;
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
        private static TileLayer[] tileLayers;

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

        public static int GetRotationAngle(Direction dir)
        {
            return (int)dir * 45;
        }

        public static TileLayer[] GetTileLayers()
        {
            if (tileLayers == null)
            {
                tileLayers = (TileLayer[])Enum.GetValues(typeof(TileLayer));
            }
            return tileLayers;
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

        public static bool IsCardinalDirection(Direction dir)
        {
            return (int) dir == 0 || (int) dir == 2 || (int) dir == 4 || (int) dir == 6 ;
        }

        public static ITileLocation CreateTileLocation(TileLayer layer, int x, int y)
        {
            switch (layer)
            {
                case TileLayer.Plenum:
                    return new SingleTileLocation(layer, x, y);
                case TileLayer.Turf:
                    return new SingleTileLocation(layer, x, y);
                case TileLayer.Wire:
                    return new SingleTileLocation(layer, x, y);
                case TileLayer.Disposal:
                    return new SingleTileLocation(layer, x, y);
                case TileLayer.Pipes:
                    return new SingleTileLocation(layer, x, y);
                case TileLayer.WallMountHigh:
                    return new CardinalTileLocation(layer, x, y);
                case TileLayer.WallMountLow:
                    return new CardinalTileLocation(layer, x, y);
                case TileLayer.FurnitureBase:
                    return new SingleTileLocation(layer, x, y);
                case TileLayer.FurnitureTop:
                    return new SingleTileLocation(layer, x, y);
                case TileLayer.Overlays:
                    return new SingleTileLocation(layer, x, y);
                default:
                    Debug.LogError($"no objects defined for layer {layer}, add a case to this switch.");
                    return null;
            }
        }
    }
}