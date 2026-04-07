using Coimbra;
using Coimbra.Services.Events;
using FishNet;
using FishNet.Transporting;
using JetBrains.Annotations;
using SS3D.Application.Events;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Networking;
using SS3D.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data
{
    /// <summary>
    /// Scene-owned facade and composition root for the SS3D asset system.
    /// Owns backends, the asset provider, and manages their lifecycle.
    /// </summary>
    public class AssetSubSystem : SubSystem
    {
        internal static event Action<string, Object> OnAssetLoaded;

        internal static event Action<string> OnAssetUnloaded;

        [SerializeField]
        private NetworkBarrier _networkBarrierPrefab;

        private readonly Dictionary<AssetBackendType, IAssetBackend> _backends = new();

        private IAssetProvider _provider;
        private AssetLifecycleTracker _lifecycleTracker;
        private IAssetCatalog[] _catalogs;
        private Dictionary<string, AddressablesDatabase> _databasesById;
        private Task _initTask;

        public bool IsInitialized => _initTask is { IsCompletedSuccessfully: true };
        
        internal NetworkBarrier NetworkBarrier { get; private set; }

        [CanBeNull]
        public AddressablesDatabase GetDatabase(string databaseID)
        {
            if (_databasesById == null || !_databasesById.TryGetValue(databaseID, out AddressablesDatabase database))
            {
                return null;
            }

            return database;
        }

        public bool Has([NotNull] string assetId) => IsInitialized && _catalogs != null && _catalogs.Any(catalog => catalog.Has(assetId));

        /// <inheritdoc cref="AcquireAsync{T}(string)"/>
        [NotNull]
        internal Task<AssetHandle<T>> AcquireAsync<T>([NotNull] ObjectAssetReference reference)
            where T : class => AcquireAsync<T>(reference.Id);

        /// <summary>
        /// Acquires a ref-counted handle for an asset by GUID.
        /// The system auto-routes to the correct backend via registered catalogs.
        /// </summary>
        internal async Task<AssetHandle<T>> AcquireAsync<T>([NotNull] string guid)
            where T : class
        {
            if (!IsInitialized || _catalogs == null)
            {
                return null;
            }

            foreach (IAssetCatalog catalog in _catalogs)
            {
                if (!catalog.Has(guid))
                {
                    continue;
                }

                string resolvedKey = catalog.ResolveKey(guid);

                if (!_backends.TryGetValue(catalog.BackendType, out IAssetBackend backend))
                {
                    Log.Error(this, "No backend registered for {BackendType} while resolving GUID '{Guid}'.", Logs.Important, catalog.BackendType, guid);

                    return null;
                }

                _lifecycleTracker.TrackAcquire(resolvedKey);

                try
                {
                    return await _provider.AcquireAsync<T>(resolvedKey, backend);
                }
                catch
                {
                    _lifecycleTracker.TrackRelease(resolvedKey);

                    throw;
                }
            }

            Log.Warning(this, "No catalog contains GUID '{Guid}'.", Logs.Important, guid);

            return null;
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            ApplicationInitializing.AddListener(HandleApplicationInitializing);
        }

        protected override void OnDestroyed()
        {
            if (InstanceFinder.ServerManager != null)
            {
                InstanceFinder.ServerManager.OnServerConnectionState -= HandleServerConnectionState;
            }

            _lifecycleTracker?.Shutdown();
            _lifecycleTracker = null;

            if (_provider != null)
            {
                _provider.OnLoaded -= RelayAssetLoaded;
                _provider.OnUnloaded -= RelayAssetUnloaded;
                _provider.Dispose();
            }

            foreach (IAssetBackend backend in _backends.Values)
            {
                backend.Dispose();
            }

            _backends.Clear();
            _catalogs = null;
            _databasesById = null;
            base.OnDestroyed();
        }

        private static void RelayAssetLoaded(string key, Object asset) => OnAssetLoaded?.Invoke(key, asset);

        private static void RelayAssetUnloaded(string key) => OnAssetUnloaded?.Invoke(key);

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

                return;
            }

            if (InstanceFinder.SceneManager)
            {
                if (InstanceFinder.ServerManager.Started)
                {
                    SpawnNetworkBarrier();
                }
                else
                {
                    InstanceFinder.ServerManager.OnServerConnectionState += HandleServerConnectionState;
                }
            }
            else
            {
                Log.Error(this, "No SceneManager found. Cannot determine when to spawn NetworkBarrier.");
            }
        }

        private void HandleServerConnectionState(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState != LocalConnectionState.Started)
            {
                return;
            }

            InstanceFinder.ServerManager.OnServerConnectionState -= HandleServerConnectionState;
            SpawnNetworkBarrier();
        }

        private void SpawnNetworkBarrier()
        {
            NetworkBarrier = Instantiate(_networkBarrierPrefab);
            NetworkSpawner.Spawn(NetworkBarrier);
        }

        private async Task InitializeInternalAsync()
        {
            try
            {
                AddressablesBackend addressablesBackend = new();
                await addressablesBackend.InitializeAsync();
                _backends[AssetBackendType.Addressables] = addressablesBackend;

                AssetLifecycleTracker tracker = null;
                _provider = new AssetProvider(releaseCallback: key => tracker!.TrackRelease(key));
                tracker = new(_provider);
                _lifecycleTracker = tracker;

                _provider.OnLoaded += RelayAssetLoaded;
                _provider.OnUnloaded += RelayAssetUnloaded;

                List<AddressablesDatabase> assetDatabases = ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedAssetDatabases;

                _databasesById = new(assetDatabases.Count);

                foreach (AddressablesDatabase database in assetDatabases)
                {
                    _databasesById[database.DatabaseID] = database;
                }

                AddressablesCatalog addressablesCatalog = new();
                addressablesCatalog.Initialize(assetDatabases.Cast<IAssetDatabase>().ToArray());

                _catalogs = new IAssetCatalog[]
                {
                    addressablesCatalog
                };

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

                _lifecycleTracker?.Shutdown();
                _lifecycleTracker = null;
                _catalogs = null;
                _databasesById = null;
                Log.Error(this, e, "An exception occurred while initializing the asset system.");

                throw;
            }
        }
    }
}