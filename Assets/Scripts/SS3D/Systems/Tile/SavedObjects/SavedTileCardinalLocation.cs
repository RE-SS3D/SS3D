using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    [Serializable]
    public class SavedTileCardinalLocation : ISavedTileLocation
    {
        List<SavedPlacedTileObject> _placedSaveObjects = new List<SavedPlacedTileObject>();

        public SavedTileCardinalLocation(List<SavedPlacedTileObject> placedSaveObjects, Vector2Int location, TileLayer layer)
        {
            _placedSaveObjects = placedSaveObjects;
            Location = location;
            Layer = layer;
        }

        public Vector2Int Location
        {
            get;
            set;
        }

        public TileLayer Layer
        {
            get;
            set;
        }

        public List<SavedPlacedTileObject> GetPlacedObjects()
        {
            return _placedSaveObjects;
        }
    }
}
