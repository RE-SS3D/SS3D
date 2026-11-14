using SS3D.Data.Persistence;
using System;
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

        /// <summary>
        /// The object's authored setup (device settings, locks, container contents) where it differs from the prefab
        /// default; empty for an object nobody configured, and in saves from before schema 3.
        /// </summary>
        public SavedSetupEntry[] setup;
    }
}