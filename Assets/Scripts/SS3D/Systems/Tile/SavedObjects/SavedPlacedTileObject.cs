using SS3D.Systems.Tile.Enums;
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
        public string TileObjectSOName;
        public Vector2Int Origin;
        public Direction Dir;
    }
}