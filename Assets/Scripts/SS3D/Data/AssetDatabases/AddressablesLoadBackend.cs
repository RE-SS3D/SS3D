using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Production backend: load/release through Unity Addressables by GUID (or address) key.
    /// </summary>
    public sealed class AddressablesLoadBackend : IAssetLoadBackend
    {
        public async UniTask<(TAsset Asset, object ReleaseToken)> LoadAsync<TAsset>(string key)
            where TAsset : Object
        {
            AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(key);
            await UniTask.WaitUntil(() => handle.IsDone);
            TAsset asset = handle.Status == AsyncOperationStatus.Succeeded ? handle.Result : null;
            return Validate(key, handle, asset);
        }

        public (TAsset Asset, object ReleaseToken) LoadSync<TAsset>(string key)
            where TAsset : Object
        {
            AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(key);
            TAsset asset = handle.WaitForCompletion();
            return Validate(key, handle, asset);
        }

        public void Release(object releaseToken)
        {
            if (releaseToken is AsyncOperationHandle handle && handle.IsValid())
            {
                Addressables.Release(handle);
            }
        }

        private static (TAsset Asset, object ReleaseToken) Validate<TAsset>(
            string key,
            AsyncOperationHandle<TAsset> handle,
            TAsset asset)
            where TAsset : Object
        {
            if (asset == null || !handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }

                throw new InvalidOperationException(
                    $"Addressables failed to load key '{key}' as {typeof(TAsset).Name}.");
            }

            // Box the typed handle so Release can Addressables.Release it.
            AsyncOperationHandle untyped = handle;
            return (asset, untyped);
        }
    }
}
