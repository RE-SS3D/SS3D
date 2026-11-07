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
        /// <summary>
        /// The prefab's <see cref="SS3D.Data.AssetDatabases.ObjectAssetReference.Id"/> (its asset GUID), which a rename
        /// keeps. Loading tries it first. Empty in saves from before schema 2.
        /// </summary>
        public string id;

        /// <summary>
        /// The asset name, kept for readable JSON and as the fallback for saves without an <see cref="id"/>.
        /// </summary>
        public string tileObjectSOName;
        public Vector2Int origin;
        public Direction dir;
    }
}