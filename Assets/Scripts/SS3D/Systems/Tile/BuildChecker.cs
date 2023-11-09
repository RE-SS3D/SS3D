using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Class for checking invalid building combinations.
    /// </summary>
    public static class BuildChecker
    {
        /// <summary>
        /// Checks whether the tile object can be build at the given position.
        /// </summary>
        /// <param name="tileObjects"></param>
        /// <param name="tileObjectSo"></param>
        /// <param name="replaceExisting"></param>
        /// <returns></returns>
        public static bool CanBuild(ITileLocation[] tileLocations, TileObjectSo tileObjectSo, Direction dir, PlacedTileObject[] adjacentObjects, bool replaceExisting)
        {
            bool canBuild = true;

            TileLayer placedLayer = tileObjectSo.layer;

            // Cannot build if the layer is already occupied. Skip if we replace the existing object
            if (!replaceExisting)
                canBuild &= tileLocations[(int)placedLayer].IsEmpty(dir);
            
            // Cannot build anything unless a plenum is placed
            if (placedLayer != TileLayer.Plenum)
            {
                if (!(tileLocations[(int)TileLayer.Plenum] is SingleTileLocation))
                {
                    Debug.LogError("Location on Plenum should be a Single object location");
                    return false;
                }

                canBuild &= CanBuildOnPlenum((SingleTileLocation) tileLocations[(int)TileLayer.Plenum]);
            }
                

            switch (placedLayer)
            {
                case TileLayer.WallMountHigh when canBuild:
                case TileLayer.WallMountLow when canBuild:
                {
                    canBuild &= CanBuildWallAttachment((SingleTileLocation) tileLocations[(int)TileLayer.Turf],
                        tileObjectSo, dir, adjacentObjects);
                    break;
                }
                // No furniture inside walls
                case TileLayer.FurnitureBase:
                case TileLayer.FurnitureTop:
                {
                    canBuild &= !IsWall((SingleTileLocation) tileLocations[(int)TileLayer.Turf]);
                    break;
                }
                // No walls on furniture
                case TileLayer.Turf when tileObjectSo.genericType == TileObjectGenericType.Wall:
                {
                    canBuild &= tileLocations[(int)TileLayer.FurnitureBase].IsFullyEmpty() &&
                    tileLocations[(int)TileLayer.FurnitureTop].IsFullyEmpty();
                    break;
                }
            }

            return canBuild;
        }

        /// <summary>
        /// Checks if a wall mount collides with a nearby wall
        /// </summary>
        /// <param name="tileObjectSo"></param>
        /// <param name="dir"></param>
        /// <param name="adjacentObjects"></param>
        /// <returns></returns>
        private static bool CanBuildWallCollision(TileObjectSo tileObjectSo, Direction dir, PlacedTileObject[] adjacentObjects)
        {
            bool canBuild = true;

            if (tileObjectSo.layer == TileLayer.WallMountHigh || tileObjectSo.layer == TileLayer.WallMountLow)
            {
                canBuild &= !(adjacentObjects[(int)dir] && adjacentObjects[(int)dir].GenericType == TileObjectGenericType.Wall);
            }

            return canBuild;
        }

        private static bool IsWall(SingleTileLocation wallLocation)
        {
            return !wallLocation.IsEmpty() && wallLocation.PlacedObject.GenericType == TileObjectGenericType.Wall;
        }

        private static bool CanBuildWallAttachment(SingleTileLocation wallLocation, TileObjectSo wallAttachment, Direction dir, PlacedTileObject[] adjacentObjects)
        {
            bool canBuild = true;

            // Cannot build when there isn't a wall
            canBuild &= IsWall(wallLocation);

            // No low wall mounts on windows
            if (!wallLocation.IsEmpty(dir))
            {
                canBuild &= !(wallLocation.PlacedObject.NameString.Contains("window") && wallAttachment.layer == TileLayer.WallMountLow);
            }

            // Mounts cannot collide with neighbouring wall
            canBuild &= CanBuildWallCollision(wallAttachment, dir, adjacentObjects);


            return canBuild;
        }

        private static bool CanBuildOnPlenum(SingleTileLocation plenumLocation)
        {
            bool canBuild = true;

            if (!plenumLocation.IsEmpty())
            {
                // Can only build on a Plenum and not Catwalks or Lattices
                canBuild &= plenumLocation.PlacedObject.NameString.Contains("plenum") || plenumLocation.PlacedObject.name.Contains("catwalk");
            }
            else
            {
                canBuild = false;
            }

            return canBuild;
        }

        /// <summary>
        /// Returns a list of incompatible existing objects that should be removed if the provided tile object is removed.
        /// E.g. A wall mount should be removed if the wall is removed.
        /// </summary>
        /// <param name="tileObjects"></param>
        /// <returns></returns>
        public static List<ITileLocation> GetToBeClearedLocations(ITileLocation[] tileObjects)
        {
            List<ITileLocation> toBeDestroyedList = new List<ITileLocation>();

            // Remove everything when the plenum is missing
            if (tileObjects[(int)TileLayer.Plenum].IsFullyEmpty())
            {
                for (int i = 1; i < tileObjects.Length; i++)
                {
                    toBeDestroyedList.Add(tileObjects[i]);
                }
            }

            // Remove any wall fixtures when the turf is missing
            else if (tileObjects[(int)TileLayer.Turf].IsFullyEmpty())
            {
                toBeDestroyedList.Add(tileObjects[(int)TileLayer.WallMountHigh]);
                toBeDestroyedList.Add(tileObjects[(int)TileLayer.WallMountLow]);
            }

            // Remove furniture top is furniture base is missing
            else if (tileObjects[(int)TileLayer.FurnitureBase].IsFullyEmpty())
                toBeDestroyedList.Add(tileObjects[(int)TileLayer.FurnitureTop]);

            return toBeDestroyedList;
        }
    }
}