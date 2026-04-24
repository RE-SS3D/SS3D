using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// Base class for all asset databases. Each concrete database holds its own serialized
    /// mapping and provides backend-specific key resolution.
    /// </summary>
    public abstract partial class AssetDatabase : ScriptableObject
    {
        /// <summary>
        /// The menu hierarchy under which to create new database in the editor. Concrete catalogs should append their backend type.
        /// </summary>
        protected const string DatabaseMenuHierarchy = "SS3D/Asset Subsystem/Database/";

        /// <summary>
        /// The ID of the database, derived from the asset's GUID on disk.
        /// </summary>
        [field: SerializeField]
        public string DatabaseID { get; internal set; }

        /// <summary>
        /// The set of asset GUIDs registered in this database.
        /// Backed by whatever storage the concrete database chooses (List, Dictionary keys, HashSet, etc.)
        /// to avoid double-serialization when the backend requires guid-to-key mapping storage.
        /// </summary>
        [NotNull]
        public abstract IReadOnlyCollection<string> AssetGuids { get; }

        /// <summary>
        /// Returns true if this database contains the given GUID.
        /// Override for O(1) performance when the backing store is a set or dictionary.
        /// </summary>
        public virtual bool Has([NotNull] string guid) => AssetGuids.Contains(guid);

        /// <summary>
        /// Resolves a GUID to the backend-specific key used for loading.
        /// </summary>
        [NotNull]
        public abstract string ResolveKey([NotNull] string guid);
    }
}