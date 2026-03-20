using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Ref-counted resource manager. Single source of truth for loaded asset state.
    /// The backend to use is passed per-call by the caller.
    /// </summary>
    public interface IAssetStore : IDisposable
    {
        /// <summary>
        /// Raised after an asset is successfully loaded for the first time (first acquire).
        /// </summary>
        event Action<string, Object> OnLoaded;

        /// <summary>
        /// Raised after an asset's last handle is disposed and the backend releases it.
        /// </summary>
        event Action<string> OnUnloaded;

        /// <summary>
        /// Acquires a ref-counted handle for the asset identified by <paramref name="key"/>.
        /// If the asset is already loaded the existing instance is reused regardless of the backend parameter.
        /// </summary>
        Task<AssetHandle<T>> AcquireAsync<T>(string key, IAssetBackend backend) where T : class;

        /// <summary>
        /// Returns <see langword="true"/> if the asset identified by <paramref name="key"/> is currently loaded.
        /// </summary>
        bool IsLoaded(string key);
    }
}
