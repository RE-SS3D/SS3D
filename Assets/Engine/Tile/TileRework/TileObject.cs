﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public class TileObject
    {
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

        public void SetPlacedObject(PlacedTileObject placedObject, int subLayerIndex)
        {
            placedObjects[subLayerIndex] = placedObject;
            map.TriggerGridObjectChanged(x, y);
        }

        public void ClearPlacedObject(int subLayerIndex)
        {
            placedObjects[subLayerIndex]?.DestroySelf();
            placedObjects[subLayerIndex] = null;
            map.TriggerGridObjectChanged(x, y);
        }

        public PlacedTileObject GetPlacedObject(int subLayerIndex)
        {
            return placedObjects[subLayerIndex];
        }

        public bool IsEmpty(int subLayerIndex)
        {
            return placedObjects[subLayerIndex] == null;
        }

        public bool IsCompletelyEmpty()
        {
            bool occupied = false;
            for (int i = 0; i < TileHelper.GetSubLayerSize(layer); i++)
            {
                occupied |= !IsEmpty(i);
            }

            return !occupied;
        }

        public TileSaveObject Save()
        {
            List<PlacedTileObject.PlacedSaveObject> placedSaveObjects = new List<PlacedTileObject.PlacedSaveObject>();
            for (int i = 0; i < placedObjects.Length; i++)
            {
                if (!IsEmpty(i))
                    placedSaveObjects.Add(placedObjects[i].Save());
            }

            return new TileSaveObject
            {
                layer = layer,
                x = x,
                y = y,
                placedSaveObjects = placedSaveObjects.ToArray(),
            };
        }
    }
}