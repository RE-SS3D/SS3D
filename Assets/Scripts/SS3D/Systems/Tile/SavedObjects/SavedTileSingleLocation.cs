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

        [SerializeField]
        public SavedPlacedTileObject placedSaveObject;

        [SerializeField]
        public int x;

        [SerializeField]
        public int y;

        public SavedTileSingleLocation(SavedPlacedTileObject placedSaveObject, Vector2Int location, TileLayer layer)
        {
            this.placedSaveObject = placedSaveObject;
            Location = location;
            Layer = layer;
        }

        public Vector2Int Location
        {
            get => new Vector2Int(x,y);
            set { x = value.x; y = value.y; }
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