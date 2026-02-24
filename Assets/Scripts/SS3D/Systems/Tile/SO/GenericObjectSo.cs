using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Generic scriptableobject that defines common attributes for tiles and items.
    /// </summary>
    public class GenericObjectSo : ScriptableObject
    {
        [NotNull]
        public string NameString => PrefabAsset.name;

        public ObjectAssetReference PrefabAsset;
        public Sprite icon;
    }
}