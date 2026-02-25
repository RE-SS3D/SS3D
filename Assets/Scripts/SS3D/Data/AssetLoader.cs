using Coimbra;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using AssetDatabase = SS3D.Data.AssetDatabases.AssetDatabase;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// A class to get specific assets on the project, without having to assign them on the inspector or hardcoding Resources.Load.
    ///
    /// A more concise and in depth explanation on how this system works and how to use is present on the GitBook page for AssetData.
    /// </summary>
    public static class AssetLoader
    {
        /// <summary>
        /// Event invoked when a asset is loaded using Addressables.
        /// </summary>
        internal static event Action<KeyValuePair<string, Object>> OnAssetLoaded;

        /// <summary>
        /// A dictionary of the loaded databases, useful to get the databases quickly with the name of it.
        /// </summary>
        private static readonly Dictionary<string, AssetDatabase> Databases = new();

        /// <summary>
        /// A dictionary to keep track of the loading operations for each asset, so we don't load the same asset multiple times if multiple requests are made before the asset is done loading.
        /// </summary>
        private static readonly Dictionary<string, AsyncOperationHandle<Object>> LoadingOperations = new();

        /// <summary>
        /// Initializes the Addressables system and loads the asset databases in the project.
        /// </summary>
        public static async void InitializeAsync()
        {
            try
            {
                await Addressables.InitializeAsync();
                LoadAssetDatabases();
            }
            catch (Exception e)
            {
                Log.Error(typeof(AssetLoader), e, "An exception occurred while initializing the Addressables system.");
            }
        }

        /// <summary>
        /// Returns an asset from a  database casting the object found to TAsset.
        /// </summary>
        [CanBeNull]
        public static TAsset Get<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object => GetDatabase(databaseId)?.Get<TAsset>(assetId);

        /// <summary>
        /// Function to get an asset asynchronously from a database.
        /// </summary>
        /// <param name="databaseId">ID of the Database the asset is to be loaded from.</param>
        /// <param name="assetId">ID of the asset to be loaded.</param>
        /// <param name="onAssetLoaded">Callback to be called after the asset is loaded. (Optional)</param>
        /// <typeparam name="TAsset">Type of the asset (UnityEngine.Object).</typeparam>
        /// <returns>Task for the asset to be loaded.</returns>
        [ItemCanBeNull]
        public static async Task<TAsset> GetAsync<TAsset>([NotNull] string databaseId, [NotNull] string assetId, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class
        {
            TAsset asset = null;
            AssetReference reference = GetDatabase(databaseId)?.GetReference(assetId);

            if (reference != null)
            {
                asset = await GetAsync<TAsset>(reference);
            }

            try
            {
                onAssetLoaded?.Invoke(asset);
            }
            catch (Exception e)
            {
                Log.Error(typeof(AssetLoader), e, "An exception occurred while invoking the onAssetLoaded callback in GetAsync");
            }

            return asset;
        }

        /// <summary>
        /// Function to get an asset asynchronously from a WorldObjectAssetReference.
        /// </summary>
        /// <param name="reference">WorldObjectAssetReference object for the asset.</param>
        /// <typeparam name="TAsset">Type of the asset (UnityEngine.Object).</typeparam>
        /// <returns>Task for the asset to be loaded.</returns>
        [ItemCanBeNull]
        public static async Task<TAsset> GetAsync<TAsset>([NotNull] ObjectAssetReference reference)
            where TAsset : class => await GetAsync<TAsset>(reference.Database, reference.Id);

        /// <summary>
        /// Checks if an asset exists in a database with the given id.
        /// </summary>
        /// <param name="databaseId">Database to check in</param>
        /// <param name="assetId">Asset ID to check</param>
        /// <returns>True if specified database has that asset</returns>
        public static bool Has([NotNull] string databaseId, [NotNull] string assetId) => GetDatabase(databaseId)!.Has(assetId);

        /// <summary>
        /// Checks if an asset exists in any database
        /// </summary>
        /// <param name="assetId">Asset ID to check</param>
        /// <returns>True if any database has that asset</returns>
        public static bool Has([NotNull] string assetId)
        {
            return Databases.Any(pair => pair.Value.Has(assetId));
        }

        public static void Unload([NotNull] ObjectAssetReference assetReference)
        {
            Unload(assetReference.Id);
        }

        public static void Unload([NotNull] string assetId)
        {
            if (LoadingOperations.TryGetValue(assetId, out AsyncOperationHandle<Object> loadingOperation))
            {
                Addressables.Release(loadingOperation);
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

            if (string.IsNullOrEmpty(databaseId))
            {
                return null;
            }

            bool databaseExists = Databases.TryGetValue(databaseId, out AssetDatabase database);

            if (!databaseExists)
            {
                Log.Warning(typeof(AssetLoader), $"Database of type {databaseId} not found", Logs.Important);
            }

            return database;
        }

        /// <summary>
        /// Loads the databases in the project from the AssetDatabaseSettings, saves it in a Dictionary for easy & performant access.
        /// </summary>
        private static void LoadAssetDatabases()
        {
            List<AssetDatabase> assetDatabases = ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedAssetDatabases;

            Databases.Clear();

            foreach (AssetDatabase database in assetDatabases)
            {
                Databases.Add(database.DatabaseID, database);
            }

            Log.Information(typeof(AssetLoader), "{assetDatabasesCount} Asset Databases initialized", Logs.Important, assetDatabases.Count);
        }

        /// <summary>
        /// Function to get an asset asynchronously from an AssetReference.
        /// </summary>
        /// <param name="reference">Asset Reference object for the asset.</param>
        /// <typeparam name="TAsset">Type of the asset (UnityEngine.Object).</typeparam>
        /// <returns>Task for the asset to be loaded.</returns>
        [ItemCanBeNull]
        private static async Task<TAsset> GetAsync<TAsset>([NotNull] AssetReference reference)
            where TAsset : class
        {
            if (!LoadingOperations.TryGetValue(reference.AssetGUID, out AsyncOperationHandle<Object> loadingOperation))
            {
                loadingOperation = reference.LoadAssetAsync<Object>();
                LoadingOperations.Add(reference.AssetGUID, loadingOperation);
            }

            Object loadedAsset = await loadingOperation.Task;

            if (loadingOperation is
            {
                IsDone: true,
                Status: AsyncOperationStatus.Failed,
            })
            {
                LoadingOperations.Remove(reference.AssetGUID);

                return null;
            }

            try
            {
                OnAssetLoaded?.Invoke(new(reference.AssetGUID, loadedAsset));
            }
            catch (Exception e)
            {
                Log.Error(typeof(AssetLoader), e, "An exception occurred while invoking the OnAssetLoaded event in GetAsync");
            }

            TAsset asset = CastAsset<TAsset>(loadedAsset);

            return asset;
        }

        private static TAsset CastAsset<TAsset>(Object obj)
            where TAsset : class
        {
            if (obj is GameObject gameObject && typeof(TAsset) != typeof(GameObject))
            {
                return gameObject.GetComponent<TAsset>();
            }

            return obj as TAsset;
        }

#if UNITY_EDITOR
        public static bool AddToAddressables(string databaseID, Object asset)
        {
            AssetDatabase database = GetDatabase(databaseID);

            if (database)
            {
                return database.AddToAddressables(asset);
            }

            Log.Error(typeof(AssetLoader), $"Database of type {databaseID} not found cannot add to addressables");

            return false;
        }
#endif
    }
}