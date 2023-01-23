using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Enum that defines every layer that should be present on each tile.
    /// </summary>
    public enum TileLayer
    {
        Plenum,
        Turf,
        Wire,
        Disposal,
        Pipes,
        WallMountHigh,
        WallMountLow,
        FurnitureBase,
        FurnitureTop,
    }

    /// <summary>
    /// Enum for each direction.
    /// </summary>
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

        public static TileLayer[] GetTileLayerNames()
        {
            if (tileLayers == null)
            {
                tileLayers = (TileLayer[])Enum.GetValues(typeof(TileLayer));
            }
            return tileLayers;
        }
    }
}