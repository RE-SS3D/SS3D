using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Pure asset loader. Single source of truth for loaded asset state.
    /// The backend to use is passed per-call by the caller.
    /// Does not make lifecycle decisions — an external owner decides when to unload.
    /// </summary>
    public interface IAssetProvider : IDisposable
    {
        /// <summary>
        /// Loads the asset identified by <paramref name="guid"/> and returns a handle.
        /// If the asset is already loaded the existing instance is reused regardless of the backend parameter.
        /// The <paramref name="resolvedKey"/> is only used for backend loading and unloading.
        /// </summary>
        Task<AssetHandle<T>> AcquireAsync<T>(string guid, string resolvedKey, IAssetBackend backend)
            where T : class;

        /// <summary>
        /// Unloads the asset identified by <paramref name="guid"/> from its backend
        /// and removes it from the loaded set.
        /// </summary>
        void Unload(string guid);

        /// <summary>
        /// Returns <see langword="true"/> if the asset identified by <paramref name="guid"/> is currently loaded.
        /// </summary>
        bool IsLoaded(string guid);
    }
}