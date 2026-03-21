using Coimbra;
using Coimbra.Services.Events;
using JetBrains.Annotations;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private IAssetCatalog[] _catalogs;
        private Dictionary<string, AssetDatabase> _databasesById;
        private Task _initTask;

        // Static reference to the active store so that static event accessors (needed by
        // ScriptableObjects like NetworkObjects that cannot hold instance references) can
        // forward subscriptions directly without a separate delegate or bridge.
        private static IAssetStore ActiveStore;

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
        public AssetDatabase GetDatabase(string databaseID)
        {
            if (_databasesById == null || !_databasesById.TryGetValue(databaseID, out AssetDatabase database))
            {
                return null;
            }

            return database;
        }

        [CanBeNull]
        public TAsset Get<TAsset>([NotNull] string databaseId, [NotNull] string assetId)
            where TAsset : Object => GetDatabase(databaseId)?.Get<TAsset>(assetId);

        public bool Has([NotNull] string assetId) => IsInitialized && _catalogs != null && _catalogs.Any(catalog => catalog.Has(assetId));

        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationInitializing.AddListener(HandleApplicationInitializing);
        }

        protected override void OnDestroyed()
        {
            ActiveStore = null;
            _store?.Dispose();

            foreach (IAssetBackend backend in _backends.Values)
            {
                backend.Dispose();
            }

            _backends.Clear();
            _catalogs = null;
            _databasesById = null;
            base.OnDestroyed();
        }

        // ── Static events ──────────────────────────────────────────────

        internal static event Action<string, Object> OnAssetLoaded
        {
            add { if (ActiveStore != null) ActiveStore.OnLoaded += value; }
            remove { if (ActiveStore != null) ActiveStore.OnLoaded -= value; }
        }

        internal static event Action<string> OnAssetUnloaded
        {
            add { if (ActiveStore != null) ActiveStore.OnUnloaded += value; }
            remove { if (ActiveStore != null) ActiveStore.OnUnloaded -= value; }
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
                ActiveStore = _store;

                List<AssetDatabase> assetDatabases = ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedAssetDatabases;

                _databasesById = new Dictionary<string, AssetDatabase>(assetDatabases.Count);
                foreach (AssetDatabase database in assetDatabases)
                {
                    _databasesById[database.DatabaseID] = database;
                }

                AddressablesCatalog addressablesCatalog = new();
                addressablesCatalog.Initialize(assetDatabases.Cast<IAssetDatabase>().ToArray());
                _catalogs = new IAssetCatalog[] { addressablesCatalog };

                Log.Information(this, "{Count} asset databases initialized.", Logs.Important, assetDatabases.Count);
            }
            catch (Exception e)
            {
                if (_catalogs != null)
                {
                    foreach (IAssetCatalog catalog in _catalogs)
                    {
                        if (catalog is AddressablesCatalog addressablesCatalog)
                        {
                            addressablesCatalog.Reset();
                        }
                    }
                }

                _catalogs = null;
                _databasesById = null;
                Log.Error(this, e, "An exception occurred while initializing the asset system.");

                throw;
            }
        }
    }
}