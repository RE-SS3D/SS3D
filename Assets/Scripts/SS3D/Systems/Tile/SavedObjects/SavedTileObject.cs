using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Save object used for reconstructing a TileObject.
    /// </summary>
    [Serializable]
    public class SavedTileObject
    {
        public TileLayer layer;
        public int x;
        public int y;
        public SavedPlacedTileObject placedSaveObject;
    }
}