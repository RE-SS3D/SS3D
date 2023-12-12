using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Represents a location in the tilemap, at a certain layer. 
    /// Can contain a single PlacedTileObject when a prefab is placed.
    /// </summary>
    public class SingleTileLocation : ITileLocation
    {
        private TileLayer _layer;
        private int _x;
        private int _y;

        public SingleTileLocation(TileLayer layer, int x, int y)
        {
            _layer = layer;
            _x = x;
            _y = y;
        }

        public PlacedTileObject PlacedObject { get; set; }

        public TileLayer Layer => _layer;

        public bool TryClearPlacedObject(Direction direction = Direction.North)
        {
            if (PlacedObject != null)
            {
                PlacedObject.DestroySelf();
                PlacedObject = null;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Saves this tileObject and includes the information from any PlacedTileObject.
        /// </summary>
        /// <returns></returns>
        public ISavedTileLocation Save()
        {
            var placedSaveObject = PlacedObject.Save();

            // If we have a multi tile object, save only the instance where the origin is
            if (PlacedObject.GridOffsetList.Count > 1 && placedSaveObject.origin != new Vector2Int(_x, _y))
            {
                return null;
            }

            return new SavedTileSingleLocation(placedSaveObject, new Vector2Int(_x, _y), _layer);
        }

        public bool TryGetPlacedObject(out PlacedTileObject placedObject, Direction direction = Direction.North)
        {
            if(PlacedObject != null)
            {
                placedObject = PlacedObject;
                return true;
            }
            placedObject = null;
            return false;
        }

        public bool IsEmpty(Direction direction = Direction.North)
        {
            return PlacedObject == null;
        }

        public void ClearAllPlacedObject()
        {
            TryClearPlacedObject();
        }

        public bool IsFullyEmpty()
        {
            return PlacedObject == null;
        }

        public void AddPlacedObject(PlacedTileObject tileObject, Direction direction = Direction.North)
        {
            PlacedObject = tileObject;
        }

        public List<PlacedTileObject> GetAllPlacedObject()
        {
            return PlacedObject != null ? 
                new List<PlacedTileObject> { PlacedObject } : new List<PlacedTileObject>();
        }
    }
}