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
    /// Scene-owned facade and composition root for the SS3D asset system.
    /// Owns backends, the asset store, and manages their lifecycle.
    /// </summary>
    public class AssetSubSystem : SubSystem
    {
        // ── New system ──────────────────────────────────────────────────

        private IAssetStore _store;
        private readonly Dictionary<AssetBackendType, IAssetBackend> _backends = new();

        /// <summary>
        /// Raised after an asset is loaded for the first time via the store.
        /// </summary>
        public event Action<string, Object> OnStoreAssetLoaded
        {
            add { if (_store != null) _store.OnLoaded += value; }
            remove { if (_store != null) _store.OnLoaded -= value; }
        }

        /// <summary>
        /// Raised after an asset's last handle is disposed via the store.
        /// </summary>
        public event Action<string> OnStoreAssetUnloaded
        {
            add { if (_store != null) _store.OnUnloaded += value; }
            remove { if (_store != null) _store.OnUnloaded -= value; }
        }

        /// <summary>
        /// Acquires a ref-counted handle for an asset via the specified backend.
        /// The key format depends on the backend: GUID for Addressables, path for Resources, filepath for File.
        /// </summary>
        [ItemCanBeNull]
        public Task<AssetHandle<T>> AcquireAsync<T>(
            [NotNull] string key,
            AssetBackendType backendType = AssetBackendType.Addressables)
            where T : class
        {
            if (!IsInitialized)
            {
                return Task.FromResult<AssetHandle<T>>(null);
            }

            if (!_backends.TryGetValue(backendType, out IAssetBackend backend))
            {
                return Task.FromResult<AssetHandle<T>>(null);
            }

            return _store.AcquireAsync<T>(key, backend);
        }

        /// <inheritdoc cref="AcquireAsync{T}(string, AssetBackendType)"/>
        [ItemCanBeNull]
        public Task<AssetHandle<T>> AcquireAsync<T>(
            [NotNull] ObjectAssetReference reference,
            AssetBackendType backendType = AssetBackendType.Addressables)
            where T : class
            => AcquireAsync<T>(reference.Id, backendType);

        protected override void OnDestroyed()
        {
            _store?.Dispose();

            foreach (IAssetBackend backend in _backends.Values)
            {
                backend.Dispose();
            }

            _backends.Clear();
            base.OnDestroyed();
        }

        // ── Unified static events (fired by both legacy and new systems) ──

        private static Action<string, Object> _onAssetLoaded;
        private static Action<string> _onAssetUnloaded;

        internal static event Action<string, Object> OnAssetLoaded
        {
            add => _onAssetLoaded += value;
            remove => _onAssetLoaded -= value;
        }

        internal static event Action<string> OnAssetUnloaded
        {
            add => _onAssetUnloaded += value;
            remove => _onAssetUnloaded -= value;
        }

        // ── Legacy system (to be removed in migration step) ─────────

        private static readonly AssetOwnerToken LegacyAsyncOwner = AssetOwnerToken.Create("AssetSubSystem.LegacyAsync");
        private static readonly AssetOwnerToken NetworkAsyncOwner = AssetOwnerToken.Create("AssetSubSystem.Network");

        public bool IsInitialized => AssetDatabaseCatalog.IsInitialized && InitializationTask is { IsCompletedSuccessfully: true };

        private Task InitializationTask { get; set; }

        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationInitializing.AddListener(HandleApplicationInitializing);
        }

        public bool IsLoaded([NotNull] string guid) => AssetResidencyRegistry.HasRecord(guid) && AssetLoader.IsLoaded(guid);

        [CanBeNull]
        public AssetDatabase GetDatabase(string databaseID) => AssetDatabaseCatalog.GetDatabase(databaseID);

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

        [ItemCanBeNull]
        public async Task<TAsset> AcquireAsync<TAsset>([NotNull] ObjectAssetReference reference, AssetOwnerToken owner, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(new AssetKey(reference.Database, reference.Id), owner, onAssetLoaded);

        public bool Has([CanBeNull] string databaseId, [CanBeNull] string assetId) => Has(new(databaseId, assetId));

        public bool Has(AssetKey assetKey) => EnsureInitialized() && AssetDatabaseCatalog.Has(assetKey);

        public void Unload([NotNull] ObjectAssetReference assetReference) => Release(new AssetKey(assetReference.Database, assetReference.Id), LegacyAsyncOwner);

        public void Unload(AssetKey assetKey) => Release(assetKey, LegacyAsyncOwner);

        public void Unload([NotNull] string guid) => AssetResidencyRegistry.Release(guid, LegacyAsyncOwner);

        public bool Release(AssetKey assetKey, AssetOwnerToken owner)
        {
            if (!owner.IsValid || !EnsureInitialized())
            {
                return false;
            }

            return AssetDatabaseCatalog.TryGetAsyncReference(assetKey, out AssetReference reference) && AssetResidencyRegistry.Release(reference, owner);
        }

        public bool Release([NotNull] ObjectAssetReference reference, AssetOwnerToken owner) => Release(new AssetKey(reference.Database, reference.Id), owner);

        [ItemCanBeNull]
        internal async Task<TAsset> AcquireNetworkAssetAsync<TAsset>(AssetKey assetKey, [CanBeNull] Action<TAsset> onAssetLoaded = null)
            where TAsset : class => await AcquireAsync(assetKey, NetworkAsyncOwner, onAssetLoaded);

        internal bool ReleaseNetworkAsset(AssetKey assetKey) => Release(assetKey, NetworkAsyncOwner);

        private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            Log.Information(this, "Loading asset databases", Logs.Important);
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                if (InitializationTask == null || InitializationTask.IsCanceled || InitializationTask.IsFaulted)
                {
                    InitializationTask = InitializeInternalAsync();
                }

                await InitializationTask;
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
                AddressablesBackend addressablesBackend = new();
                await addressablesBackend.InitializeAsync();
                _backends[AssetBackendType.Addressables] = addressablesBackend;

                _store = new AssetStore();

                // Bridge both event sources into the unified static events.
                AssetLoader.OnAssetLoaded += (key, obj) => _onAssetLoaded?.Invoke(key, obj);
                AssetLoader.OnAssetUnloaded += key => _onAssetUnloaded?.Invoke(key);
                _store.OnLoaded += (key, obj) => _onAssetLoaded?.Invoke(key, obj);
                _store.OnUnloaded += key => _onAssetUnloaded?.Invoke(key);

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