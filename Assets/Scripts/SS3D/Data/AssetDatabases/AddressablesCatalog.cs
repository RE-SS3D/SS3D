using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System.Linq;

namespace SS3D.Data
{
    /// <summary>
    /// <see cref="IAssetCatalog"/> for Addressables assets.
    /// Wraps a set of <see cref="AssetDatabase"/> instances and resolves GUIDs
    /// using identity mapping (GUID is the Addressables key).
    /// </summary>
    internal sealed class AddressablesCatalog : IAssetCatalog
    {
        private AssetDatabase[] _databases;

        public AssetBackendType BackendType => AssetBackendType.Addressables;

        /// <inheritdoc/>
        public bool Has(string guid)
        {
            return _databases != null && _databases.Any(database => database.Has(guid));
        }

        /// <inheritdoc/>
        public string ResolveKeyInternal(string guid) => _databases.First(database => database.Has(guid)).ResolveKey(guid);

        /// <summary>
        /// Initializes the catalog with the given databases.
        /// </summary>
        internal void Initialize([NotNull] AssetDatabase[] databases)
        {
            _databases = databases;
            Log.Information(typeof(AddressablesCatalog), "{Count} asset databases registered in Addressables catalog.", Logs.Important, databases.Length);
        }

        internal void Reset()
        {
            _databases = null;
        }
    }
}