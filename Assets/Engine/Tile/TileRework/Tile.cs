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
    }

}