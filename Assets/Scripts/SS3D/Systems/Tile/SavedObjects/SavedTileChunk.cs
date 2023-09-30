using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// SaveObject used by chunks, containing their origin position, their key and a list of saved tiles in the chunk.
    /// </summary>
    [Serializable]
    public class SavedTileChunk
    {
        public Vector2Int chunkKey;
        public Vector3 originPosition;

        /// <summary>
        /// Super important to have it as a serialize reference as it allows for polymorphic serialization. 
        /// TODO : Check if a third party library would allow for polymorphic serialization without using references.
        /// It's not really bad, but the save files are bigger than what they could be.
        /// </summary>
        [SerializeReference]
        public ISavedTileLocation[] savedTiles;
    }
}