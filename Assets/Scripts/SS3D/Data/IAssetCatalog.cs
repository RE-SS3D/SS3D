using JetBrains.Annotations;
using SS3D.Logging;

namespace SS3D.Data
{
    /// <summary>
    /// Pure resolver that maps asset GUIDs to backend-specific keys.
    /// Catalogs do not load assets or manage ref counts.
    /// </summary>
    public interface IAssetCatalog
    {
        /// <summary>
        /// The backend type this catalog routes to.
        /// </summary>
        AssetBackendType BackendType { get; }

        /// <summary>
        /// Returns true if this catalog knows about the given GUID.
        /// </summary>
        bool Has([NotNull] string guid);

        /// <summary>
        /// Resolves a GUID to the backend-specific key used for loading.
        /// Implementors provide the actual resolution logic via <see cref="ResolveKeyInternal"/>.
        /// </summary>
        [CanBeNull]
        string ResolveKey([NotNull] string guid)
        {
            if (!Has(guid))
            {
                Log.Error(typeof(IAssetCatalog), "No asset found for GUID '{Guid}' in {CatalogType}.", Logs.Important, guid, GetType().Name);
                return null;
            }

            return ResolveKeyInternal(guid);
        }

        /// <summary>
        /// Backend-specific key resolution. Called by <see cref="ResolveKey"/> after validation.
        /// </summary>
        [NotNull]
        string ResolveKeyInternal([NotNull] string guid);
    }
}
