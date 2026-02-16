using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using UnityEngine;
#if UNITY_EDITOR
using NaughtyAttributes;
#endif

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Generic scriptableobject that defines common attributes for tiles and items.
    /// </summary>
    public partial class GenericObjectSo : ScriptableObject
    {
#if UNITY_EDITOR
        [OnValueChanged(nameof(OnReferenceChanged))]
#endif
        public ObjectAssetReference PrefabAsset;

        public Sprite icon;

        [CanBeNull]
        public string NameString => PrefabAsset?.name;
    }
}