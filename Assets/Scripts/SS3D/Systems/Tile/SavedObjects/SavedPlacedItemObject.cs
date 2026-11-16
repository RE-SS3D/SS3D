using SS3D.Data.Persistence;
using System;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// SaveObject that contains all information required to reconstruct a placed item object.
    /// </summary>
    [Serializable]
    public struct SavedPlacedItemObject
    {
        /// <summary>
        /// The prefab's <see cref="SS3D.Data.AssetDatabases.ObjectAssetReference.Id"/> (its asset GUID), which a rename
        /// keeps. Loading tries it first. Empty in saves from before schema 2.
        /// </summary>
        public string id;

        /// <summary>
        /// The asset name, kept for readable JSON and as the fallback for saves without an <see cref="id"/>.
        /// </summary>
        public string itemName;
        public Vector3 worldPosition;
        public Quaternion rotation;

        /// <summary>
        /// The item's authored setup (a bag's contents) where it differs from the prefab default; empty for an item
        /// nobody configured, and in saves from before schema 3.
        /// </summary>
        public SavedSetupEntry[] setup;
    }
}