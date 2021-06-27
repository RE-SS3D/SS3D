using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public enum TileLayerType
    {
        Plenum,
        Atmos,
        Turf,
        Wire,
        Disposal,
        Pipes,
        HighWall,
        LowWall,
        FurnitureBase,
        FurnitureTop,
        Overlays
    }

    public enum Direction
    {
        North,
        NorthEast,
        East,
        SouthEast,
        South,
        SouthWest,
        West,
        NorthWest,
    }

    public static class TileHelper
    {
        public static TileLayerType[] GetTileLayers()
        {
            return (TileLayerType[])Enum.GetValues(typeof(TileLayerType));
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
    }

}