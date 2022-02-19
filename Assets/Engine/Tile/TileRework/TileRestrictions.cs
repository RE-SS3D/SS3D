using System.Collections.Generic;
using SS3D.Engine.Tiles;
using UnityEngine;

namespace SS3D.Engine.Tile.TileRework
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
        public static bool CanBuild(TileMap map, Vector3 position, int subLayerIndex, TileObjectSo tileObjectSO, Direction dir)
        {
            TileManager tileManager = TileManager.Instance;
            TileLayer placedLayer = tileObjectSO.layer;
            TileObject[] tileObjects = new TileObject[TileHelper.GetTileLayers().Length];

            foreach (TileLayer layer in TileHelper.GetTileLayers())
            {
                tileObjects[(int)layer] = map.GetTileObject(layer, position);
            }

            // Cannot build anything unless a plenum is placed
            if (placedLayer != TileLayer.Plenum && !CanBuildOnPlenum(map, position, tileObjectSO, dir))
                return false;

            // No wall mounts on non-walls
            if (tileObjects[(int)TileLayer.Turf].IsCompletelyEmpty() &&
                (placedLayer == TileLayer.LowWallMount || placedLayer == TileLayer.HighWallMount))
                return false;

            switch (placedLayer)
            {
                case TileLayer.LowWallMount:
                case TileLayer.HighWallMount:
                {
                    if (!CanBuildWallAttachment(map, position, tileObjectSO, dir))
                    {
                        return false;
                    }
                    break;
                }
                // No furniture inside walls
                case TileLayer.FurnitureBase:
                case TileLayer.FurnitureTop:
                case TileLayer.Overlay:
                {
                    TileObject wallObject = map.GetTileObject(TileLayer.Turf, position);
                    if (!wallObject.IsCompletelyEmpty() && wallObject.GetPlacedObject(0).GetGenericType() == TileObjectGenericType.Wall)
                        return false;
                    break;
                }
                // No walls on furniture
                case TileLayer.Turf when tileObjectSO.genericType == TileObjectGenericType.Wall && (!tileObjects[(int)TileLayer.FurnitureBase].IsCompletelyEmpty() ||
                    !tileObjects[(int)TileLayer.FurnitureTop].IsCompletelyEmpty() ||
                    !tileObjects[(int)TileLayer.Overlay].IsCompletelyEmpty()):
                    return false;
            }


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
        private static bool CanBuildWallAttachment(TileMap map, Vector3 position, TileObjectSo wallAttachment, Direction dir)
        {
            TileObject wallObject = map.GetTileObject(TileLayer.Turf, position);
            // Cannot build when there isn't a wall
            if (wallObject.IsCompletelyEmpty() || wallObject.GetPlacedObject(0).GetGenericType() != TileObjectGenericType.Wall)
                return false;

            // No low wall mounts on windows
            if (wallObject.GetPlacedObject(0).GetName().Contains("window") && wallAttachment.layer == TileLayer.LowWallMount)
                return false;

            // Cannot build wall mount if it collides with the next wall
            PlacedTileObject[] adjacentObjects = map.GetNeighbourObjects(TileLayer.Turf, 0, position);
            if (adjacentObjects[(int)dir] && adjacentObjects[(int)dir].GetGenericType() == TileObjectGenericType.Wall)
                return false;

            return true;
        }

        /// <summary>
        /// Checks whether an object can be build on top of a certain plenum
        /// </summary>
        /// <param name="map"></param>
        /// <param name="position"></param>
        /// <param name="plenumAttachment"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        private static bool CanBuildOnPlenum(TileMap map, Vector3 position, TileObjectSo plenumAttachment, Direction dir)
        {
            TileObject plenumObject = map.GetTileObject(TileLayer.Plenum, position);

            // No plenum means we cannot build anything on top
            if (plenumObject.IsCompletelyEmpty())
                return false;

            // Only allow wires and machines on catwalks
            if (plenumObject.GetPlacedObject(0).name.Contains("Catwalk") && (plenumAttachment.layer != TileLayer.Wire &&
                plenumAttachment.layer != TileLayer.FurnitureBase))
                return false;

            // Can only build on a Plenum and not Catwalks or Lattices
            if (!plenumObject.GetPlacedObject(0).name.Contains("Plenum") && !plenumObject.GetPlacedObject(0).name.Contains("Catwalk"))
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