using System;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Why a tile placement failed <see cref="BuildChecker"/> rules.
    /// </summary>
    public enum BuildFailReason
    {
        LayerOccupied,
        MissingOrInvalidPlenum,
        WallMountNeedsWall,
        WallMountBlockedByNeighbourWall,
        LargeWallMountOverlap,
        NoLowMountOnWindow,
        NoFurnitureInWalls,
        NoWallsOnFurniture,
        NeighbourWallMountBlocksWall,
        BodyInTheWay,
    }

    /// <summary>
    /// Human-readable messages for map editor toasts / hints.
    /// </summary>
    public static class BuildFailMessages
    {
        public static string Format(BuildFailReason reason) =>
            reason switch
            {
                BuildFailReason.LayerOccupied => "That layer is already occupied — hold Alt to replace",
                BuildFailReason.MissingOrInvalidPlenum => "Needs a plenum or catwalk underneath",
                BuildFailReason.WallMountNeedsWall => "Wall mounts need a wall",
                BuildFailReason.WallMountBlockedByNeighbourWall => "Blocked by a neighbouring wall",
                BuildFailReason.LargeWallMountOverlap => "Overlaps another large wall mount",
                BuildFailReason.NoLowMountOnWindow => "Low mounts cannot go on windows",
                BuildFailReason.NoFurnitureInWalls => "Cannot place furniture inside a wall",
                BuildFailReason.NoWallsOnFurniture => "Cannot place a wall on furniture",
                BuildFailReason.NeighbourWallMountBlocksWall => "A neighbouring wall mount blocks this wall",
                BuildFailReason.BodyInTheWay => "Someone is standing there",
                _ => "Cannot place here",
            };

        public static string FormatPrimary(BuildFailReason[] failures)
        {
            if (failures == null || failures.Length == 0)
                return string.Empty;

            return Format(failures[0]);
        }
    }
}
