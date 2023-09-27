using System.Collections.Generic;
using Coimbra;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using UnityEngine;
using SS3D.Logging;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Assets is used to load assets using enums or ints, with the addition of an enum generator for easy "id" of items.
    /// </summary>
    public static class Assets
    {
        /// <summary>
        /// All loaded databases.
        /// </summary>
        private static readonly Dictionary<string, AssetDatabase> Databases = new();

        // TODO: Find a way to automate the getters, this is a nightmare way of doing it.

        /// <summary>
        /// Generic getter, supports all databases and all asset database enums.
        /// </summary>
        /// <param name="databaseId"></param>
        /// <param name="assetId"></param>
        /// <typeparam name="TAsset"></typeparam>
        /// <returns></returns>
        public static TAsset Get<TAsset>(string databaseId, string assetId) where TAsset : Object
        {
            return GetDatabase(databaseId).Get<TAsset>(assetId);
        }
        /// <summary>
        /// Loads the databases in the project from the AssetDatabaseSettings, saves it in a Dictionary for easy & performant access.
        /// </summary>
        public static void LoadAssetDatabases()
        {
            List<AssetDatabase> assetDatabases = ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedAssetDatabases;

            Databases.Clear();

            for (int index = 0; index < assetDatabases.Count; index++)
            {
                AssetDatabase database = assetDatabases[index];
                Databases.Add(database.name, database);
            }

            Log.Information(typeof(Assets), "{assetDatabasesCount} Asset Databases initialized", Logs.Important, assetDatabases.Count);
        }

        /// <summary>
        /// Helper function to find a database in the database list. Used to link the enum to which database to find.
        /// </summary>
        /// <param name="key">The enum name used to identify which database to load.</param>
        /// <returns></returns>
        public static AssetDatabase GetDatabase(string key)
        {
            if (Databases.Count == 0)
            {
                LoadAssetDatabases();
            }

            bool databaseExists = Databases.TryGetValue(key, out AssetDatabase database);

            if (!databaseExists)
            {
                Log.Warning(typeof(Assets), "Database of type {key} not found", Logs.Important, key);
            }

            return database;
        }
    }
}