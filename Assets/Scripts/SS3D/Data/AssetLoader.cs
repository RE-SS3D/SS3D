using JetBrains.Annotations;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using AssetDatabase = SS3D.Data.AssetDatabases.AssetDatabase;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Facade for SS3D asset lookup and addressable residency.
    /// Database resolution and addressable residency are handled by dedicated internal services,
    /// while this class preserves the public compatibility surface used across the project.
    /// </summary>
    public static class AssetLoader
    {
        /// <summary>
        /// Raised after an addressable asset finishes loading through this loader.
        /// The key is the asset GUID used by Addressables.
        /// </summary>
        internal static event Action<KeyValuePair<string, Object>> OnAssetLoaded
        {
            add => AssetResidencyRegistry.OnAssetLoaded += value;
            remove => AssetResidencyRegistry.OnAssetLoaded -= value;
        }

        /// <summary>
        /// Raised after the final ownership claim for a shared addressable residency record is released.
        /// </summary>
        internal static event Action<string> OnAssetUnloaded
        {
            add => AssetResidencyRegistry.OnAssetUnloaded += value;
            remove => AssetResidencyRegistry.OnAssetUnloaded -= value;
        }

        /// <summary>
        /// Compatibility owner used by the legacy async loading path. Assets loaded through
        /// <see cref="GetAsync{TAsset}(string,string,Action{TAsset})"/> stay resident until
        /// the matching compatibility unload path releases this owner.
        /// </summary>
        private static readonly AssetOwnerToken LegacyAsyncOwner = AssetOwnerToken.Create("AssetLoader.LegacyAsync");

        /// <summary>
        /// Shared owner token used by the synchronized network path.
        /// Network residency is kept alive until the active network world manifest releases it.
        /// </summary>
        private static readonly AssetOwnerToken NetworkAsyncOwner = AssetOwnerToken.Create("AssetLoader.Network");

        private static Task InitializationTask;

        /// <summary>
        /// Returns <see langword="true"/> when the asset system finished initialization successfully.
        /// </summary>
        public static bool IsInitialized => AssetDatabaseCatalog.IsInitialized && InitializationTask is { IsCompletedSuccessfully: true };

        /// <summary>
        /// Boots Addressables and populates the in-memory asset database registry.
        /// Callers are expected to trigger this explicitly during application startup.
        /// </summary>
        [NotNull]
        public static Task InitializeAsync()
        {
            if (InitializationTask == null || InitializationTask.IsCanceled || InitializationTask.IsFaulted)
            {
                InitializationTask = InitializeInternalAsync();
            }

            return InitializationTask;
        }

        /// <summary>
        /// Checks whether this loader currently holds a successful Addressables handle for the given GUID.
        /// </summary>
        public static bool IsLoaded([NotNull] string guid) => AssetResidencyRegistry.IsLoaded(guid);

        /// <summary>
        /// Returns an asset from a database using its direct serialized reference.
        /// This is the legacy direct-reference path and does not go through Addressables.
        /// </summary>
        [CanBeNull]
        public static TAsset Get<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object => AssetDatabaseCatalog.GetDatabase(databaseId)?.Get<TAsset>(assetId);

        /// <summary>
        /// Resolves an asset by logical database and asset IDs through the legacy compatibility async path.
        /// This keeps a shared compatibility owner claim so existing GetAsync/Unload callers keep their previous behavior
        /// while newer systems migrate to the explicit ownership API.
        /// </summary>
        [ItemCanBeNull]
        public static async Task<TAsset> GetAsync<TAsset>([NotNull] string databaseId, [NotNull] string assetId, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(new AssetKey(databaseId, assetId), LegacyAsyncOwner, onAssetLoaded);

        /// <summary>
        /// Resolves an asset by <see cref="AssetKey"/> through the legacy compatibility async path.
        /// This keeps a shared compatibility owner claim so existing GetAsync/Unload callers keep their previous behavior
        /// while newer systems migrate to the explicit ownership API.
        /// </summary>
        [ItemCanBeNull]
        public static async Task<TAsset> GetAsync<TAsset>(AssetKey assetKey, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(assetKey, LegacyAsyncOwner, onAssetLoaded);

        /// <summary>
        /// Convenience overload that resolves an asset from a serialized SS3D asset reference.
        /// </summary>
        [ItemCanBeNull]
        public static async Task<TAsset> GetAsync<TAsset>([NotNull] ObjectAssetReference reference)
            where TAsset : class => await AcquireAsync<TAsset>(new AssetKey(reference.Database, reference.Id), LegacyAsyncOwner);

        /// <summary>
        /// Resolves an asset through the Addressables residency system while registering an explicit ownership claim.
        /// Owners keep one shared claim until <see cref="Release(AssetKey,AssetOwnerToken)"/> removes it.
        /// </summary>
        [ItemCanBeNull]
        public static async Task<TAsset> AcquireAsync<TAsset>(AssetKey assetKey, AssetOwnerToken owner, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class
        {
            if (!owner.IsValid)
            {
                Log.Warning(typeof(AssetLoader), $"Cannot acquire asset '{assetKey}' because the owner token is invalid.");
                InvokeOnAssetLoadedCallback(onAssetLoaded, null);

                return null;
            }

            if (!await EnsureInitializedAsync())
            {
                InvokeOnAssetLoadedCallback(onAssetLoaded, null);

                return null;
            }

            if (!AssetDatabaseCatalog.TryGetAsyncReference(assetKey, out AssetReference reference))
            {
                Log.Warning(typeof(AssetLoader), $"Asset '{assetKey}' is not available through the async runtime-loading path.");
                InvokeOnAssetLoadedCallback(onAssetLoaded, null);

                return null;
            }

            TAsset asset = await AssetResidencyRegistry.AcquireAsync<TAsset>(reference, owner);
            InvokeOnAssetLoadedCallback(onAssetLoaded, asset);

            return asset;
        }

        /// <summary>
        /// Convenience overload that acquires explicit ownership from a serialized SS3D asset reference.
        /// </summary>
        [ItemCanBeNull]
        public static async Task<TAsset> AcquireAsync<TAsset>([NotNull] ObjectAssetReference reference, AssetOwnerToken owner, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(new AssetKey(reference.Database, reference.Id), owner, onAssetLoaded);

        /// <summary>
        /// Resolves an asset through the shared network ownership claim used by synchronized loads and late-join preloading.
        /// </summary>
        [ItemCanBeNull]
        internal static async Task<TAsset> AcquireNetworkAssetAsync<TAsset>(AssetKey assetKey, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(assetKey, NetworkAsyncOwner, onAssetLoaded);

        /// <summary>
        /// Checks whether the asset can be resolved through the async runtime-loading path used by synchronized multiplayer systems.
        /// </summary>
        public static bool Has([CanBeNull] string databaseId, [CanBeNull] string assetId)
        {
            return Has(new(databaseId, assetId));
        }

        /// <summary>
        /// Checks whether the specified asset key has an async-loadable asset reference.
        /// </summary>
        public static bool Has(AssetKey assetKey)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            return AssetDatabaseCatalog.Has(assetKey);
        }

        /// <summary>
        /// Releases the legacy compatibility ownership claim associated with the given asset reference.
        /// </summary>
        public static void Unload([NotNull] ObjectAssetReference assetReference)
        {
            Release(new AssetKey(assetReference.Database, assetReference.Id), LegacyAsyncOwner);
        }

        /// <summary>
        /// Releases the legacy compatibility ownership claim associated with the given asset key.
        /// </summary>
        public static void Unload(AssetKey assetKey)
        {
            Release(assetKey, LegacyAsyncOwner);
        }

        /// <summary>
        /// Releases the legacy compatibility ownership claim associated with the given asset GUID.
        /// This preserves the old GetAsync/Unload pairing while allowing other systems to keep the same asset resident.
        /// </summary>
        public static void Unload([NotNull] string guid)
        {
            AssetResidencyRegistry.Release(guid, LegacyAsyncOwner);
        }

        /// <summary>
        /// Releases one explicit ownership claim for the specified asset key.
        /// The shared Addressables handle is only released once the final owner is gone.
        /// </summary>
        public static bool Release(AssetKey assetKey, AssetOwnerToken owner)
        {
            if (!owner.IsValid || !EnsureInitialized())
            {
                return false;
            }

            if (!AssetDatabaseCatalog.TryGetAsyncReference(assetKey, out AssetReference reference))
            {
                return false;
            }

            return AssetResidencyRegistry.Release(reference.AssetGUID, owner);
        }

        /// <summary>
        /// Releases one explicit ownership claim for a serialized SS3D asset reference.
        /// </summary>
        public static bool Release([NotNull] ObjectAssetReference reference, AssetOwnerToken owner)
        {
            return Release(new AssetKey(reference.Database, reference.Id), owner);
        }

#if UNITY_EDITOR
        public static bool AddToAddressables(string databaseID, Object asset)
        {
            AssetDatabase database = AssetDatabaseCatalog.EditorGetDatabase(databaseID);

            if (database)
            {
                return database.AddToAddressables(asset);
            }

            Log.Error(typeof(AssetLoader), $"Database of type {databaseID} not found cannot add to addressables");

            return false;
        }
#endif

        /// <summary>
        /// Releases the shared network ownership claim used by synchronized addressable residency.
        /// </summary>
        internal static bool ReleaseNetworkAsset(AssetKey assetKey)
        {
            return Release(assetKey, NetworkAsyncOwner);
        }

        private static async Task InitializeInternalAsync()
        {
            try
            {
                await Addressables.InitializeAsync().Task;
                AssetDatabaseCatalog.Initialize();
            }
            catch (Exception e)
            {
                AssetDatabaseCatalog.Reset();
                AssetResidencyRegistry.Clear();
                Log.Error(typeof(AssetLoader), e, "An exception occurred while initializing the Addressables system.");

                throw;
            }
        }

        private static bool EnsureInitialized()
        {
            if (AssetDatabaseCatalog.IsInitialized)
            {
                return true;
            }

            LogInitializationState();

            return false;
        }

        private static async Task<bool> EnsureInitializedAsync()
        {
            if (InitializationTask == null)
            {
                LogInitializationState();

                return false;
            }

            try
            {
                await InitializationTask;

                return AssetDatabaseCatalog.IsInitialized;
            }
            catch
            {
                return false;
            }
        }

        private static void LogInitializationState()
        {
            if (InitializationTask == null)
            {
                Log.Error(typeof(AssetLoader), "AssetLoader was used before initialization started.");

                return;
            }

            if (!InitializationTask.IsCompleted)
            {
                Log.Error(typeof(AssetLoader), "AssetLoader was used while initialization is still in progress.");

                return;
            }

            if (InitializationTask.IsFaulted)
            {
                Log.Error(typeof(AssetLoader), "AssetLoader initialization previously failed.");

                return;
            }

            Log.Error(typeof(AssetLoader), "AssetLoader is not initialized.");
        }

        private static void InvokeOnAssetLoadedCallback<TAsset>([CanBeNull] Action<TAsset> onAssetLoaded, [CanBeNull] TAsset asset)
            where TAsset : class
        {
            try
            {
                onAssetLoaded?.Invoke(asset);
            }
            catch (Exception e)
            {
                Log.Error(typeof(AssetLoader), e, "An exception occurred while invoking the onAssetLoaded callback in AssetLoader");
            }
        }
    }
}