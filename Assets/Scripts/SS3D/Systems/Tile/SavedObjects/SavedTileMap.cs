using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Save object used for reconstructing a tilemap.
    /// </summary>
    [Serializable]
    public class SavedTileMap
    {
        [FormerlySerializedAs("mapName")]
        [SerializeField]
        private string _mapName;

        [FormerlySerializedAs("savedChunkList")]
        [SerializeField]
        private SavedTileChunk[] _savedChunkList;

        [FormerlySerializedAs("savedItemList")]
        [SerializeField]
        private SavedPlacedItemObject[] _savedItemList;

        public SavedTileMap(string mapName, SavedTileChunk[] savedChunkList, SavedPlacedItemObject[] savedItemList)
        {
            _mapName = mapName;
            _savedItemList = savedItemList;
            _savedChunkList = savedChunkList;
        }

        public string MapName => _mapName;

        public SavedTileChunk[] SavedChunkList => _savedChunkList;

        public SavedPlacedItemObject[] SavedItemList => _savedItemList;
    }
}
