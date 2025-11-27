using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// SaveObject that contains all information required to reconstruct a placed tile object.
    /// </summary>
    [Serializable]
    public struct SavedPlacedTileObject
    {
        [FormerlySerializedAs("tileObjectSOName")]
        public string TileObjectSoName;

        [FormerlySerializedAs("origin")]
        public Vector2Int Origin;

        [FormerlySerializedAs("dir")]
        public Direction Dir;
    }
}
