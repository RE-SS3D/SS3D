using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using System.Linq;
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
                return result;
            }

            Addressables.Release(handle);
            _handles.Remove(guid);

            return null;
        }

        /// <inheritdoc/>
        public void Unload([NotNull] string guid)
        {
            if (_handles.Remove(guid, out AsyncOperationHandle<Object> handle) && handle.IsValid())
            {
                Addressables.Release(handle);
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
