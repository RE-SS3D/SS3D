﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    public static class TileRestrictions
    {
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
                (placedLayer == TileLayer.LowWall || placedLayer == TileLayer.HighWall))
                return false;

            if (placedLayer == TileLayer.LowWall || placedLayer == TileLayer.HighWall)
                if (!CanBuildWallAttachment(map, position, tileObjectSO, dir))
                    return false;

            return true;
        }

        private static bool CanBuildWallAttachment(TileMap map, Vector3 position, TileObjectSO wallAttachment, Direction dir)
        {
            TileObject wallObject = map.GetTileObject(TileLayer.Turf, position);
            // Cannot build when there isn't a wall
            if (wallObject.IsCompletelyEmpty() || !wallObject.GetPlacedObject(0).GetGenericType().Contains("wall"))
                return false;

            // No low wall mounts on windows
            if (wallObject.GetPlacedObject(0).GetName().Contains("window") && wallAttachment.layerType == TileLayer.LowWall)
                return false;

            // Cannot build wall mount if it collides with the next wall
            PlacedTileObject[] adjacentObjects = map.GetNeighbourObjects(TileLayer.Turf, 0, position);
            if (adjacentObjects[(int)dir] && adjacentObjects[(int)dir].GetGenericType().Contains("wall"))
                return false;

            return true;
        }
    }
}