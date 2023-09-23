using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Save object used for reconstructing a tilemap.
    /// </summary>
    [Serializable]
    public class SavedTileMap
    {
        public string MapName;
        public SavedTileChunk[] SavedChunkList;
        public SavedPlacedItemObject[] SavedItemList;
    }
}