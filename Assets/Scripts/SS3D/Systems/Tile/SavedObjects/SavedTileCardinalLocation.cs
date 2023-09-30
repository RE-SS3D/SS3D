using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Implementation of ISavedTileLocation for saving cardinal tile locations.
    /// </summary>
    [Serializable]
    public class SavedTileCardinalLocation : ISavedTileLocation
    {
        [SerializeField]
        private List<SavedPlacedTileObject> _placedSaveObjects = new List<SavedPlacedTileObject>();

        [SerializeField]
        private int x;

        [SerializeField]
        private int y;

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
    }
}
