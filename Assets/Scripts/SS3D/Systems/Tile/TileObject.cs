using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Represents a single grid in the tilemap. Can contain PlacedTileObject when a prefab is placed.
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
            public PlacedTileObject.PlacedSaveObject placedSaveObject;
        }

        private TileLayer _layer;
        private int _x;
        private int _y;
        private PlacedTileObject _placedObject;

        public TileObject(TileLayer layer, int x, int y)
        {
            _layer = layer;
            _x = x;
            _y = y;
        }

        public PlacedTileObject GetPlacedObject()
        {
            return _placedObject;
        }

        public void SetPlacedObject(PlacedTileObject placedObject)
        {
            _placedObject = placedObject;
        }

        public void ClearPlacedObject()
        {
            if (_placedObject != null)
            {
                _placedObject.DestroySelf();
            }
        }

        public bool IsEmpty()
        {
            return _placedObject == null;
        }

        /// <summary>
        /// Saves this tileObject and includes the information from any PlacedTileObject.
        /// </summary>
        /// <returns></returns>
        public TileSaveObject Save()
        {
            var placedSaveObject = _placedObject.Save();

            // If we have a multi tile object, save only the instance where the origin is
            if (_placedObject.GetGridOffsetList().Count > 1 && placedSaveObject.origin != new Vector2Int(_x, _y))
            {
                return null;
            }

            return new TileSaveObject
            {
                layer = _layer,
                x = _x,
                y = _y,
                placedSaveObject = placedSaveObject,
            };
        }
    }
}