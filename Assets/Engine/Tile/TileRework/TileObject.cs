using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Engine.TilesRework
{
    public class TileObject
    {
        [Serializable]
        public class SaveObject
        {
            public TileLayerType layer;
            public int x;
            public int y;
            public PlacedTileObject.SaveObject placedSaveObject;
        }

        private TileMap map;
        private TileLayerType layer;
        private int x;
        private int y;
        public PlacedTileObject placedObject;

        public TileObject(TileMap map, TileLayerType layer, int x, int y)
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

        public SaveObject Save()
        {
            return new SaveObject
            {
                layer = layer,
                x = x,
                y = y,
                placedSaveObject = placedObject.Save(),
            };
        }
    }
}