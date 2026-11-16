using System;
using Cysharp.Threading.Tasks;
using Object = UnityEngine.Object;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Abstraction over Addressables (or a test double) for <see cref="AssetProvider"/>.
    /// </summary>
    public interface IAssetLoadBackend
    {
        UniTask<(TAsset Asset, object ReleaseToken)> LoadAsync<TAsset>(string key)
            where TAsset : Object;

        (TAsset Asset, object ReleaseToken) LoadSync<TAsset>(string key)
            where TAsset : Object;

        void Release(object releaseToken);
    }
}
