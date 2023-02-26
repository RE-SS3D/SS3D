using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// SaveObject that contains all information required to reconstruct a placed tile object.
    /// </summary>
    [Serializable]
    public struct SavedPlacedTileObject
    {
        public string tileObjectSOName;
        public Vector2Int origin;
        public Direction dir;
    }
}