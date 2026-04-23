using Coimbra;
using SS3D.Attributes;
using SS3D.CodeGeneration.Creators;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Data.AssetDatabases
{
    [ProjectSettings("SS3D/Assets")]
    public sealed class AssetDatabaseSettings : ScriptableSettings
    {
        /// <summary>
        /// Catalogs included in the game. Each catalog owns its own databases and produces the backend
        /// that loads their assets.
        /// </summary>
#if UNITY_EDITOR
        [ReadOnly]
#endif
        public List<AssetCatalog> IncludedCatalogs = new();

        /// <summary>
        /// Flat view of every database owned by every included catalog, with nulls filtered out.
        /// </summary>
        [JetBrains.Annotations.NotNull]
        public IEnumerable<AssetDatabase> AllDatabases =>
            IncludedCatalogs == null
                ? Enumerable.Empty<AssetDatabase>()
                : IncludedCatalogs.Where(catalog => catalog).SelectMany(catalog => catalog.Databases).Where(database => database);

#if UNITY_EDITOR
        [SerializeField]
        private bool _skipCodeGeneration;

        /// <summary>
        /// If ticked the asset data system won't try to generate the generated asset data code.
        /// </summary>
        public static bool SkipCodeGeneration => GetOrFind<AssetDatabaseSettings>()._skipCodeGeneration;
#endif

#if UNITY_EDITOR
        /// <summary>
        /// Generates the script with the data from this database.
        /// </summary>
        public void CreateDatabaseCode()
        {
            if (SkipCodeGeneration)
            {
                return;
            }

            const string dataPath = AssetDatabase.DatabaseAssetPath;

            DatabaseScriptCreator.CreateAtPath(
                dataPath,
                "AssetDatabases",
                AllDatabases.Select(db => db.DatabaseID).ToList(),
                AssetDatabase.DatabaseAssetNamespaceName);
        }
#endif

        public bool Has(string guid)
        {
            return IncludedCatalogs != null && IncludedCatalogs.Any(catalog => catalog.Has(guid));
        }
    }
}