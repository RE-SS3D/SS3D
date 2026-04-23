#if UNITY_EDITOR
using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace SS3D.Data.AssetDatabases
{
    public abstract partial class AssetCatalog
    {
        /// <summary>
        /// Discovers all databases of this catalog's type in the project.
        /// Concrete catalogs override this with their type-specific search logic.
        /// </summary>
        [NotNull]
        public abstract IEnumerable<AssetDatabase> FindAllDatabasesInProject();

        /// <summary>
        /// Refreshes the backing database list from the project via <see cref="FindAllDatabasesInProject"/>.
        /// Called from the AssetDatabaseSettings editor's "Find and load" button.
        /// </summary>
        public void PopulateFromProject()
        {
            _databases = FindAllDatabasesInProject().ToList();
            EditorUtility.SetDirty(this);
        }
    }
}
#endif