using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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

        private readonly Dictionary<string, AsyncOperationHandle<Object>> _handles = new();

        /// <inheritdoc/>
        public async Task InitializeAsync() => await Addressables.InitializeAsync().Task;

        /// <inheritdoc/>
        public async Task<Object> LoadAsync([NotNull] string guid, [NotNull] string resolvedKey)
        {
            if (!_handles.TryGetValue(guid, out AsyncOperationHandle<Object> handle))
            {
                handle = Addressables.LoadAssetAsync<Object>(resolvedKey);
                _handles.Add(guid, handle);
            }

            Object result = await handle.Task;

            if (handle.Status != AsyncOperationStatus.Failed)
            {
                OnLoaded?.Invoke(guid, result);

                return result;
            }

            if (_handles.Remove(guid))
            {
                Addressables.Release(handle);
            }

            return null;
        }

        /// <inheritdoc/>
        public void Unload([NotNull] string guid, [NotNull] string resolvedKey)
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
    }
}