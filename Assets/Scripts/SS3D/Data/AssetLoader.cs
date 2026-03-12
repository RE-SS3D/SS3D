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
    /// Central entry point for resolving assets referenced by SS3D asset databases.
    /// It provides synchronous lookup for directly referenced assets and asynchronous
    /// Addressables-backed loading with a shared per-GUID handle cache.
    /// </summary>
    public static class AssetLoader
    {
        /// <summary>
        /// Raised after an addressable asset finishes loading through this loader.
        /// The key is the asset GUID used by Addressables.
        /// </summary>
        internal static event Action<KeyValuePair<string, Object>> OnAssetLoaded;

        /// <summary>
        /// Raised after a cached addressable handle is released through <see cref="Unload(string)"/>.
        /// </summary>
        internal static event Action<string> OnAssetUnloaded;

        /// <summary>
        /// Asset database registry keyed by database ID. The databases describe how logical
        /// asset IDs map to direct object references and Addressables references.
        /// </summary>
        private static readonly Dictionary<string, AssetDatabase> Databases = new();

        /// <summary>
        /// Shared Addressables handle cache keyed by asset GUID.
        /// The same handle is reused for concurrent requests and remains resident until <see cref="Unload(string)"/> releases it.
        /// </summary>
        private static readonly Dictionary<string, AsyncOperationHandle<Object>> LoadingOperations = new();

        /// <summary>
        /// Boots Addressables and populates the in-memory asset database registry.
        /// This method is intentionally fire-and-forget because it is used during project startup.
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
        /// Checks whether this loader currently holds a successful Addressables handle for the given GUID.
        /// </summary>
        /// <param name="guid">GUID of the addressable asset.</param>
        /// <returns><see langword="true"/> when the cached handle completed successfully.</returns>
        public static bool IsLoaded([NotNull] string guid) => LoadingOperations.TryGetValue(guid, out AsyncOperationHandle<Object> loadingOperation)
            && loadingOperation is
            {
                IsDone: true,
                Status: AsyncOperationStatus.Succeeded,
            };

        /// <summary>
        /// Returns an asset from a database using its direct serialized reference.
        /// This path does not go through Addressables and assumes the asset is already part of the loaded content set.
        /// </summary>
        [CanBeNull]
        public static TAsset Get<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object => GetDatabase(databaseId)?.Get<TAsset>(assetId);

        /// <summary>
        /// Resolves an asset by logical database and asset IDs, loading it through Addressables when needed.
        /// </summary>
        /// <param name="databaseId">ID of the Database the asset is to be loaded from.</param>
        /// <param name="assetId">ID of the asset to be loaded.</param>
        /// <param name="onAssetLoaded">Optional callback invoked after the load attempt completes.</param>
        /// <typeparam name="TAsset">Requested asset type or component type.</typeparam>
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
        /// Convenience overload that resolves an asset from a serialized SS3D asset reference.
        /// </summary>
        /// <param name="reference">ObjectAssetReference object for the asset.</param>
        /// <typeparam name="TAsset">Requested asset type or component type.</typeparam>
        /// <returns>Task for the asset to be loaded.</returns>
        [ItemCanBeNull]
        public static async Task<TAsset> GetAsync<TAsset>([NotNull] ObjectAssetReference reference)
            where TAsset : class => await GetAsync<TAsset>(reference.Database, reference.Id);

        /// <summary>
        /// Checks whether the specified database contains an entry for the given asset ID.
        /// This is a metadata query and does not indicate whether the asset is currently loaded.
        /// </summary>
        /// <param name="databaseId">Database to check in</param>
        /// <param name="assetId">Asset ID to check</param>
        /// <returns>True if specified database has that asset</returns>
        public static bool Has([NotNull] string databaseId, [NotNull] string assetId)
        {
            AssetDatabase database = GetDatabase(databaseId);

            return database && database.Has(assetId);
        }

        /// <summary>
        /// Checks whether any registered database contains an entry for the given asset ID.
        /// </summary>
        /// <param name="assetId">Asset ID to check</param>
        /// <returns>True if any database has that asset</returns>
        public static bool Has([NotNull] string assetId)
        {
            return Databases.Any(pair => pair.Value.Has(assetId));
        }

        /// <summary>
        /// Releases the cached Addressables handle associated with the given asset reference.
        /// </summary>
        /// <param name="assetReference">ObjectAssetReference of the object to be unloaded</param>
        public static void Unload([NotNull] ObjectAssetReference assetReference)
        {
            Unload(assetReference.Id);
        }

        /// <summary>
        /// Releases the cached Addressables handle associated with the given asset GUID.
        /// This only affects assets loaded through <see cref="GetAsync{TAsset}(string,string,Action{TAsset})"/> or its overloads.
        /// </summary>
        /// <param name="guid">guid of the prefab</param>
        public static void Unload([NotNull] string guid)
        {
            if (!LoadingOperations.TryGetValue(guid, out AsyncOperationHandle<Object> loadingOperation))
            {
                return;
            }

            Addressables.Release(loadingOperation);
            LoadingOperations.Remove(guid);
            try
            {
                OnAssetUnloaded?.Invoke(guid);
            }
            catch (Exception e)
            {
                Log.Error(typeof(AssetLoader), e, "An exception occurred while invoking the OnAssetUnloaded event in Unload");
            }
        }

        /// <summary>
        /// Resolves a registered asset database by ID.
        /// The registry is lazily initialized as a fallback for call sites that run before the normal boot path.
        /// </summary>
        /// <param name="databaseId">The id used to identify which database to load.</param>
        /// <returns>the database corresponding to the ID provided</returns>
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
        /// Rebuilds the in-memory asset database registry from project settings.
        /// This loads database metadata only; it does not load any addressable assets.
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

        // ReSharper disable Unity.PerformanceAnalysis

        /// <summary>
        /// Loads an Addressables asset by reference while deduplicating concurrent requests for the same GUID.
        /// </summary>
        /// <param name="reference">Asset Reference object for the asset.</param>
        /// <typeparam name="TAsset">Requested asset type or component type.</typeparam>
        /// <returns>Task for the asset to be loaded.</returns>
        [ItemCanBeNull]
        private static async Task<TAsset> GetAsync<TAsset>([NotNull] AssetReference reference)
            where TAsset : class
        {
            if (!LoadingOperations.TryGetValue(reference.AssetGUID, out AsyncOperationHandle<Object> loadingOperation))
            {
                // Cache the handle immediately so later requests await the same operation instead of kicking off a duplicate load.
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

        /// <summary>
        /// Adapts a loaded Unity object to the requested API surface.
        /// Prefab GameObjects can be requested as one of their components for convenience.
        /// </summary>
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
