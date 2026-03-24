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
        /// One-time initialization of the backend (e.g. <c>Addressables.InitializeAsync</c>).
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Loads the asset identified by <paramref name="key"/> and returns it.
        /// The meaning of <paramref name="key"/> is backend-specific (GUID for Addressables, path for Resources, etc.).
        /// </summary>
        Task<Object> LoadAsync(string key);

        /// <summary>
        /// Releases backend resources associated with <paramref name="key"/>.
        /// </summary>
        void Unload(string key);
    }
}
