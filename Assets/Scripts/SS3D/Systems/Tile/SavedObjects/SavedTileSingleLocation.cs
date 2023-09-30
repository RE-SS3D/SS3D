using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Saved location used for reconstructing a Tile location and what it contains. 
    /// Implementation of ISavedTileLocation specifically for single object tile locations.
    /// </summary>
    [Serializable]
    public class SavedTileSingleLocation : ISavedTileLocation
    {

        [SerializeField]
        public SavedPlacedTileObject _placedSaveObject;

        [SerializeField]
        public int _x;

        [SerializeField]
        public int _y;

        public SavedTileSingleLocation(SavedPlacedTileObject placedSaveObject, Vector2Int location, TileLayer layer)
        {
            _placedSaveObject = placedSaveObject;
            Location = location;
            Layer = layer;
        }

        public Vector2Int Location
        {
            get => new Vector2Int(_x,_y);
            set { _x = value.x; _y = value.y; }
        }

        public TileLayer Layer
        {
            get;
            set;
        }

        public SavedPlacedTileObject GetPlacedObject(Direction dir = Direction.North)
        {
            return _placedSaveObject;
        }

        public List<SavedPlacedTileObject> GetPlacedObjects()
        {
            return new List<SavedPlacedTileObject>() { _placedSaveObject };
        }
    }
}