using Coimbra;
using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System.Collections.Generic;
using AssetDatabase = SS3D.Data.AssetDatabases.AssetDatabase;

namespace SS3D.Data
{
    /// <summary>
    /// Holds the initialized set of SS3D asset databases and resolves logical asset keys to addressable references.
    /// </summary>
    internal static class AssetDatabaseCatalog
    {
        private static readonly Dictionary<string, AssetDatabase> Databases = new();

        internal static bool IsInitialized { get; private set; }

        [CanBeNull]
        public static AssetDatabase GetDatabase([CanBeNull] string databaseId)
        {
            if (!IsInitialized)
            {
                Log.Error(typeof(AssetDatabaseCatalog), "Asset databases were requested before AssetSubSystem initialization completed.");

                return null;
            }

            if (string.IsNullOrWhiteSpace(databaseId))
            {
                return null;
            }

            bool databaseExists = Databases.TryGetValue(databaseId, out AssetDatabase database);

            if (!databaseExists)
            {
                Log.Warning(typeof(AssetDatabaseCatalog), $"Database of type {databaseId} not found", Logs.Important);
            }

            return database;
        }

        /// <summary>
        /// Rebuilds the in-memory asset database registry from project settings.
        /// This loads database metadata only; it does not load any addressable assets.
        /// </summary>
        internal static void Initialize()
        {
            List<AssetDatabase> assetDatabases = ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedAssetDatabases;

            Databases.Clear();

            foreach (AssetDatabase database in assetDatabases)
            {
                Databases[database.DatabaseID] = database;
            }

            IsInitialized = true;
            Log.Information(typeof(AssetDatabaseCatalog), "{assetDatabasesCount} Asset Databases initialized", Logs.Important, assetDatabases.Count);
        }

        internal static void Reset()
        {
            Databases.Clear();
            IsInitialized = false;
        }

#if UNITY_EDITOR
        [CanBeNull]
        internal static AssetDatabase EditorGetDatabase([CanBeNull] string databaseId)
        {
            if (string.IsNullOrWhiteSpace(databaseId))
            {
                Log.Error(typeof(AssetDatabaseCatalog), "Cannot get database for editor because the provided database ID is null or whitespace.");

                return null;
            }

            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(databaseId);
            AssetDatabase database = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetDatabase>(path);

            if (database)
            {
                return database;
            }

            Log.Error(typeof(AssetDatabaseCatalog), $"Failed to load database asset at path '{path}' for database ID '{databaseId}'.");

            return null;
        }
#endif
    }
}