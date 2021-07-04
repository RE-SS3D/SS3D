using System;
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
            public PlacedTileObject.PlacedSaveObject placedSaveObject;
        }

        private TileChunk map;
        private TileLayer layer;
        private int x;
        private int y;
        public PlacedTileObject placedObject;

        public TileObject(TileChunk map, TileLayer layer, int x, int y)
        {
            this.map = map;
            this.layer = layer;
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return x + ", " + y + "\n" + placedObject;
        }

        public void SetPlacedObject(PlacedTileObject placedObject)
        {
            this.placedObject = placedObject;
            map.TriggerGridObjectChanged(x, y);
        }

        public void ClearPlacedObject()
        {
            placedObject?.DestroySelf();
            placedObject = null;
            map.TriggerGridObjectChanged(x, y);
        }

        public PlacedTileObject GetPlacedObject()
        {
            return placedObject;
        }

        public bool IsEmpty()
        {
            return placedObject == null;
        }

        public TileSaveObject Save()
        {
            return new TileSaveObject
            {
                layer = layer,
                x = x,
                y = y,
                placedSaveObject = placedObject.Save(),
            };
        }
    }
}