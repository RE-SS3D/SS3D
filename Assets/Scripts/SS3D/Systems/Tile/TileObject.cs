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
        private TileLayer _layer;
        private int _x;
        private int _y;

        public TileObject(TileLayer layer, int x, int y)
        {
            _layer = layer;
            _x = x;
            _y = y;
        }

        public PlacedTileObject PlacedObject { get; set; }

        public TileLayer Layer => _layer;

        public bool IsEmpty => PlacedObject == null;

        public void ClearPlacedObject()
        {
            if (PlacedObject != null)
            {
                PlacedObject.DestroySelf();
                PlacedObject = null;
            }
        }

        /// <summary>
        /// Saves this tileObject and includes the information from any PlacedTileObject.
        /// </summary>
        /// <returns></returns>
        public SavedTileObject Save()
        {
            var placedSaveObject = PlacedObject.Save();

            // If we have a multi tile object, save only the instance where the origin is
            if (PlacedObject.GridOffsetList.Count > 1 && placedSaveObject.origin != new Vector2Int(_x, _y))
            {
                return null;
            }

            return new SavedTileObject
            {
                layer = _layer,
                x = _x,
                y = _y,
                placedSaveObject = placedSaveObject,
            };
        }
    }
}