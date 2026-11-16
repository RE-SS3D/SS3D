using Coimbra;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using AssetDatabase = SS3D.Data.AssetDatabases.AssetDatabase;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// A class to get specific assets on the project, without having to assign them on the inspector or hardcoding Resources.Load.
    ///
    /// How this system works and how to use it: Documents/architecture/systems/data-codegen.md.
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
        /// Async load via Addressables (ref-counted). Prefer for databases with
        /// <see cref="AssetDatabaseLoadMode.AddressablesAsync"/>; also works for GUID keys in general.
        /// </summary>
        public static UniTask<AssetHandle<TAsset>> GetAsync<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object
        {
            AssetDatabase database = GetDatabase(databaseId);
            if (database == null)
            {
                return UniTask.FromException<AssetHandle<TAsset>>(
                    new InvalidOperationException($"Database '{databaseId}' not found."));
            }

            if (database.LoadMode == AssetDatabaseLoadMode.AddressablesAsync
                && database.AssetKeys != null
                && !database.AssetKeys.Contains(assetId))
            {
                return UniTask.FromException<AssetHandle<TAsset>>(
                    new InvalidOperationException(
                        $"Asset '{assetId}' is not listed on AddressablesAsync database '{database.DatabaseName}'."));
            }

            return AssetProvider.AcquireAsync<TAsset>(assetId);
        }

        /// <summary>
        /// Returns an asset from a database casting the object found to TAsset.
        /// </summary>
        public static bool TryGet<TAsset>([NotNull] string databaseId, [NotNull] string assetId, [CanBeNull] out TAsset asset)
            where TAsset : Object
        {
            AssetDatabase database = GetDatabase(databaseId);
            if (database == null)
            {
                asset = null;
                return false;
            }

            return database.TryGet(assetId, out asset);
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
                if (database == null || string.IsNullOrEmpty(database.DatabaseID))
                {
                    Log.Warning(
                        typeof(Assets),
                        "IncludedAssetDatabases[{index}] is missing or has no DatabaseID; skipping (stale settings entry?)",
                        Logs.Important,
                        index);
                    continue;
                }

                Databases.Add(database.DatabaseID, database);
            }

            Log.Debug(typeof(Assets), "{assetDatabasesCount} Asset Databases initialized", Logs.Important, assetDatabases.Count);
        }

        /// <summary>
        /// Warm-cache all <see cref="AssetDatabaseLoadMode.AddressablesAsync"/> databases so sync Get still works.
        /// </summary>
        public static void PreloadAddressableDatabases()
        {
            if (Databases.Count == 0)
            {
                LoadAssetDatabases();
            }

            foreach (AssetDatabase database in Databases.Values)
            {
                if (database.LoadMode != AssetDatabaseLoadMode.AddressablesAsync)
                {
                    continue;
                }

                if (database.AssetKeys == null || database.AssetKeys.Count == 0)
                {
                    Log.Warning(
                        typeof(Assets),
                        "AddressablesAsync database {name} has no AssetKeys; sync Get will fail until keys are loaded.",
                        Logs.Important,
                        database.DatabaseName);
                    continue;
                }

                Log.Debug(
                    typeof(Assets),
                    "Preloading {count} Addressables keys for {name}",
                    Logs.Important,
                    database.AssetKeys.Count,
                    database.DatabaseName);

                // InteractionIcons are Sprite assets; typed load avoids Addressables Object cast failures.
                if (string.Equals(database.DatabaseName, "InteractionIcons", StringComparison.Ordinal))
                {
                    AssetProvider.PreloadAndHoldSync<UnityEngine.Sprite>(database.AssetKeys);
                }
                else
                {
                    AssetProvider.PreloadAndHoldSync(database.AssetKeys);
                }
            }
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
