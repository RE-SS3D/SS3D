using Coimbra;
using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using AssetDatabase = SS3D.Data.AssetDatabases.AssetDatabase;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SS3D.Data
{
    /// <summary>
    /// A class to get specific assets on the project, without having to assign them on the inspector or hardcoding Resources.Load.
    ///
    /// A more concise and in depth explanation on how this system works and how to use is present on the GitBook page for AssetData.
    /// </summary>
    public static class Assets
    {
        /// <summary>
        /// A dictionary of the loaded databases, useful to get the databases quickly with the name of it.
        /// </summary>
        private static readonly Dictionary<string, AssetDatabase> Databases = new();

        /// <summary>
        /// Returns an asset from a  database casting the object found to TAsset.
        /// </summary>
        [CanBeNull]
        public static TAsset Get<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object
        {
            return GetDatabase(databaseId)?.Get<TAsset>(assetId);
        }

        /// <summary>
        /// Returns an asset from a  database casting the object found to TAsset.
        /// </summary>
        [CanBeNull]
        public static TAsset Get<TAsset>([NotNull] ObjectAssetReference assetReference)
            where TAsset : Object
        {
            return GetDatabase(assetReference.Database)?.Get<TAsset>(assetReference.Id);
        }

        /// <summary>
        /// Returns an asset from a database casting the object found to TAsset.
        /// </summary>
        public static bool TryGet<TAsset>([NotNull] string databaseId, [NotNull] string assetId, [CanBeNull] out TAsset asset)
            where TAsset : Object
        {
            return GetDatabase(databaseId).TryGet(assetId, out asset);
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
                Databases.Add(database.DatabaseID, database);
            }

            Log.Information(typeof(Assets), "{assetDatabasesCount} Asset Databases initialized", Logs.Important, assetDatabases.Count);
        }

        /// <summary>
        /// Helper function to find a database in the database dict.
        /// </summary>
        /// <param name="databaseId">The id used to identify which database to load.</param>
        /// <returns></returns>
        [CanBeNull]
        public static AssetDatabase GetDatabase([NotNull] string databaseId)
        {
            // TODO: Move this to the new initialization flow.
            if (Databases.Count == 0)
            {
                LoadAssetDatabases();
            }

            if (databaseId == String.Empty)
            {
                return null;
            }

            bool databaseExists = Databases.TryGetValue(databaseId, out AssetDatabase database);

            if (!databaseExists)
            {
                Log.Warning(typeof(Assets), $"Database of type {databaseId} not found", Logs.Important);
            }

            return database;
        }

#if UNITY_EDITOR
        public static bool AddToAddressables(string databaseID, Object asset)
        {
            AssetDatabase database = GetDatabase(databaseID);

            if (database)
            {
                return database.AddToAddressables(asset);
            }

            Log.Error(typeof(Assets), $"Database of type {databaseID} not found cannot add to addressables");
            return false;

        }
#endif
    }
}