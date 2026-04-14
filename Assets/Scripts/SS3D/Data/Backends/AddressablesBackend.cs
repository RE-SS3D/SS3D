using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Concrete <see cref="IAssetBackend"/> wrapping Unity Addressables.
    /// This is the only file in the asset system that references <c>UnityEngine.AddressableAssets</c>.
    /// <para>
    /// Loads assets by GUID string directly. The Addressables catalog maps GUIDs to asset locations
    /// at build time, so no <see cref="AssetReference"/> dependency is needed.
    /// </para>
    /// </summary>
    internal sealed class AddressablesBackend : IAssetBackend
    {
        /// <inheritdoc/>>
        public event Action<string, Object> OnLoaded;

        /// <inheritdoc/>>
        public event Action<string> OnUnloaded;

        /// <summary>
        /// 
        /// </summary>
        private readonly Dictionary<string, AsyncOperationHandle<Object>> _handles = new();

        /// <inheritdoc/>
        public async Task InitializeAsync() => await Addressables.InitializeAsync().Task;

        /// <inheritdoc/>
        public async Task<Object> LoadAsync([NotNull] string guid)
        {
            AsyncOperationHandle<Object> handle = Addressables.LoadAssetAsync<Object>(guid);
            _handles[guid] = handle;

            Object result = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Failed)
            {
                await InvokeOnLoadedAsync(guid, result);
                return result;
            }

            Addressables.Release(handle);
            _handles.Remove(guid);

            return null;
        }

        /// <inheritdoc/>
        public void Unload([NotNull] string guid)
        {
            if (!_handles.Remove(guid, out AsyncOperationHandle<Object> handle) || !handle.IsValid())
            {
                return;
            }

            Addressables.Release(handle);

            if (!handle.IsValid())
            {
                OnUnloaded?.Invoke(guid);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            foreach (AsyncOperationHandle<Object> handle in _handles.Values.Where(handle => handle.IsValid()))
            {
                Addressables.Release(handle);
            }

            _handles.Clear();
        }

        /// <summary>
        /// Fires <see cref="OnLoaded"/> for each addressable dependency of <paramref name="guid"/>,
        /// then for the root asset itself. Dependencies are temporarily loaded to obtain a reference
        /// and immediately released so they remain under the root handle's ref count.
        /// </summary>
        private async Task InvokeOnLoadedAsync(string guid, Object rootAsset)
        {
            IList<IResourceLocation> locations =
                await Addressables.LoadResourceLocationsAsync(guid, typeof(Object)).Task;

            foreach (IResourceLocation location in locations)
            {
                if (location.PrimaryKey == guid)
                {
                    continue;
                }

                AsyncOperationHandle<Object> handle = Addressables.LoadAssetAsync<Object>(location);
                Object asset = await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    await InvokeOnLoadedAsync(location.PrimaryKey, asset);
                }

                Addressables.Release(handle);
            }

            OnLoaded?.Invoke(guid, rootAsset);
        }
    }
}
