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
        /// Loads the asset identified by <paramref name="key"/> and returns a handle.
        /// If the asset is already loaded the existing instance is reused regardless of the backend parameter.
        /// </summary>
        Task<AssetHandle<T>> AcquireAsync<T>(string key, IAssetBackend backend)
            where T : class;

        /// <summary>
        /// Unloads the asset identified by <paramref name="key"/> from its backend
        /// and removes it from the loaded set.
        /// </summary>
        void Unload(string key);

        /// <summary>
        /// Returns <see langword="true"/> if the asset identified by <paramref name="key"/> is currently loaded.
        /// </summary>
        bool IsLoaded(string key);
    }
}