using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// SaveObject used by chunks, containing their origin position, their key and a list of saved tiles in the chunk.
    /// </summary>
    [Serializable]
    public class SavedTileChunk
    {
        [FormerlySerializedAs("chunkKey")]
        [SerializeField]
        private Vector2Int _chunkKey;

        [FormerlySerializedAs("originPosition")]
        [SerializeField]
        private Vector3 _originPosition;

        /// <summary>
        /// Super important to have it as a serialize reference as it allows for polymorphic serialization.
        /// TODO : Check if a third party library would allow for polymorphic serialization without using references.
        /// It's not really bad, but the save files are bigger than what they could be.
        /// </summary>
        [SerializeReference]
        private ISavedTileLocation[] _savedTiles;

        public SavedTileChunk(ISavedTileLocation[] savedTiles, Vector2Int chunkKey, Vector3 originPosition)
        {
            _savedTiles = savedTiles;
            _chunkKey = chunkKey;
            _originPosition = originPosition;
        }

        public Vector2Int ChunkKey => _chunkKey;

        public Vector3 OriginPosition => _originPosition;

        public ISavedTileLocation[] SavedTiles => _savedTiles;
    }
}
