using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Implementation of ISavedTileLocation for saving cardinal tile locations
    /// </summary>
    [Serializable]
    public class SavedTileCardinalLocation : ISavedTileLocation
    {
        [SerializeField]
        private List<SavedPlacedTileObject> _placedSaveObjects;

        [FormerlySerializedAs("x")]
        [SerializeField]
        private int _x;

        [FormerlySerializedAs("y")]
        [SerializeField]
        private int _y;

        public SavedTileCardinalLocation(List<SavedPlacedTileObject> placedSaveObjects, Vector2Int location, TileLayer layer)
        {
            _placedSaveObjects = placedSaveObjects;
            Location = location;
            Layer = layer;
        }

        public Vector2Int Location
        {
            get => new Vector2Int(_x, _y);
            set
            {
                _x = value.x;
                _y = value.y;
            }
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
