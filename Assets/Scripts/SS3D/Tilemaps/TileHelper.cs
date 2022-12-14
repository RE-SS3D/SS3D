﻿using System;
using System.Collections.Generic;
using SS3D.Tilemaps.Enums;
using UnityEngine;

namespace SS3D.Tilemaps
{
    public static class TileHelper
    {
        private static TileObjectLayer[] tileLayers;
        
        public static TileObjectLayer[] GetTileLayers()
        {
            if (tileLayers == null)
            {
                tileLayers = (TileObjectLayer[]) Enum.GetValues(typeof(TileObjectLayer));
            }
            return tileLayers;
        }

        public static bool ContainsSubLayers(TileObjectLayer objectLayer)
        {
            switch (objectLayer)
            {
                // case TileLayer.HighWallMount:
                // case TileLayer.LowWallMount:
                // case TileLayer.Overlay:
                //     return true;
                default:
                    return false;
            }
        }

        public static int GetSubLayerSize(TileObjectLayer objectLayer)
        {
            switch(objectLayer)
            {
                // case TileLayer.HighWallMount:
                // case TileLayer.LowWallMount:
                // case TileLayer.Overlay:
                    // return 4;
                default:
                    return 1;
            }
        }

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

        public static Direction Apply(Direction first, Direction second)
        {
            return (Direction)(((int)first + (int)second + 8) % 8);
        }

        public static Direction ToPerpendicularDirection(Direction dir)
        {
            return (Direction)(((int)dir + 1 % 4) * 2);
        }

        public static int GetRotationAngle(Direction dir)
        {
            return (int)dir * 45;
        }

        public static float AngleBetween(Direction from, Direction to)
        {
            return ((int)to - (int)from) * 45.0f;
        }

        public static Direction GetRelativeDirection(Direction to, Direction from)
        {
            return (Direction)((((int)to - (int)from) + 8) % 8);
        }

        public static Tuple<int, int> ToCardinalVector(Direction direction)
        {
            return new Tuple<int, int>(
                (direction > Direction.North && direction < Direction.South) ? 1 : (direction > Direction.South) ? -1 : 0,
                (direction > Direction.East && direction < Direction.West) ? -1 : (direction == Direction.East || direction == Direction.West) ? 0 : 1
            );
        }

        public static Direction GetOpposite(Direction direction)
        {
            return (Direction)(((int)direction + 4) % 8);
        }

        public static int GetDirectionIndex(Direction dir)
        {
            return (int)dir / 2;
        }

        public static Vector3 GetWorldPosition(int x, int y, float tileSize, Vector3 originPosition)
        {
            return new Vector3(x, 0, y) * tileSize + originPosition;
        }

        public static Vector2Int GetXY(Vector3 worldPosition, Vector3 originPosition)
        {
            return new Vector2Int((int)Math.Round(worldPosition.x - originPosition.x), (int)Math.Round(worldPosition.z - originPosition.z));
        }

        public static Vector3 GetClosestPosition(Vector3 worldPosition)
        {
            return new Vector3(Mathf.Round(worldPosition.x), 0, Mathf.Round(worldPosition.z));
        }

        public static List<Direction> CardinalDirections()
        {
            return new List<Direction> {Direction.North, Direction.East, Direction.South, Direction.West};
        }

        public static List<Direction> DiagonalDirections()
        {
            return new List<Direction> {Direction.NorthEast, Direction.SouthEast, Direction.SouthWest, Direction.NorthWest};
        }

        public static Direction GetDiagonalBetweenTwoCardinals(Direction cardinal1, Direction cardinal2)
        {
            List<Direction> givenCardinals = new List<Direction> {cardinal1, cardinal2};
            return givenCardinals.Contains(Direction.South) ? 
                givenCardinals.Contains(Direction.East) ? Direction.SouthEast : Direction.SouthWest : 
                givenCardinals.Contains(Direction.West) ? Direction.NorthWest : Direction.NorthEast;
        }

        public static Direction GetCardinalBetweenTwoDiagonals(Direction diagonal1, Direction diagonal2)
        {
            List<Direction> givenDiagonals = new List<Direction> {diagonal1, diagonal2};
            return givenDiagonals.Contains(Direction.SouthEast) ?
                givenDiagonals.Contains(Direction.NorthEast) ? Direction.East : Direction.South :
                givenDiagonals.Contains(Direction.SouthWest) ? Direction.West : Direction.North;
        }
    }
}