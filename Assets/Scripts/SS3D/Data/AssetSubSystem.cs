using Coimbra;
using Coimbra.Services.Events;
using JetBrains.Annotations;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Scene-owned facade for the SS3D asset system.
    /// It owns initialization and coordinates catalog lookup, ownership-aware addressable loading,
    /// and temporary compatibility behavior while callers migrate away from <see cref="AssetLoader"/>.
    /// </summary>
    public class AssetSubSystem : SubSystem
    {
        internal static event Action<string, Object> OnAssetLoaded
        {
            add => AssetLoader.OnAssetLoaded += value;
            remove => AssetLoader.OnAssetLoaded -= value;
        }

        internal static event Action<string> OnAssetUnloaded
        {
            add => AssetLoader.OnAssetUnloaded += value;
            remove => AssetLoader.OnAssetUnloaded -= value;
        }

        private static readonly AssetOwnerToken LegacyAsyncOwner = AssetOwnerToken.Create("AssetSubSystem.LegacyAsync");
        private static readonly AssetOwnerToken NetworkAsyncOwner = AssetOwnerToken.Create("AssetSubSystem.Network");

        public static AssetSubSystem Instance { get; private set; }

        // TODO: Replace AssetLoader.IsInitialized callers with AssetSubSystem.IsInitialized.
        public bool IsInitialized => AssetDatabaseCatalog.IsInitialized && InitializationTask is { IsCompletedSuccessfully: true };

        public Task InitializationTask { get; private set; }

#if UNITY_EDITOR
        public static bool AddToAddressables(string databaseID, Object asset)
        {
            AssetDatabase database = AssetDatabaseCatalog.EditorGetDatabase(databaseID);

            if (database)
            {
                return database.AddToAddressables(asset);
            }

            Log.Error(typeof(AssetSubSystem), $"Database of type {databaseID} not found cannot add to addressables");

            return false;
        }
#endif

        protected override void OnAwake()
        {
            base.OnAwake();

            if (Instance && Instance != this)
            {
                Log.Error(this, $"Multiple instances of {nameof(AssetSubSystem)} detected. Destroying the new one.");
                GameObject.Dispose(true);

                return;
            }

            Instance = this;
            ApplicationInitializing.AddListener(HandleApplicationInitializing);
        }

        protected override void OnDestroyed()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            base.OnDestroyed();
        }

        // TODO: Replace AssetLoader.InitializeAsync() callers with AssetSubSystem.InitializeAsync().
        [NotNull]
        public Task InitializeAsync()
        {
            if (InitializationTask == null || InitializationTask.IsCanceled || InitializationTask.IsFaulted)
            {
                InitializationTask = InitializeInternalAsync();
            }

            return InitializationTask;
        }

        // TODO: Replace AssetLoader.IsLoaded(string) callers with AssetSubSystem.IsLoaded(string).
        public bool IsLoaded([NotNull] string guid) => AssetResidencyRegistry.IsLoaded(guid);

        // TODO: Replace AssetLoader.Get(...) callers with AssetSubSystem.Get(...).
        [CanBeNull]
        public TAsset Get<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object => AssetDatabaseCatalog.GetDatabase(databaseId)?.Get<TAsset>(assetId);

        // TODO: Replace AssetLoader.GetAsync(...) callers with AssetSubSystem.GetAsync(...).
        [ItemCanBeNull]
        public async Task<TAsset> GetAsync<TAsset>([NotNull] string databaseId, [NotNull] string assetId, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(new AssetKey(databaseId, assetId), LegacyAsyncOwner, onAssetLoaded);

        // TODO: Replace AssetLoader.GetAsync(...) callers with AssetSubSystem.GetAsync(...).
        [ItemCanBeNull]
        public async Task<TAsset> GetAsync<TAsset>(AssetKey assetKey, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(assetKey, LegacyAsyncOwner, onAssetLoaded);

        // TODO: Replace AssetLoader.GetAsync(...) callers with AssetSubSystem.GetAsync(...).
        [ItemCanBeNull]
        public async Task<TAsset> GetAsync<TAsset>([NotNull] ObjectAssetReference reference)
            where TAsset : class => await AcquireAsync<TAsset>(new AssetKey(reference.Database, reference.Id), LegacyAsyncOwner);

        // TODO: Replace AssetLoader.AcquireAsync(...) callers with AssetSubSystem.AcquireAsync(...).
        [ItemCanBeNull]
        public async Task<TAsset> AcquireAsync<TAsset>(AssetKey assetKey, AssetOwnerToken owner, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class
        {
            if (!owner.IsValid)
            {
                Log.Warning(this, $"Cannot acquire asset '{assetKey}' because the owner token is invalid.");
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
                Log.Warning(this, $"Asset '{assetKey}' is not available through the async runtime-loading path.");
                InvokeOnAssetLoadedCallback(onAssetLoaded, null);

                return null;
            }

            TAsset asset = await AssetResidencyRegistry.AcquireAsync<TAsset>(reference, owner);
            InvokeOnAssetLoadedCallback(onAssetLoaded, asset);

            return asset;
        }

        // TODO: Replace AssetLoader.AcquireAsync(...) callers with AssetSubSystem.AcquireAsync(...).
        [ItemCanBeNull]
        public async Task<TAsset> AcquireAsync<TAsset>([NotNull] ObjectAssetReference reference, AssetOwnerToken owner, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(new AssetKey(reference.Database, reference.Id), owner, onAssetLoaded);

        // TODO: Replace AssetLoader.Has(...) callers with AssetSubSystem.Has(...).
        public bool Has([CanBeNull] string databaseId, [CanBeNull] string assetId) => Has(new(databaseId, assetId));

        // TODO: Replace AssetLoader.Has(...) callers with AssetSubSystem.Has(...).
        public bool Has(AssetKey assetKey)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            return AssetDatabaseCatalog.Has(assetKey);
        }

        // TODO: Replace AssetLoader.Unload(...) callers with AssetSubSystem.Unload(...).
        public void Unload([NotNull] ObjectAssetReference assetReference)
        {
            Release(new AssetKey(assetReference.Database, assetReference.Id), LegacyAsyncOwner);
        }

        // TODO: Replace AssetLoader.Unload(...) callers with AssetSubSystem.Unload(...).
        public void Unload(AssetKey assetKey)
        {
            Release(assetKey, LegacyAsyncOwner);
        }

        // TODO: Replace AssetLoader.Unload(...) callers with AssetSubSystem.Unload(...).
        public void Unload([NotNull] string guid)
        {
            AssetResidencyRegistry.Release(guid, LegacyAsyncOwner);
        }

        // TODO: Replace AssetLoader.Release(...) callers with AssetSubSystem.Release(...).
        public bool Release(AssetKey assetKey, AssetOwnerToken owner)
        {
            if (!owner.IsValid || !EnsureInitialized())
            {
                return false;
            }

            if (!AssetDatabaseCatalog.TryGetAsyncReference(assetKey, out AssetReference reference))
            {
                return false;
            }

            return AssetResidencyRegistry.Release(reference, owner);
        }

        // TODO: Replace AssetLoader.Release(...) callers with AssetSubSystem.Release(...).
        public bool Release([NotNull] ObjectAssetReference reference, AssetOwnerToken owner)
        {
            return Release(new AssetKey(reference.Database, reference.Id), owner);
        }

        // TODO: Replace AssetLoader.AcquireNetworkAssetAsync(...) callers with AssetSubSystem.AcquireNetworkAssetAsync(...).
        [ItemCanBeNull]
        internal async Task<TAsset> AcquireNetworkAssetAsync<TAsset>(AssetKey assetKey, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(assetKey, NetworkAsyncOwner, onAssetLoaded);

        // TODO: Replace AssetLoader.ReleaseNetworkAsset(...) callers with AssetSubSystem.ReleaseNetworkAsset(...).
        internal bool ReleaseNetworkAsset(AssetKey assetKey) => Release(assetKey, NetworkAsyncOwner);

        private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            Log.Information(this, "Loading asset databases", Logs.Important);
            InitializeAssetsAsync();
        }

        private async void InitializeAssetsAsync()
        {
            try
            {
                await InitializeAsync();
            }
            catch (Exception exception)
            {
                Log.Error(this, exception, "Asset system initialization failed during application startup.");
            }
        }

        private async Task InitializeInternalAsync()
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
                Log.Error(this, e, "An exception occurred while initializing the asset system.");

                throw;
            }
        }

        private bool EnsureInitialized()
        {
            if (AssetDatabaseCatalog.IsInitialized)
            {
                return true;
            }

            LogInitializationState();

            return false;
        }

        private async Task<bool> EnsureInitializedAsync()
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

        private void LogInitializationState()
        {
            if (InitializationTask == null)
            {
                Log.Error(this, $"{nameof(AssetSubSystem)} was used before initialization started.");

                return;
            }

            if (!InitializationTask.IsCompleted)
            {
                Log.Error(this, $"{nameof(AssetSubSystem)} was used while initialization is still in progress.");

                return;
            }

            if (InitializationTask.IsFaulted)
            {
                Log.Error(this, $"{nameof(AssetSubSystem)} initialization previously failed.");

                return;
            }

            Log.Error(this, $"{nameof(AssetSubSystem)} is not initialized.");
        }

        private void InvokeOnAssetLoadedCallback<TAsset>([CanBeNull] Action<TAsset> onAssetLoaded, [CanBeNull] TAsset asset)
            where TAsset : class
        {
            try
            {
                onAssetLoaded?.Invoke(asset);
            }
            catch (Exception e)
            {
                Log.Error(this, e, "An exception occurred while invoking the onAssetLoaded callback in AssetSubSystem.");
            }
        }
    }
}
