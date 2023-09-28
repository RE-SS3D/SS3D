using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// SaveObject used by chunks.
    /// </summary>
    [Serializable]
    public class SavedTileChunk
    {
        public Vector2Int chunkKey;
        public Vector3 originPosition;
        public ISavedTileLocation[] tileObjectSaveObjectArray;
    }
}