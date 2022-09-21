using System;
using System.Collections;
using System.Collections.Generic;
using SS3D.Engine.Tile.TileRework;
using UnityEngine;

namespace SS3D.Engine.Tiles
{
    /// <summary>
    /// Class for the base TileObject that is used by the TileMap.
    /// </summary>
    public class TileObject
    {
        /// <summary>
        /// Save object used for reconstructing a TileObject.
        /// </summary>
        [Serializable]
        public class TileSaveObject
        {
            public TileLayer layer;
            public int x;
            public int y;
            public PlacedTileObject.PlacedSaveObject[] placedSaveObjects;
        }

        private TileChunk map;
        private TileLayer layer;
        private int x;
        private int y;
        public PlacedTileObject[] placedObjects;

        public TileObject(TileChunk map, TileLayer layer, int x, int y, int subLayerSize)
        {
            this.map = map;
            this.layer = layer;
            this.x = x;
            this.y = y;
            placedObjects = new PlacedTileObject[subLayerSize];
        }

        /// <summary>
        /// Sets a PlacedObject on the TileObject.
        /// </summary>
        /// <param name="placedObject"></param>
        /// <param name="subLayerIndex">Which sublayer to place the object</param>
        public void SetPlacedObject(PlacedTileObject placedObject, int subLayerIndex)
        {
            placedObjects[subLayerIndex] = placedObject;
            map.TriggerGridObjectChanged(x, y);
        }

        /// <summary>
        /// Clears a PlacedObject.
        /// </summary>
        /// <param name="subLayerIndex">Which sublayer to place the object</param>
        public void ClearPlacedObject(int subLayerIndex)
        {
            if (placedObjects[subLayerIndex] != null)
                placedObjects[subLayerIndex].DestroySelf();

            placedObjects[subLayerIndex] = null;
            map.TriggerGridObjectChanged(x, y);
        }

        /// <summary>
        /// Clears the PlacedObject for all sublayers.
        /// </summary>
        public void ClearAllPlacedObjects()
        {
            foreach (PlacedTileObject placedObject in placedObjects)
                placedObject?.DestroySelf();

            map.TriggerGridObjectChanged(x, y);
        }

        /// <summary>
        /// Returns the PlacedObject for a given sub layer.
        /// </summary>
        /// <param name="subLayerIndex"></param>
        /// <returns></returns>
        public PlacedTileObject GetPlacedObject(int subLayerIndex)
        {
            return placedObjects[subLayerIndex];
        }

        /// <summary>
        /// Returns an array of all PlacedObjects.
        /// </summary>
        /// <returns></returns>
        public PlacedTileObject[] GetAllPlacedObjects()
        {
            return placedObjects;
        }

        /// <summary>
        /// Returns if a given sub layer does not contain a PlacedObject.
        /// </summary>
        /// <param name="subLayerIndex"></param>
        /// <returns></returns>
        public bool IsEmpty(int subLayerIndex)
        {
            return placedObjects[subLayerIndex] == null;
        }

        /// <summary>
        /// Returns if all sub layers do not contain a PlacedObject.
        /// </summary>
        /// <returns></returns>
        public bool IsCompletelyEmpty()
        {
            bool occupied = false;
            for (int i = 0; i < TileHelper.GetSubLayerSize(layer); i++)
            {
                occupied |= !IsEmpty(i);
            }

            return !occupied;
        }

        /// <summary>
        /// Saves this tileObject and includes the information from any PlacedTileObject.
        /// </summary>
        /// <returns></returns>
        public TileSaveObject Save()
        {
            PlacedTileObject.PlacedSaveObject[] placedSaveObjects = new PlacedTileObject.PlacedSaveObject[placedObjects.Length];
            for (int i = 0; i < placedObjects.Length; i++)
            {
                // If we have a multi tile object, save only the instance where the origin is
                if (placedObjects[i]?.GetGridPositionList().Count > 1)
                {
                    if (placedObjects[i].Save().origin != new Vector2Int(x, y))
                        continue;
                }

                placedSaveObjects[i] = placedObjects[i]?.Save();
            }

            return new TileSaveObject
            {
                layer = layer,
                x = x,
                y = y,
                placedSaveObjects = placedSaveObjects,
            };
        }
    }
}