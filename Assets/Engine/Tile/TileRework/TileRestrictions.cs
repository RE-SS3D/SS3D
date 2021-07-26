﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
   

    /// <summary>
    /// Class used for setting certain restrictions when building objects on the tilemap. For example, most objects cannot be build if a plenum is missing.
    /// </summary>
    public static class TileRestrictions
    {
        /// <summary>
        /// Enum used for which restrictions should be applied to CanBuild.
        /// 
        /// - Everything:   Restrictions + tile occupancy are checked
        /// - Restrictions: Restrictions are checked
        /// - None:         Tile occupancy is checked
        /// </summary>
        public enum CheckRestrictions
        {
            Everything,
            None,
            OnlyRestrictions,
        }

        /// <summary>
        /// Main function for verifying if a tileObjectSO can be placed at a given location.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="position"></param>
        /// <param name="subLayerIndex"></param>
        /// <param name="tileObjectSO"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static bool CanBuild(TileMap map, Vector3 position, int subLayerIndex, TileObjectSO tileObjectSO, Direction dir)
        {
            TileManager tileManager = TileManager.Instance;
            TileLayer placedLayer = tileObjectSO.layerType;
            TileObject[] tileObjects = new TileObject[TileHelper.GetTileLayers().Length];

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                tileObjects[(int)layer] = map.GetTileObject(layer, position);
            }

            // Cannot build anything unless a plenum is placed
            if (placedLayer != TileLayer.Plenum && tileObjects[(int)TileLayer.Plenum].IsCompletelyEmpty())
                return false;

            // No wall mounts on non-walls
            if (tileObjects[(int)TileLayer.Turf].IsCompletelyEmpty() &&
                (placedLayer == TileLayer.LowWallMount || placedLayer == TileLayer.HighWallMount))
                return false;

            if (placedLayer == TileLayer.LowWallMount || placedLayer == TileLayer.HighWallMount)
                if (!CanBuildWallAttachment(map, position, tileObjectSO, dir))
                    return false;

            // No furniture inside walls
            if (placedLayer == TileLayer.FurnitureBase || placedLayer == TileLayer.FurnitureTop ||
                placedLayer == TileLayer.Overlay)
            {
                TileObject wallObject = map.GetTileObject(TileLayer.Turf, position);
                if (!wallObject.IsCompletelyEmpty() && wallObject.GetPlacedObject(0).GetGenericType().Contains("wall"))
                    return false;
            }

            // No walls on furniture
            if (placedLayer == TileLayer.Turf && tileObjectSO.genericType.Contains("wall") &&
                (!tileObjects[(int)TileLayer.FurnitureBase].IsCompletelyEmpty() ||
                 !tileObjects[(int)TileLayer.FurnitureTop].IsCompletelyEmpty() ||
                 !tileObjects[(int)TileLayer.Overlay].IsCompletelyEmpty()))
                return false;


            return true;
        }

        /// <summary>
        /// Checks whether a wall attachment can be placed and if it collides with a nearby wall.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="position"></param>
        /// <param name="wallAttachment"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static bool CanBuildWallAttachment(TileMap map, Vector3 position, TileObjectSO wallAttachment, Direction dir)
        {
            TileObject wallObject = map.GetTileObject(TileLayer.Turf, position);
            // Cannot build when there isn't a wall
            if (wallObject.IsCompletelyEmpty() || !wallObject.GetPlacedObject(0).GetGenericType().Contains("wall"))
                return false;

            // No low wall mounts on windows
            if (wallObject.GetPlacedObject(0).GetName().Contains("window") && wallAttachment.layerType == TileLayer.LowWallMount)
                return false;

            // Cannot build wall mount if it collides with the next wall
            PlacedTileObject[] adjacentObjects = map.GetNeighbourObjects(TileLayer.Turf, 0, position);
            if (adjacentObjects[(int)dir] && adjacentObjects[(int)dir].GetGenericType().Contains("wall"))
                return false;

            return true;
        }

        /// <summary>
        /// Returns a list of TileObjects that will be destroyed if an object on the given layer is destroyed.
        /// </summary>
        /// <param name="map"></param>
        /// <param name="layer"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        public static List<TileObject> GetToBeDestroyedObjects(TileMap map, TileLayer layer, Vector3 position)
        {
            List<TileObject> toBeDestroyedList = new List<TileObject>();

            // Remove everything when the plenum is missing
            if (layer == TileLayer.Plenum)
            {
                foreach (TileLayer layerToCheck in TileHelper.GetTileLayers())
                {
                    if (layerToCheck == TileLayer.Plenum)
                        continue;

                    toBeDestroyedList.Add(map.GetTileObject(layerToCheck, position));
                }
            }

            // Remove any wall fixtures when the turf is missing
            else if (layer == TileLayer.Turf)
            {
                toBeDestroyedList.Add(map.GetTileObject(TileLayer.HighWallMount, position));
                toBeDestroyedList.Add(map.GetTileObject(TileLayer.LowWallMount, position));
            }

            // Remove furniture top is furniture base is missing
            else if (layer == TileLayer.FurnitureBase)
                toBeDestroyedList.Add(map.GetTileObject(TileLayer.FurnitureTop, position));

            return toBeDestroyedList;
        }
    }
}