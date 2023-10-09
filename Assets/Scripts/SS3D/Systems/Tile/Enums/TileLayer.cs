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
        PipeSurface,
        WallMountHigh,
        WallMountLow,
        FurnitureBase,
        FurnitureTop,
        Overlays,
        PipeMiddle,
        PipeLeft,
        PipeRight,
    }
}