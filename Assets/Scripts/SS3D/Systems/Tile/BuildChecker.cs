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
        public static bool CanBuild(TileObject[] tileObjects, TileObjectSo tileObjectSo, bool replaceExisting)
        {
            bool canBuild = true;

            TileLayer placedLayer = tileObjectSo.layer;

            // Cannot build if the layer is already occupied. Skip if we replace the existing object
            if (!replaceExisting)
                canBuild &= tileObjects[(int)placedLayer].IsEmpty;

            // Cannot build anything unless a plenum is placed
            if (placedLayer != TileLayer.Plenum)
                canBuild &= CanBuildOnPlenum(tileObjects[(int)TileLayer.Plenum], tileObjectSo);


            switch (placedLayer)
            {
                case TileLayer.WallMountHigh when canBuild:
                case TileLayer.WallMountLow when canBuild:
                    {
                        canBuild &= CanBuildWallAttachment(tileObjects[(int)TileLayer.Turf], tileObjectSo);
                        break;
                    }
                // No furniture inside walls
                case TileLayer.FurnitureBase:
                case TileLayer.FurnitureTop:
                    {
                        canBuild &= !IsWall(tileObjects[(int)TileLayer.Turf]);
                        break;
                    }
                // No walls on furniture
                case TileLayer.Turf when tileObjectSo.genericType == TileObjectGenericType.Wall:
                    {
                        canBuild &= tileObjects[(int)TileLayer.FurnitureBase].IsEmpty&&
                        tileObjects[(int)TileLayer.FurnitureTop].IsEmpty;
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
        public static bool CanBuildWallCollision(TileObjectSo tileObjectSo, Direction dir, PlacedTileObject[] adjacentObjects)
        {
            bool canBuild = true;

            if (tileObjectSo.layer == TileLayer.WallMountHigh || tileObjectSo.layer == TileLayer.WallMountLow)
            {
                canBuild &= !(adjacentObjects[(int)dir] && adjacentObjects[(int)dir].GenericType == TileObjectGenericType.Wall);
            }

            return canBuild;
        }

        private static bool IsWall(TileObject wallObject)
        {
            return !wallObject.IsEmpty&& wallObject.PlacedObject.GenericType == TileObjectGenericType.Wall;
        }

        private static bool CanBuildWallAttachment(TileObject wallObject, TileObjectSo wallAttachment)
        {
            bool canBuild = true;

            // Cannot build when there isn't a wall
            canBuild &= IsWall(wallObject);

            // No low wall mounts on windows
            if (!wallObject.IsEmpty)
                canBuild &= !(wallObject.PlacedObject.NameString.Contains("window") && wallAttachment.layer == TileLayer.WallMountLow);

            return canBuild;
        }

        private static bool CanBuildOnPlenum(TileObject plenumObject, TileObjectSo plenumAttachment)
        {
            bool canBuild = true;

            if (!plenumObject.IsEmpty)
            {
                // Can only build on a Plenum and not Catwalks or Lattices
                canBuild &= plenumObject.PlacedObject.NameString.Contains("plenum") || plenumObject.PlacedObject.name.Contains("catwalk");
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
        public static List<TileObject> GetToBeDestroyedObjects(TileObject[] tileObjects)
        {
            List<TileObject> toBeDestroyedList = new List<TileObject>();

            // Remove everything when the plenum is missing
            if (tileObjects[(int)TileLayer.Plenum].IsEmpty)
            {
                for (int i = 1; i < tileObjects.Length; i++)
                {
                    toBeDestroyedList.Add(tileObjects[i]);
                }
            }

            // Remove any wall fixtures when the turf is missing
            else if (tileObjects[(int)TileLayer.Turf].IsEmpty)
            {
                toBeDestroyedList.Add(tileObjects[(int)TileLayer.WallMountHigh]);
                toBeDestroyedList.Add(tileObjects[(int)TileLayer.WallMountLow]);
            }

            // Remove furniture top is furniture base is missing
            else if (tileObjects[(int)TileLayer.FurnitureBase].IsEmpty)
                toBeDestroyedList.Add(tileObjects[(int)TileLayer.FurnitureTop]);

            return toBeDestroyedList;
        }
    }
}