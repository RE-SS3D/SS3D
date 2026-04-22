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

        private readonly Dictionary<AssetBackendType, IAssetBackend> _backends = new();

        [SerializeField]
        private NetworkBarrier _networkBarrierPrefab;

        private IAssetProvider _provider;
        private AssetLifecycleTracker _lifecycleTracker;
        private AssetCatalog[] _catalogs;
        private Dictionary<string, AssetDatabase> _databasesById;
        private Task _initTask;

        public bool IsInitialized => _initTask is { IsCompletedSuccessfully: true };

        internal NetworkBarrier NetworkBarrier { get; private set; }

        [CanBeNull]
        public AssetDatabase GetDatabase(string databaseID)
        {
            if (_databasesById == null || !_databasesById.TryGetValue(databaseID, out AssetDatabase database))
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

            foreach (AssetCatalog catalog in _catalogs)
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

                _lifecycleTracker.TrackAcquire(guid);

                try
                {
                    return await _provider.AcquireAsync<T>(guid, resolvedKey, backend);
                }
                catch
                {
                    _lifecycleTracker.TrackRelease(guid);

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
            if (InstanceFinder.ServerManager)
            {
                InstanceFinder.ServerManager.OnServerConnectionState -= HandleServerConnectionState;
            }

            _lifecycleTracker?.Shutdown();
            _lifecycleTracker = null;

            foreach (IAssetBackend backend in _backends.Values)
            {
                backend.OnLoaded -= RelayAssetLoaded;
                backend.OnUnloaded -= RelayAssetUnloaded;
                backend.Dispose();
            }

            _provider?.Dispose();

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
                List<AssetCatalog> catalogs = ScriptableSettings.GetOrFind<AssetDatabaseSettings>().IncludedCatalogs;

                if (catalogs == null || catalogs.Count == 0)
                {
                    Log.Error(this, "No catalogs are registered in AssetDatabaseSettings.IncludedCatalogs.", Logs.Important);

                    return;
                }

                AssetLifecycleTracker tracker = null;
                _provider = new AssetProvider(releaseCallback: key => tracker!.TrackRelease(key));
                tracker = new(_provider);
                _lifecycleTracker = tracker;

                _databasesById = new();

                foreach (AssetCatalog catalog in catalogs.Where(catalog => catalog))
                {
                    if (!_backends.ContainsKey(catalog.BackendType))
                    {
                        IAssetBackend backend = catalog.CreateBackend();
                        await backend.InitializeAsync();
                        backend.OnLoaded += RelayAssetLoaded;
                        backend.OnUnloaded += RelayAssetUnloaded;
                        _backends[catalog.BackendType] = backend;
                    }

                    foreach (AssetDatabase database in catalog.Databases.Where(database => database))
                    {
                        _databasesById[database.DatabaseID] = database;
                    }
                }

                _catalogs = catalogs.ToArray();

                Log.Information(this, "{Count} asset databases initialized.", Logs.Important, _databasesById.Count);
            }
            catch (Exception e)
            {
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