using JetBrains.Annotations;
using SS3D.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Groups a set of asset databases that share a backend. The catalog owns the
    /// databases (serialized reference list) and produces the backend that will
    /// load their assets, keeping <see cref="AssetSubSystem"/> backend-agnostic.
    /// </summary>
    public abstract partial class AssetCatalog : ScriptableObject
    {
        /// <summary>
        /// The menu hierarchy under which to create new catalogs in the editor. Concrete catalogs should append their backend type.
        /// </summary>
        protected const string CatalogMenuHierarchy = "SS3D/Asset Subsystem/Catalog/";
        
        [SerializeField]
        private List<AssetDatabase> _databases = new();

        /// <summary>
        /// The databases grouped under this catalog.
        /// </summary>
        public IReadOnlyList<AssetDatabase> Databases => _databases;

        /// <summary>
        /// The backend type this catalog routes to.
        /// </summary>
        public abstract AssetBackendType BackendType { get; }

        /// <summary>
        /// Constructs the backend implementation responsible for loading assets from this catalog.
        /// Called once per distinct backend type at system initialization.
        /// </summary>
        [NotNull]
        public abstract IAssetBackend CreateBackend();

        /// <summary>
        /// Returns true if any of the catalog's databases contain the given GUID.
        /// </summary>
        public bool Has([NotNull] string guid) =>
            _databases != null && _databases.Any(database => database && database.Has(guid));

        /// <summary>
        /// Resolves a GUID to the backend-specific key by delegating to the owning database.
        /// </summary>
        [CanBeNull]
        public string ResolveKey([NotNull] string guid)
        {
            if (Has(guid))
            {
                return _databases.First(database => database.Has(guid)).ResolveKey(guid);
            }

            Log.Error(this, "No asset found for GUID '{Guid}' in {CatalogType}.", Logs.Important, guid, GetType().Name);

            return null;
        }
    }
}