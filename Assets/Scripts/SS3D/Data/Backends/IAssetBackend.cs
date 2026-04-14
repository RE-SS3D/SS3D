using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Abstraction over a concrete asset-loading technology (e.g. Addressables, Resources).
    /// Implementations are the only place where backend-specific APIs should appear.
    /// </summary>
    public interface IAssetBackend : IDisposable
    {
        /// <summary>
        /// Raised after an asset is successfully loaded for the first time.
        /// </summary>
        event Action<string, Object> OnLoaded;

        /// <summary>
        /// Raised after an asset is unloaded from the backend.
        /// </summary>
        event Action<string> OnUnloaded;

        /// <summary>
        /// One-time initialization of the backend (e.g. <c>Addressables.InitializeAsync</c>).
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Loads the asset identified by <paramref name="resolvedKey"/> and returns it.
        /// The <paramref name="guid"/> is used for event notifications, while <paramref name="resolvedKey"/>
        /// is the backend-specific key used for the actual load operation.
        /// </summary>
        Task<Object> LoadAsync(string guid, string resolvedKey);

        /// <summary>
        /// Releases backend resources associated with <paramref name="resolvedKey"/>.
        /// The <paramref name="guid"/> is used for event notifications.
        /// </summary>
        void Unload(string guid, string resolvedKey);
    }
}
