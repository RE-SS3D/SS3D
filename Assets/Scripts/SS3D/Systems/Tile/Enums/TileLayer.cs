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
        Plenum = 0,
        Turf = 1,
        Wire = 2,
        Disposal = 3,
        PipeSurface = 4,
        WallMountHigh = 5,
        WallMountLow = 6,
        FurnitureBase = 7,
        FurnitureTop = 8,
        Overlays = 9,
        PipeMiddle = 10,
        PipeLeft = 11,
        PipeRight = 12,
    }
}
