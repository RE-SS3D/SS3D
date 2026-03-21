using Coimbra.Services.Events;
using JetBrains.Annotations;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Scene-owned facade and composition root for the SS3D asset system.
    /// Owns backends, the asset store, and manages their lifecycle.
    /// </summary>
    public class AssetSubSystem : SubSystem
    {
        private IAssetStore _store;
        private readonly Dictionary<AssetBackendType, IAssetBackend> _backends = new();
        private Task _initTask;

        // Static reference to the active store so that static event accessors (needed by
        // ScriptableObjects like NetworkObjects that cannot hold instance references) can
        // forward subscriptions directly without a separate delegate or bridge.
        private static IAssetStore _activeStore;

        public bool IsInitialized => _initTask is { IsCompletedSuccessfully: true };

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

        [CanBeNull]
        public AssetDatabase GetDatabase(string databaseID) => AssetDatabaseCatalog.GetDatabase(databaseID);

        [CanBeNull]
        public TAsset Get<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object => AssetDatabaseCatalog.GetDatabase(databaseId)?.Get<TAsset>(assetId);

        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationInitializing.AddListener(HandleApplicationInitializing);
        }

        protected override void OnDestroyed()
        {
            _activeStore = null;
            _store?.Dispose();

            foreach (IAssetBackend backend in _backends.Values)
            {
                backend.Dispose();
            }

            _backends.Clear();
            base.OnDestroyed();
        }

        // ── Static events ──────────────────────────────────────────────

        internal static event Action<string, Object> OnAssetLoaded
        {
            add { if (_activeStore != null) _activeStore.OnLoaded += value; }
            remove { if (_activeStore != null) _activeStore.OnLoaded -= value; }
        }

        internal static event Action<string> OnAssetUnloaded
        {
            add { if (_activeStore != null) _activeStore.OnUnloaded += value; }
            remove { if (_activeStore != null) _activeStore.OnUnloaded -= value; }
        }

        // ── Initialization ─────────────────────────────────────────────

        private void HandleApplicationInitializing(ref EventContext context, in ApplicationInitializing e)
        {
            Log.Information(this, "Loading asset databases", Logs.Important);
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            try
            {
                if (_initTask == null || _initTask.IsCanceled || _initTask.IsFaulted)
                {
                    _initTask = InitializeInternalAsync();
                }

                await _initTask;
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
                _activeStore = _store;

                AssetDatabaseCatalog.Initialize();
            }
            catch (Exception e)
            {
                AssetDatabaseCatalog.Reset();
                Log.Error(this, e, "An exception occurred while initializing the asset system.");

                throw;
            }
        }
    }
}