using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Saved location used for reconstructing a Tile location and what it contains.
    /// </summary>
    [Serializable]
    public class SavedTileSingleLocation : ISavedTileLocation
    {
        
        public SavedPlacedTileObject placedSaveObject;

        public SavedTileSingleLocation(SavedPlacedTileObject placedSaveObject, Vector2Int location, TileLayer layer)
        {
            this.placedSaveObject = placedSaveObject;
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

        public SavedPlacedTileObject GetPlacedObject(Direction dir = Direction.North)
        {
            return placedSaveObject;
        }

        public List<SavedPlacedTileObject> GetPlacedObjects()
        {
            return new List<SavedPlacedTileObject>() { placedSaveObject };
        }
    }
}