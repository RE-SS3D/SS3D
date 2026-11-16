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
        public string mapName;
        public bool hasAuthoredAreas;
        public SavedTileChunk[] savedChunkList;
        public SavedPlacedItemObject[] savedItemList;
        public SavedAreaRecord[] savedAreas;
    }
}