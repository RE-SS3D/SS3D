using System;
using JetBrains.Annotations;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// How a saved map finds the asset behind a placed tile object or item. Saves store the prefab's
    /// <see cref="SS3D.Data.AssetDatabases.ObjectAssetReference.Id"/> (its asset GUID, which a rename keeps) and the name.
    /// Loading tries the id first, then the name through <see cref="RenamedTileObjects"/>, which covers saves from before
    /// ids and an asset deleted and recreated under its old name.
    /// </summary>
    public static class SavedAssetLookup
    {
        [CanBeNull]
        public static string IdOf([CanBeNull] GenericObjectSo asset) =>
            asset != null && asset.PrefabAsset != null && !string.IsNullOrEmpty(asset.PrefabAsset.Id) ? asset.PrefabAsset.Id : null;

        [CanBeNull]
        public static GenericObjectSo Resolve(string id, string savedName,
            Func<string, GenericObjectSo> byId, Func<string, GenericObjectSo> byName)
        {
            if (!string.IsNullOrEmpty(id))
            {
                GenericObjectSo asset = byId(id);
                if (asset != null)
                    return asset;
            }

            return string.IsNullOrEmpty(savedName) ? null : byName(RenamedTileObjects.Resolve(savedName));
        }
    }
}
