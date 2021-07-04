using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public enum TileLayer
    {
        Plenum = 0,
        Atmos = 1,
        Turf = 2,
        Wire = 3,
        Disposal = 4,
        PipeLeft = 5,
        PipeMiddle = 6,
        PipeRight = 7,
        HighWall = 8,
        LowWall = 9,
        FurnitureBase = 10,
        FurnitureTop = 11,
        Overlay = 12
    }

    public enum Direction
    {
        North = 0,
        NorthEast = 1,
        East = 2,
        SouthEast = 3,
        South = 4,
        SouthWest = 5,
        West = 6,
        NorthWest = 7,
    }

    public static class TileHelper
    {
        public static TileLayer[] GetTileLayers()
        {
            return (TileLayer[])Enum.GetValues(typeof(TileLayer));
        }

        public static int GetSubLayerSize(TileLayer layer)
        {
            switch(layer)
            {
                case TileLayer.HighWall:
                case TileLayer.LowWall:
                    return 4;
                case TileLayer.Overlay:
                    return 3;
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

        public static int GetRotationAngle(Direction dir)
        {
            switch (dir)
            {
                default:
                case Direction.South: return 0;
                case Direction.West: return 90;
                case Direction.North: return 180;
                case Direction.East: return 270;
            }
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
    }
}