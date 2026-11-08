using SS3D.Logging;
using SS3D.Core;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Class for checking invalid building combinations.
    /// </summary>
    public static class BuildChecker
    {
        private static readonly BuildFailReason[] EmptyFailures = System.Array.Empty<BuildFailReason>();

        /// <summary>
        /// Checks whether the tile object can be built at the given position.
        /// </summary>
        public static bool CanBuild(ITileLocation[] tileLocations, TileObjectSo tileObjectSo, Direction dir, Vector3 gridPosition,
            PlacedTileObject[] adjacentObjects, bool replaceExisting, TileMap map = null) =>
            Evaluate(tileLocations, tileObjectSo, dir, gridPosition, adjacentObjects, replaceExisting, map).Length == 0;

        /// <summary>
        /// Returns every failing rule for this placement (empty when valid).
        /// Order is stable and suitable for toasting the first entry as the primary reason.
        /// </summary>
        /// <param name="map">Map the cell belongs to, for the neighbour wall-mount rule. Null falls back to the current map.</param>
        public static BuildFailReason[] Evaluate(ITileLocation[] tileLocations, TileObjectSo tileObjectSo, Direction dir,
            Vector3 gridPosition, PlacedTileObject[] adjacentObjects, bool replaceExisting, TileMap map = null)
        {
            if (tileLocations == null || tileObjectSo == null)
                return EmptyFailures;

            var failures = new List<BuildFailReason>(4);
            TileLayer placedLayer = tileObjectSo.layer;

            if (!replaceExisting && !tileLocations[(int)placedLayer].IsEmpty(dir))
                failures.Add(BuildFailReason.LayerOccupied);

            if (placedLayer != TileLayer.Plenum)
            {
                if (tileLocations[(int)TileLayer.Plenum] is not SingleTileLocation plenumLocation)
                {
                    Log.Error(typeof(BuildChecker), "Location on Plenum should be a Single object location");
                    failures.Add(BuildFailReason.MissingOrInvalidPlenum);
                }
                else if (!CanBuildOnPlenum(plenumLocation))
                {
                    failures.Add(BuildFailReason.MissingOrInvalidPlenum);
                }
            }

            switch (placedLayer)
            {
                case TileLayer.WallMountHigh:
                case TileLayer.WallMountLow:
                {
                    if (tileObjectSo.isLarge)
                    {
                        if (!tileLocations[(int)placedLayer].IsEmpty(TileHelper.GetNextCardinalDir(dir)) ||
                            !tileLocations[(int)placedLayer].IsEmpty(TileHelper.GetPreviousCardinalDir(dir)))
                        {
                            failures.Add(BuildFailReason.LargeWallMountOverlap);
                        }
                    }

                    CollectWallAttachmentFailures(
                        (SingleTileLocation)tileLocations[(int)TileLayer.Turf],
                        tileObjectSo,
                        dir,
                        adjacentObjects,
                        failures);
                    break;
                }
                case TileLayer.FurnitureBase:
                case TileLayer.FurnitureTop:
                {
                    if (IsWall((SingleTileLocation)tileLocations[(int)TileLayer.Turf]))
                        failures.Add(BuildFailReason.NoFurnitureInWalls);
                    break;
                }
                case TileLayer.Turf when tileObjectSo.genericType == TileObjectGenericType.Wall:
                {
                    if (!tileLocations[(int)TileLayer.FurnitureBase].IsFullyEmpty() ||
                        !tileLocations[(int)TileLayer.FurnitureTop].IsFullyEmpty())
                    {
                        failures.Add(BuildFailReason.NoWallsOnFurniture);
                    }

                    if (!NoNeighbouringWallMount(map, gridPosition))
                        failures.Add(BuildFailReason.NeighbourWallMountBlocksWall);
                    break;
                }
            }

            return failures.Count == 0 ? EmptyFailures : failures.ToArray();
        }

        private static void CollectWallAttachmentFailures(SingleTileLocation wallLocation, TileObjectSo wallAttachment,
            Direction dir, PlacedTileObject[] adjacentObjects, List<BuildFailReason> failures)
        {
            if (!IsWall(wallLocation))
                failures.Add(BuildFailReason.WallMountNeedsWall);

            if (!wallLocation.IsEmpty(dir) &&
                wallLocation.PlacedObject.NameString.Contains("Window") &&
                wallAttachment.layer == TileLayer.WallMountLow)
            {
                failures.Add(BuildFailReason.NoLowMountOnWindow);
            }

            if (wallAttachment.layer is TileLayer.WallMountHigh or TileLayer.WallMountLow &&
                adjacentObjects != null &&
                adjacentObjects[(int)dir] &&
                adjacentObjects[(int)dir].GenericType == TileObjectGenericType.Wall)
            {
                failures.Add(BuildFailReason.WallMountBlockedByNeighbourWall);
            }
        }

        private static bool IsWall(SingleTileLocation wallLocation)
        {
            return !wallLocation.IsEmpty() && wallLocation.PlacedObject.GenericType == TileObjectGenericType.Wall;
        }

        /// <summary>
        /// Check that no wall mount on a side neighbour faces the tile at the grid position, since a wall
        /// there would collide with it. Mounts facing elsewhere and diagonal neighbours do not block.
        /// </summary>
        private static bool NoNeighbouringWallMount(TileMap map, Vector3 gridPosition)
        {
            if (map == null)
            {
                TileSubSystem tileSystem = SubSystems.Get<TileSubSystem>();
                map = tileSystem != null ? tileSystem.CurrentMap : null;
            }

            if (map == null)
                return true;

            PlacedTileObject[] neighboursHigh = map.GetNeighbourPlacedObjects(TileLayer.WallMountHigh, gridPosition);
            PlacedTileObject[] neighboursLow = map.GetNeighbourPlacedObjects(TileLayer.WallMountLow, gridPosition);
            return !neighboursHigh.Any(x => x != null) && !neighboursLow.Any(x => x != null);
        }

        private static bool CanBuildOnPlenum(SingleTileLocation plenumLocation)
        {
            if (plenumLocation.IsEmpty())
                return false;

            return plenumLocation.PlacedObject.NameString.Contains("Plenum") ||
                   plenumLocation.PlacedObject.name.Contains("Catwalk") ||
                   plenumLocation.PlacedObject.name.Contains("Lattice");
        }

        /// <summary>
        /// Returns a list of incompatible existing objects that should be removed if the provided tile object is removed.
        /// E.g. A wall mount should be removed if the wall is removed.
        /// </summary>
        public static List<ITileLocation> GetToBeClearedLocations(ITileLocation[] tileObjects)
        {
            List<ITileLocation> toBeDestroyedList = new List<ITileLocation>();

            if (tileObjects[(int)TileLayer.Plenum].IsFullyEmpty())
            {
                for (int i = 1; i < tileObjects.Length; i++)
                {
                    toBeDestroyedList.Add(tileObjects[i]);
                }
            }
            else if (tileObjects[(int)TileLayer.Turf].IsFullyEmpty())
            {
                toBeDestroyedList.Add(tileObjects[(int)TileLayer.WallMountHigh]);
                toBeDestroyedList.Add(tileObjects[(int)TileLayer.WallMountLow]);
            }
            else if (tileObjects[(int)TileLayer.FurnitureBase].IsFullyEmpty())
            {
                toBeDestroyedList.Add(tileObjects[(int)TileLayer.FurnitureTop]);
            }

            return toBeDestroyedList;
        }

        /// <summary>
        /// Returns the dependents a replace on <paramref name="replacedLayer"/> left invalid. A replace swaps the
        /// object without the clear cascade, so a wall swapped for a wall keeps its mounts; this catches the swaps
        /// that change what the cell can hold, such as a wall replaced by a floor or a window.
        /// </summary>
        public static List<ITileLocation> GetInvalidatedByReplace(ITileLocation[] tileObjects, TileLayer replacedLayer)
        {
            List<ITileLocation> invalid = GetToBeClearedLocations(tileObjects);

            switch (replacedLayer)
            {
                case TileLayer.Plenum when tileObjects[(int)TileLayer.Plenum] is SingleTileLocation plenum
                                           && !CanBuildOnPlenum(plenum):
                {
                    for (int i = 1; i < tileObjects.Length; i++)
                        invalid.Add(tileObjects[i]);
                    break;
                }
                case TileLayer.Turf when tileObjects[(int)TileLayer.Turf] is SingleTileLocation turf:
                {
                    if (!IsWall(turf))
                    {
                        invalid.Add(tileObjects[(int)TileLayer.WallMountHigh]);
                        invalid.Add(tileObjects[(int)TileLayer.WallMountLow]);
                    }
                    else if (turf.PlacedObject.NameString.Contains("Window"))
                    {
                        invalid.Add(tileObjects[(int)TileLayer.WallMountLow]);
                    }
                    break;
                }
            }

            return invalid;
        }
    }
}
