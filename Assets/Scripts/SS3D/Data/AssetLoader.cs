using JetBrains.Annotations;
using SS3D.Core;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Convenience wrapper for synchronous asset lookups via the asset database catalog.
    /// </summary>
    public static class AssetLoader
    {
        [CanBeNull]
        public static TAsset Get<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object
        {
            AssetSubSystem assetSubSystem = SubSystems.Get<AssetSubSystem>();

            return assetSubSystem ? assetSubSystem.Get<TAsset>(databaseId, assetId) : null;
        }
    }
}
