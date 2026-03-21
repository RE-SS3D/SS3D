using Coimbra;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using FishNet.Transporting;
using JetBrains.Annotations;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SS3D.Data.Networking
{
    /// <summary>
    /// Coordinates asset loading across the network using the new handle-based asset system.
    /// The server owns synchronized load barriers, clients report local load results,
    /// and late joiners are instructed to preload assets that are still active in the world.
    /// Backend-agnostic: RPCs carry the <see cref="AssetBackendType"/> so clients use the correct backend.
    /// </summary>
    [RequireComponent(typeof(NetworkObserver))]
    internal sealed class NetworkBarrier : NetworkActor
    {
        /// <summary>
        /// Tracks one in-flight synchronized load and the clients that still need to acknowledge it.
        /// </summary>
        private sealed class LoadBarrier
        {
            private bool _failed;
            private TaskCompletionSource<bool> _taskSource;
            private HashSet<int> _pendingClientIds;

            internal AssetBackendType BackendType { get; }

            internal LoadBarrier([NotNull] HashSet<int> pendingClientIds, AssetBackendType backendType)
            {
                _taskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingClientIds = pendingClientIds ?? new HashSet<int>();
                BackendType = backendType;

                if (_pendingClientIds.Count == 0)
                {
                    _taskSource.TrySetResult(true);
                }
            }

            internal Task<bool> Task => _taskSource.Task;

            internal void AddClient(int clientId)
            {
                if (Task.IsCompleted)
                {
                    _taskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                _pendingClientIds.Add(clientId);
            }

            internal void RemoveClient(int clientId)
            {
                if (_pendingClientIds.Remove(clientId))
                {
                    UpdateState();
                }
            }

            internal void Acknowledge(int clientId, bool loaded)
            {
                if (loaded)
                {
                    _pendingClientIds.Remove(clientId);
                }
                else
                {
                    _failed = true;
                }

                UpdateState();
            }

            internal bool TrySetCanceled() => _taskSource.TrySetCanceled();

            private void UpdateState()
            {
                if (_failed)
                {
                    _taskSource.TrySetResult(false);
                }

                if (_pendingClientIds.Count == 0)
                {
                    _taskSource.TrySetResult(true);
                }
            }
        }

        /// <summary>
        /// Tracks the one-time late-join preload snapshot for a specific client.
        /// </summary>
        private sealed class PreloadSession
        {
            private readonly HashSet<string> _pendingKeys;
            private readonly TaskCompletionSource<bool> _taskSource;
            private bool? _result;

            internal PreloadSession([CanBeNull] HashSet<string> pendingKeys)
            {
                _pendingKeys = pendingKeys ?? new HashSet<string>();
                _taskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

                if (_pendingKeys.Count == 0)
                {
                    Complete(true);
                }
            }

            internal Task<bool> Task => _taskSource.Task;

            internal int PendingCount => _pendingKeys.Count;

            internal bool Succeeded => _result == true;

            internal bool TryAcknowledge(string key, bool loaded)
            {
                if (Task.IsCompleted || !_pendingKeys.Contains(key))
                {
                    return false;
                }

                if (!loaded)
                {
                    Complete(false);

                    return true;
                }

                _pendingKeys.Remove(key);

                if (_pendingKeys.Count == 0)
                {
                    Complete(true);
                }

                return true;
            }

            internal bool TrySetCanceled() => _taskSource.TrySetCanceled();

            internal bool TrySetFailed()
            {
                if (Task.IsCompleted)
                {
                    return false;
                }

                Complete(false);

                return true;
            }

            private void Complete(bool result)
            {
                if (Task.IsCompleted)
                {
                    return;
                }

                _result = result;
                _taskSource.TrySetResult(result);
            }
        }

        private const float ClientWaitResponseGraceSeconds = 1f;

        /// <summary>
        /// Server-side synchronized load barriers keyed by asset key.
        /// </summary>
        private readonly Dictionary<string, LoadBarrier> _loadBarriers = new();

        /// <summary>
        /// Server-side late-join preload sessions keyed by client ID.
        /// </summary>
        private readonly Dictionary<int, PreloadSession> _preloadSessions = new();

        /// <summary>
        /// Tracks clients that completed their most recent late-join preload session successfully.
        /// </summary>
        private readonly HashSet<int> _clientsWithCompletedPreload = new();

        /// <summary>
        /// Client-side awaiters used when a client asks the server for the result of an in-flight load barrier.
        /// </summary>
        private readonly Dictionary<string, TaskCompletionSource<bool>> _clientWaitRequests = new();

        /// <summary>
        /// Client-side handles kept alive to maintain asset residency while spawned instances exist.
        /// </summary>
        private readonly Dictionary<string, IAssetHandle> _clientHandles = new();

        [UnityEngine.SerializeField]
        private int _retryAttempts = 5;

        [UnityEngine.SerializeField]
        private float _lateJoinPreloadTimeoutSeconds = 15f;

        public static NetworkBarrier Instance { get; private set; }

        private WorldTracker _worldTracker;

        internal WorldTracker WorldTracker => _worldTracker;

        // ── Lifecycle ────────────────────────────────────────────────────

        protected override void OnAwake()
        {
            base.OnAwake();

            if (Instance)
            {
                Log.Error(this, $"Multiple instances of {nameof(NetworkBarrier)} detected. Destroying the new one.");
                GameObject.Dispose(true);

                return;
            }

            Instance = this;
        }

        protected override void OnDestroyed()
        {
            if (ReferenceEquals(Instance, this))
            {
                Instance = null;
            }

            base.OnDestroyed();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public override void OnStartServer()
        {
            base.OnStartServer();

            _worldTracker = new WorldTracker(SubSystems.Get<AssetSubSystem>());
            _worldTracker.OnLastInstanceDestroyed += HandleLastInstanceDestroyed;

            SceneManager.OnClientLoadedStartScenes += HandleClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();

            SceneManager.OnClientLoadedStartScenes -= HandleClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;

            _worldTracker.OnLastInstanceDestroyed -= HandleLastInstanceDestroyed;
            _worldTracker.Shutdown();
            _worldTracker = null;

            foreach (LoadBarrier barrier in _loadBarriers.Values)
            {
                barrier.TrySetCanceled();
            }

            foreach (PreloadSession session in _preloadSessions.Values)
            {
                session.TrySetCanceled();
            }

            _loadBarriers.Clear();
            _preloadSessions.Clear();
            _clientsWithCompletedPreload.Clear();
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            foreach (TaskCompletionSource<bool> source in _clientWaitRequests.Values)
            {
                source.TrySetCanceled();
            }

            _clientWaitRequests.Clear();

            foreach (IAssetHandle handle in _clientHandles.Values)
            {
                handle?.Dispose();
            }

            _clientHandles.Clear();
        }

        // ── Internal API ─────────────────────────────────────────────────

        /// <summary>
        /// Ensures an asset is loaded on every connected client before dependent server logic proceeds.
        /// Servers create or join the authoritative barrier, while clients await that barrier through the server.
        /// </summary>
        internal async Task<bool> EnsureAllClientsReadyAsync(string key, AssetBackendType backendType, float timeoutSeconds = 15f)
        {
            if (!IsServer)
            {
                return await WaitForLoadClientAsync(key, timeoutSeconds);
            }

            StartSynchronizedLoad(key, backendType);

            return await WaitForLoadServerAsync(key, timeoutSeconds);
        }

        /// <summary>
        /// Returns <see langword="true"/> once the latest late-join preload session for the specified client
        /// completed successfully.
        /// </summary>
        [Server]
        internal bool IsClientPreloadComplete(int clientId) => _clientsWithCompletedPreload.Contains(clientId);

        /// <summary>
        /// Waits for the specified client's late-join preload snapshot to finish.
        /// </summary>
        [Server]
        internal async Task<bool> WaitForClientPreloadAsync(int clientId, float timeoutSeconds = -1f)
        {
            if (!_preloadSessions.TryGetValue(clientId, out PreloadSession session))
            {
                return _clientsWithCompletedPreload.Contains(clientId);
            }

            float effectiveTimeout = timeoutSeconds > 0f ? timeoutSeconds : _lateJoinPreloadTimeoutSeconds;

            try
            {
                return await session.Task.WaitWithTimeout(effectiveTimeout);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (TimeoutException)
            {
                Log.Error(this, $"Timed out waiting for late-join preload for ClientID {clientId} after {effectiveTimeout:0.##} seconds.");
                session.TrySetFailed();

                return false;
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Unexpected exception while waiting for late-join preload for ClientID {clientId}.");
                session.TrySetFailed();

                return false;
            }
            finally
            {
                TryFinalizePreloadSession(clientId, session);
            }
        }

        /// <summary>
        /// Broadcasts a network-wide asset release once the asset falls out of the active world.
        /// </summary>
        internal void BroadcastUnload(string key)
        {
            RpcUnloadAsset(key);
            ReleaseLocalHandle(key);
        }

        // ── Server-side synchronized load ────────────────────────────────

        [Server]
        private void StartSynchronizedLoad(string key, AssetBackendType backendType)
        {
            if (string.IsNullOrEmpty(key))
            {
                Log.Warning(this, "Ignoring invalid synchronize request with null/empty key.");

                return;
            }

            if (!TryRegisterLoadBarrier(key, backendType))
            {
                return;
            }

            RpcLoadAsset(key, (byte)backendType);
        }

        [Server]
        private async Task<bool> WaitForLoadServerAsync(string key, float timeoutSeconds)
        {
            if (string.IsNullOrEmpty(key))
            {
                Log.Error(this, "Cannot wait for load with null/empty key.");

                return false;
            }

            if (!_loadBarriers.TryGetValue(key, out LoadBarrier barrier))
            {
                Log.Error(this, $"No load barrier found for '{key}' on server.");

                return false;
            }

            try
            {
                return await barrier.Task.WaitWithTimeout(timeoutSeconds);
            }
            catch (TimeoutException)
            {
                Log.Error(this, $"Timed out waiting for load barrier '{key}' after {timeoutSeconds:0.##} seconds.");
                _loadBarriers.Remove(key);

                return false;
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed while waiting for load barrier '{key}'.");
                _loadBarriers.Remove(key);

                return false;
            }
            finally
            {
                if (barrier.Task.IsCompleted)
                {
                    _loadBarriers.Remove(key);
                }
            }
        }

        private bool TryRegisterLoadBarrier(string key, AssetBackendType backendType)
        {
            if (_loadBarriers.ContainsKey(key))
            {
                return false;
            }

            LoadBarrier barrier = new(GetConnectedClientIds(), backendType);
            _loadBarriers.Add(key, barrier);

            return true;
        }

        [NotNull]
        private HashSet<int> GetConnectedClientIds()
        {
            return ServerManager.Clients.Values
                .Where(connection => connection != null && connection.IsActive)
                .Select(connection => connection.ClientId)
                .ToHashSet();
        }

        // ── Client-side wait flow ────────────────────────────────────────

        [Client]
        private async Task<bool> WaitForLoadClientAsync(string key, float timeoutSeconds)
        {
            if (!_clientWaitRequests.TryGetValue(key, out TaskCompletionSource<bool> source))
            {
                source = new(TaskCreationOptions.RunContinuationsAsynchronously);

                if (!_clientWaitRequests.TryAdd(key, source))
                {
                    Log.Error(this, $"Failed to register wait request for '{key}'.");

                    return false;
                }

                RpcRequestLoadResult(key, timeoutSeconds, Owner);
            }

            float timeoutWithGrace = timeoutSeconds + ClientWaitResponseGraceSeconds;

            try
            {
                return await source.Task.WaitWithTimeout(timeoutWithGrace);
            }
            catch (TimeoutException)
            {
                Log.Error(this, $"Timed out waiting for load result '{key}' after {timeoutWithGrace:0.##} seconds.");
                source.TrySetResult(false);

                return false;
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Unexpected exception while waiting for load result '{key}'.");
                source.TrySetResult(false);

                return false;
            }
            finally
            {
                _clientWaitRequests.Remove(key);
            }
        }

        // ── RPCs ─────────────────────────────────────────────────────────

        [ObserversRpc]
        private void RpcLoadAsset(string key, byte backendType)
        {
            LoadLocallyAsync(key, (AssetBackendType)backendType);
        }

        [ObserversRpc]
        private void RpcUnloadAsset(string key)
        {
            if (!IsClient)
            {
                return;
            }

            ReleaseLocalHandle(key);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcAcknowledgeLoad(string key, bool success, [CanBeNull] NetworkConnection connection = null)
        {
            if (connection == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            if (_loadBarriers.TryGetValue(key, out LoadBarrier barrier))
            {
                barrier.Acknowledge(connection.ClientId, success);

                if (barrier.Task.IsCompleted)
                {
                    _loadBarriers.Remove(key);
                }
            }

            if (_preloadSessions.TryGetValue(connection.ClientId, out PreloadSession session)
                && session.TryAcknowledge(key, success))
            {
                TryFinalizePreloadSession(connection.ClientId, session);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcRequestLoadResult(string key, float timeoutSeconds, NetworkConnection connection)
        {
            ProcessLoadResultAsync(key, connection, timeoutSeconds);
        }

        // ReSharper disable once UnusedParameter.Local
        [TargetRpc]
        private void RpcSendLoadResult(NetworkConnection connection, string key, bool result)
        {
            if (_clientWaitRequests.TryGetValue(key, out TaskCompletionSource<bool> source))
            {
                source.TrySetResult(result);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        [TargetRpc]
        private void RpcPreloadForClient(NetworkConnection connection, string key, byte backendType)
        {
            LoadLocallyAsync(key, (AssetBackendType)backendType);
        }

        // ── Client-side loading ──────────────────────────────────────────

        private async void LoadLocallyAsync(string key, AssetBackendType backendType)
        {
            AssetSubSystem assetSubSystem = SubSystems.Get<AssetSubSystem>();

            if (!assetSubSystem)
            {
                Log.Error(this, $"Cannot start synchronized load for '{key}' because {nameof(AssetSubSystem)} instance is missing.");

                if (IsClient)
                {
                    RpcAcknowledgeLoad(key, false);
                }

                return;
            }

            try
            {
                for (int attempt = 0; attempt < _retryAttempts; attempt++)
                {
                    AssetHandle<Object> handle = await assetSubSystem.AcquireAsync<Object>(key, backendType);

                    if (handle?.Asset == null)
                    {
                        Log.Warning(this, $"Attempt {attempt + 1} to load asset '{key}' returned null. Retrying...");
                        handle?.Dispose();

                        continue;
                    }

                    // Store handle to keep the asset alive on this client.
                    if (_clientHandles.TryGetValue(key, out IAssetHandle existing))
                    {
                        existing?.Dispose();
                    }

                    _clientHandles[key] = handle;

                    if (IsClient)
                    {
                        RpcAcknowledgeLoad(key, true);
                    }

                    return;
                }

                Log.Error(this, $"Failed to load asset '{key}' after {_retryAttempts} attempts.");

                if (IsClient)
                {
                    RpcAcknowledgeLoad(key, false);
                }
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed to load asset '{key}'.");

                if (IsClient)
                {
                    RpcAcknowledgeLoad(key, false);
                }
            }
        }

        private void ReleaseLocalHandle(string key)
        {
            if (_clientHandles.Remove(key, out IAssetHandle handle))
            {
                handle?.Dispose();
            }
        }

        // ── Server-side event handlers ───────────────────────────────────

        [Server]
        private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs stateData)
        {
            if (stateData.ConnectionState != RemoteConnectionState.Stopped)
            {
                return;
            }

            foreach (LoadBarrier barrier in _loadBarriers.Values)
            {
                barrier.RemoveClient(connection.ClientId);
            }

            ClearPreloadState(connection.ClientId);
        }

        [Server]
        private void HandleClientLoadedStartScenes([CanBeNull] NetworkConnection connection, bool asServer)
        {
            if (!asServer || connection == null)
            {
                return;
            }

            StartPreloadSession(connection);
        }

        [Server]
        private void HandleLastInstanceDestroyed(string key)
        {
            BroadcastUnload(key);
        }

        // ── Late-join preload ────────────────────────────────────────────

        [Server]
        private void StartPreloadSession([NotNull] NetworkConnection connection)
        {
            int clientId = connection.ClientId;
            ClearPreloadState(clientId);

            HashSet<string> preloadKeys = new();

            foreach (KeyValuePair<string, LoadBarrier> pair in _loadBarriers.Where(pair => !pair.Value.Task.IsCompleted))
            {
                pair.Value.AddClient(clientId);
                preloadKeys.Add(pair.Key);
            }

            (string Key, AssetBackendType BackendType)[] activeAssets = _worldTracker.GetActiveAssets();

            foreach ((string key, _) in activeAssets)
            {
                preloadKeys.Add(key);
            }

            PreloadSession session = new(preloadKeys);
            _preloadSessions[clientId] = session;

            if (preloadKeys.Count == 0)
            {
                TryFinalizePreloadSession(clientId, session);

                return;
            }

            Log.Information(this, $"Starting late-join preload for ClientID {clientId} with {session.PendingCount} assets.");

            // Send active world assets with their known backend type.
            HashSet<string> sentKeys = new();

            foreach ((string key, AssetBackendType backendType) in activeAssets)
            {
                if (preloadKeys.Contains(key))
                {
                    RpcPreloadForClient(connection, key, (byte)backendType);
                    sentKeys.Add(key);
                }
            }

            // Send in-flight barrier assets not already covered by active world state.
            foreach (KeyValuePair<string, LoadBarrier> pair in _loadBarriers.Where(pair => !pair.Value.Task.IsCompleted))
            {
                if (!sentKeys.Contains(pair.Key))
                {
                    RpcPreloadForClient(connection, pair.Key, (byte)pair.Value.BackendType);
                }
            }

            MonitorPreloadAsync(clientId);
        }

        [Server]
        private void ClearPreloadState(int clientId)
        {
            _clientsWithCompletedPreload.Remove(clientId);

            if (!_preloadSessions.TryGetValue(clientId, out PreloadSession session))
            {
                return;
            }

            session.TrySetCanceled();
            _preloadSessions.Remove(clientId);
        }

        [Server]
        private void TryFinalizePreloadSession(int clientId, [NotNull] PreloadSession session)
        {
            if (!session.Task.IsCompleted
                || !_preloadSessions.TryGetValue(clientId, out PreloadSession current)
                || !ReferenceEquals(current, session))
            {
                return;
            }

            _preloadSessions.Remove(clientId);

            if (session.Task.IsCanceled)
            {
                _clientsWithCompletedPreload.Remove(clientId);

                return;
            }

            if (session.Succeeded)
            {
                _clientsWithCompletedPreload.Add(clientId);
                Log.Information(this, $"Late-join preload completed for ClientID {clientId}.");

                return;
            }

            _clientsWithCompletedPreload.Remove(clientId);
            Log.Error(this, $"Late-join preload failed for ClientID {clientId}.");
        }

        [Server]
        private async void MonitorPreloadAsync(int clientId)
        {
            try
            {
                await WaitForClientPreloadAsync(clientId);
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed while monitoring late-join preload for ClientID {clientId}.");
            }
        }

        [Server]
        private async void ProcessLoadResultAsync(string key, NetworkConnection connection, float timeoutSeconds)
        {
            try
            {
                bool result = await WaitForLoadServerAsync(key, timeoutSeconds);
                RpcSendLoadResult(connection, key, result);
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed to process load result for '{key}' from ClientID {connection.ClientId}.");
                RpcSendLoadResult(connection, key, false);
            }
        }
    }
}
