using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    [Serializable]
    public class SavedTileCardinalLocation : ISavedTileLocation
    {
        [SerializeField]
        List<SavedPlacedTileObject> _placedSaveObjects = new List<SavedPlacedTileObject>();

        [SerializeField]
        public int x;

        [SerializeField]
        public int y;

        public SavedTileCardinalLocation(List<SavedPlacedTileObject> placedSaveObjects, Vector2Int location, TileLayer layer)
        {
            _placedSaveObjects = placedSaveObjects;
            Location = location;
            Layer = layer;
        }


        public List<SavedPlacedTileObject> GetPlacedObjects()
        {
            return _placedSaveObjects;
        }

        public Vector2Int Location
        {
            get => new Vector2Int(x, y);
            set { x = value.x; y = value.y; }
        }

        public TileLayer Layer
        {
            get;
            set;
        }
    }
}
