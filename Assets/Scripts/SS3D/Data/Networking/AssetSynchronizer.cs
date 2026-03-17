using Coimbra;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using FishNet.Transporting;
using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Data.AssetDatabases;
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
    /// Coordinates addressable asset residency across the network.
    /// The server owns synchronized load barriers, clients report local load results,
    /// and late joiners are instructed to preload assets that are still active in the world.
    /// </summary>
    [RequireComponent(typeof(NetworkObserver))]
    internal sealed class AssetSynchronizer : NetworkActor
    {
        /// <summary>
        /// Tracks one in-flight synchronized load and the clients that still need to acknowledge it.
        /// </summary>
        private sealed class LoadRequest
        {
            private bool _failed;
            private TaskCompletionSource<bool> _taskSource;
            private HashSet<int> _pendingClientIds;

            internal LoadRequest([NotNull] HashSet<int> pendingClientIds)
            {
                Setup(pendingClientIds);
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

            private void Setup([CanBeNull] HashSet<int> pendingClientIds)
            {
                _taskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingClientIds = pendingClientIds ?? new HashSet<int>();

                if (_pendingClientIds.Count == 0)
                {
                    _taskSource.TrySetResult(true);
                }
            }

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
        /// This is separate from synchronized load barriers so new live-world loads
        /// do not mutate the original catch-up snapshot created when the client joined.
        /// </summary>
        private sealed class ClientPreloadSession
        {
            private readonly HashSet<AssetKey> _pendingAssets;
            private readonly TaskCompletionSource<bool> _taskSource;
            private bool? _result;

            internal ClientPreloadSession([CanBeNull] HashSet<AssetKey> pendingAssets)
            {
                _pendingAssets = pendingAssets ?? new HashSet<AssetKey>();
                _taskSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

                if (_pendingAssets.Count == 0)
                {
                    Complete(true);
                }
            }

            internal Task<bool> Task => _taskSource.Task;

            internal int PendingCount => _pendingAssets.Count;

            internal bool Succeeded => _result == true;

            internal bool TryAcknowledge(AssetKey assetKey, bool loaded)
            {
                if (Task.IsCompleted || !_pendingAssets.Contains(assetKey))
                {
                    return false;
                }

                if (!loaded)
                {
                    Complete(false);

                    return true;
                }

                _pendingAssets.Remove(assetKey);

                if (_pendingAssets.Count == 0)
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
        /// Server-side synchronized load barriers keyed by logical asset identity.
        /// Entries exist only while a coordinated load is still unresolved.
        /// </summary>
        private readonly Dictionary<AssetKey, LoadRequest> _loadRequests = new();

        /// <summary>
        /// Client-side awaiters used when a client asks the server for the result of an in-flight load barrier.
        /// </summary>
        private readonly Dictionary<AssetKey, TaskCompletionSource<bool>> _clientWaitRequests = new();

        /// <summary>
        /// Server-side late-join preload sessions keyed by client ID.
        /// Each session represents the fixed asset snapshot that client still needs to acknowledge before
        /// it is considered caught up with the currently active addressable world state.
        /// </summary>
        private readonly Dictionary<int, ClientPreloadSession> _clientPreloadSessions = new();

        /// <summary>
        /// Tracks clients that completed their most recent late-join preload session successfully.
        /// </summary>
        private readonly HashSet<int> _clientsWithCompletedPreload = new();

        [SerializeField]
        private int _retryAttempts = 5;

        [SerializeField]
        private float _lateJoinPreloadTimeoutSeconds = 15f;

        public static AssetSynchronizer Instance { get; private set; }

        // ReSharper disable Unity.PerformanceAnalysis
        public override void OnStartServer()
        {
            base.OnStartServer();
            SceneManager.OnClientLoadedStartScenes += HandleClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
            NetworkAssetRegistry.OnAssetNoLongerActive += HandleAssetNoLongerActive;
        }

        public override void OnStopClient()
        {
            base.OnStopClient();

            foreach (TaskCompletionSource<bool> completionSource in _clientWaitRequests.Values)
            {
                completionSource.TrySetCanceled();
            }

            _clientWaitRequests.Clear();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            SceneManager.OnClientLoadedStartScenes -= HandleClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;
            NetworkAssetRegistry.OnAssetNoLongerActive -= HandleAssetNoLongerActive;

            foreach (LoadRequest loadRequest in _loadRequests.Values)
            {
                loadRequest.TrySetCanceled();
            }

            foreach (ClientPreloadSession preloadSession in _clientPreloadSessions.Values)
            {
                preloadSession.TrySetCanceled();
            }

            _loadRequests.Clear();
            _clientPreloadSessions.Clear();
            _clientsWithCompletedPreload.Clear();
            NetworkAssetRegistry.Clear();
        }

        /// <summary>
        /// Ensures an addressable asset is loaded on every connected client before dependent server logic proceeds.
        /// Servers create or join the authoritative barrier, while clients await that barrier through the server.
        /// </summary>
        internal Task<bool> EnsureLoadedOnAllClientsAsync(string databaseId, string assetId, float timeoutSeconds = 15f)
        {
            return EnsureLoadedOnAllClientsAsync(new AssetKey(databaseId, assetId), timeoutSeconds);
        }

        /// <summary>
        /// Ensures an addressable asset is loaded on every connected client before dependent server logic proceeds.
        /// Servers create or join the authoritative barrier, while clients await that barrier through the server.
        /// </summary>
        internal async Task<bool> EnsureLoadedOnAllClientsAsync(AssetKey assetKey, float timeoutSeconds = 15f)
        {
            if (!IsServer)
            {
                return await WaitForLoadClientAsync(assetKey, timeoutSeconds);
            }

            StartSynchronizedLoad(assetKey);

            return await WaitForLoadServerAsync(assetKey, timeoutSeconds);

        }

        protected override void OnAwake()
        {
            base.OnAwake();

            if (Instance)
            {
                Log.Error(this, $"Multiple instances of {nameof(AssetSynchronizer)} detected. Destroying the new one.");

                GameObject.Dispose(true);

                return;
            }

            Instance = this;
        }

        /// <summary>
        /// Starts a synchronized load using a serialized asset reference.
        /// </summary>
        [Server]
        private void StartSynchronizedLoad(ObjectAssetReference assetReference)
        {
            if (!assetReference)
            {
                Log.Warning(this, "Cannot synchronize asset load because the reference is null.");

                return;
            }

            StartSynchronizedLoad(new AssetKey(assetReference.Database, assetReference.Id));
        }

        /// <summary>
        /// Creates a server-owned load barrier and broadcasts the local load request to all observers.
        /// Duplicate requests reuse the existing barrier instead of re-broadcasting.
        /// </summary>
        [Server]
        private void StartSynchronizedLoad(AssetKey assetKey)
        {
            if (!assetKey.IsValid)
            {
                Log.Warning(this, $"Ignoring invalid synchronize request '{assetKey}'.");

                return;
            }

            if (!TryGetAssetSubSystem(out AssetSubSystem assetSubSystem))
            {
                Log.Error(this, $"Cannot synchronize asset '{assetKey}' because {nameof(AssetSubSystem)} instance is missing.");

                return;
            }

            if (!assetSubSystem.Has(assetKey))
            {
                Log.Error(this, $"Cannot synchronize asset '{assetKey}' because it is not available through the async runtime-loading path.");

                return;
            }

            if (!TryRegisterSynchronizedLoad(assetKey))
            {
                return;
            }

            RpcSynchronizedLoad(assetKey.DatabaseId, assetKey.AssetId);
        }

        [ObserversRpc]
        private void RpcSynchronizedLoad([NotNull] string databaseId, [NotNull] string assetId)
        {
            StartLoadAsync(databaseId, assetId);
        }

        /// <summary>
        /// Waits for the server-owned load barrier to resolve for the requested asset.
        /// </summary>
        [Server]
        private async Task<bool> WaitForLoadServerAsync(AssetKey assetKey, float timeoutSeconds)
        {
            if (!assetKey.IsValid)
            {
                Log.Error(this, $"Can't wait to load asset on server with invalid request '{assetKey}'.");

                return false;
            }

            if (!_loadRequests.TryGetValue(assetKey, out LoadRequest request))
            {
                Log.Error(this, $"No load request found for '{assetKey}' on server.");

                return false;
            }

            try
            {
                return await request.Task.WaitWithTimeout(timeoutSeconds);
            }
            catch (TimeoutException)
            {
                Log.Error(this, $"Timed out waiting for load request '{assetKey}' after {timeoutSeconds:0.##} seconds.");
                _loadRequests.Remove(assetKey);

                return false;
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed while waiting for load request '{assetKey}'.");
                _loadRequests.Remove(assetKey);

                return false;
            }
            finally
            {
                if (request.Task.IsCompleted)
                {
                    _loadRequests.Remove(assetKey);
                }
            }
        }

        /// <summary>
        /// Waits on a client for the server to report the result of a synchronized load barrier.
        /// </summary>
        [Client]
        private async Task<bool> WaitForLoadClientAsync(AssetKey assetKey, float timeoutSeconds)
        {
            if (!_clientWaitRequests.TryGetValue(assetKey, out TaskCompletionSource<bool> completionSource))
            {
                completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

                if (!_clientWaitRequests.TryAdd(assetKey, completionSource))
                {
                    Log.Error(this, $"Failed to register wait request for '{assetKey}'.");

                    return false;
                }

                RpcWaitForLoadClient(assetKey.DatabaseId, assetKey.AssetId, timeoutSeconds, Owner);
            }

            float timeoutWithGrace = timeoutSeconds + ClientWaitResponseGraceSeconds;

            try
            {
                return await completionSource.Task.WaitWithTimeout(timeoutWithGrace);
            }
            catch (TimeoutException)
            {
                Log.Error(this, $"Timed out waiting for load result '{assetKey}' after {timeoutWithGrace:0.##} seconds.");
                completionSource.TrySetResult(false);

                return false;
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Unexpected exception while waiting for load result '{assetKey}'.");
                completionSource.TrySetResult(false);

                return false;
            }
            finally
            {
                _clientWaitRequests.Remove(assetKey);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcWaitForLoadClient(string databaseId, string assetId, float timeoutSeconds, NetworkConnection connection)
        {
            ProcessLoadResult(databaseId, assetId, connection, timeoutSeconds);
        }

        /// <summary>
        /// Resolves a client wait request by awaiting the authoritative server-side load barrier and replying to that client.
        /// </summary>
        [Server]
        private async void ProcessLoadResult(string databaseId, string assetId, NetworkConnection connection, float timeoutSeconds)
        {
            AssetKey assetKey = new(databaseId, assetId);

            try
            {
                bool result = await WaitForLoadServerAsync(assetKey, timeoutSeconds);

                RpcReceiveLoadResult(connection, databaseId, assetId, result);
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed to process load result for '{assetKey}' from ClientID {connection.ClientId}.");

                RpcReceiveLoadResult(connection, databaseId, assetId, false);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        [TargetRpc]
        private void RpcReceiveLoadResult(NetworkConnection connection, string databaseId, string assetId, bool result)
        {
            AssetKey assetKey = new(databaseId, assetId);

            if (_clientWaitRequests.TryGetValue(assetKey, out TaskCompletionSource<bool> waitRequest))
            {
                waitRequest.TrySetResult(result);
            }
        }

        /// <summary>
        /// Loads the requested asset locally on this peer and reports the outcome back to the server.
        /// </summary>
        private async void StartLoadAsync([NotNull] string databaseId, [NotNull] string assetId)
        {
            AssetKey assetKey = new(databaseId, assetId);

            if (!TryGetAssetSubSystem(out AssetSubSystem assetSubSystem))
            {
                Log.Error(this, $"Cannot start synchronized load for '{assetKey}' because {nameof(AssetSubSystem)} instance is missing.");

                if (IsClient)
                {
                    RpcHandleAssetLoaded(databaseId, assetId, false);
                }

                return;
            }

            if (!assetSubSystem.Has(assetKey))
            {
                Log.Error(this, $"Cannot start synchronized load for '{assetKey}' because it is not available through the async runtime-loading path.");

                if (IsClient)
                {
                    RpcHandleAssetLoaded(databaseId, assetId, false);
                }

                return;
            }

            try
            {
                for (int attempt = 0; attempt < _retryAttempts; attempt++)
                {
                    Object asset = await assetSubSystem.AcquireNetworkAssetAsync<Object>(assetKey);

                    if (!asset)
                    {
                        Log.Warning(this, $"Attempt {attempt + 1} to load asset '{assetKey}' returned null. Retrying...");

                        continue;
                    }

                    if (!IsClient)
                    {
                        return;
                    }

                    RpcHandleAssetLoaded(databaseId, assetId, true);

                    return;
                }

                Log.Error(this, $"Failed to load asset '{assetKey}' after {_retryAttempts} attempts.");
                RpcHandleAssetLoaded(databaseId, assetId, false);
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed to load asset '{assetKey}'.");
                RpcHandleAssetLoaded(databaseId, assetId, false);
            }
        }

        [Server]
        private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs stateData)
        {
            if (stateData.ConnectionState != RemoteConnectionState.Stopped)
            {
                return;
            }

            // A disconnected client can no longer acknowledge pending loads, so remove it from every active barrier.
            foreach (LoadRequest request in _loadRequests.Values)
            {
                request.RemoveClient(connection.ClientId);
            }

            ClearClientPreloadState(connection.ClientId, cancelSession: true);

            // Don't if I have to handle added as well
            // Action<LoadRequest> actionToTake = stateData.ConnectionState switch
            // {
            //     RemoteConnectionState.Started => loadRequest => loadRequest.AddClient(connection.ClientId),
            //     RemoteConnectionState.Stopped => loadRequest => loadRequest.RemoveClient(connection.ClientId),
            //     _ => null,
            // };
            //
            // if (actionToTake == null)
            // {
            //     Log.Error(this, $"Unknown connection state for ClientID {connection.ClientId}: {stateData.ConnectionState}");
            //
            //     return;
            // }
            //
            // foreach (LoadRequest request in _loadRequests.Values.Where(request => !request.Task.IsCompleted))
            // {
            //     actionToTake(request);
            // }
        }

        [Server]
        private void HandleClientLoadedStartScenes([CanBeNull] NetworkConnection connection, bool asServer)
        {
            if (!asServer || connection == null)
            {
                return;
            }

            StartClientPreloadSession(connection);
        }

        // ReSharper disable once UnusedParameter.Local
        [TargetRpc]
        private void RpcLoadForClient(NetworkConnection connection, [NotNull] string databaseId, [NotNull] string assetId)
        {
            StartLoadAsync(databaseId, assetId);
        }

        [ServerRpc(RequireOwnership = false)]
        private void RpcHandleAssetLoaded([NotNull] string databaseId, [NotNull] string assetId, bool loaded, [CanBeNull] NetworkConnection connection = null)
        {
            AssetKey assetKey = new(databaseId, assetId);

            if (connection == null || !assetKey.IsValid)
            {
                return;
            }

            if (_loadRequests.TryGetValue(assetKey, out LoadRequest request))
            {
                request.Acknowledge(connection.ClientId, loaded);

                if (request.Task.IsCompleted)
                {
                    _loadRequests.Remove(assetKey);
                }
            }

            if (_clientPreloadSessions.TryGetValue(connection.ClientId, out ClientPreloadSession preloadSession)
                && preloadSession.TryAcknowledge(assetKey, loaded))
            {
                TryFinalizeClientPreloadSession(connection.ClientId, preloadSession);
            }
        }

        /// <summary>
        /// Registers a new server-owned load barrier for the given asset.
        /// </summary>
        /// <returns><see langword="true"/> when a new request was created; otherwise <see langword="false"/>.</returns>
        private bool TryRegisterSynchronizedLoad(AssetKey assetKey)
        {
            if (_loadRequests.TryGetValue(assetKey, out LoadRequest request))
            {
                return false;
            }

            request = new(GetConnectedClientIds());
            _loadRequests.Add(assetKey, request);

            return true;
        }

        [NotNull]
        private HashSet<int> GetConnectedClientIds()
        {
            return ServerManager.Clients.Values.Where(connection => connection != null && connection.IsActive).Select(connection => connection.ClientId).ToHashSet();
        }

        /// <summary>
        /// Returns <see langword="true"/> once the latest late-join preload session for the specified client
        /// completed successfully.
        /// </summary>
        [Server]
        internal bool IsClientPreloadComplete(int clientId) => _clientsWithCompletedPreload.Contains(clientId);

        /// <summary>
        /// Waits for the specified client's late-join preload snapshot to finish.
        /// This is the server-side hook future join/world-ready flow should await before granting
        /// full participation in addressable-backed world state.
        /// </summary>
        [Server]
        internal async Task<bool> WaitForClientPreloadAsync(int clientId, float timeoutSeconds = -1f)
        {
            if (!_clientPreloadSessions.TryGetValue(clientId, out ClientPreloadSession preloadSession))
            {
                return _clientsWithCompletedPreload.Contains(clientId);
            }

            float effectiveTimeout = timeoutSeconds > 0f ? timeoutSeconds : _lateJoinPreloadTimeoutSeconds;

            try
            {
                return await preloadSession.Task.WaitWithTimeout(effectiveTimeout);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (TimeoutException)
            {
                Log.Error(this, $"Timed out waiting for late-join preload for ClientID {clientId} after {effectiveTimeout:0.##} seconds.");
                preloadSession.TrySetFailed();

                return false;
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Unexpected exception while waiting for late-join preload for ClientID {clientId}.");
                preloadSession.TrySetFailed();

                return false;
            }
            finally
            {
                TryFinalizeClientPreloadSession(clientId, preloadSession);
            }
        }

        /// <summary>
        /// Broadcasts a network residency release once the asset falls out of the active world manifest.
        /// The underlying asset is only fully unloaded when no other loader owners still retain it.
        /// </summary>
        [Server]
        internal void SynchronizeUnload(AssetKey assetKey)
        {
            if (!assetKey.IsValid)
            {
                return;
            }

            RpcSynchronizedUnload(assetKey.DatabaseId, assetKey.AssetId);
            if (TryGetAssetSubSystem(out AssetSubSystem assetSubSystem))
            {
                assetSubSystem.ReleaseNetworkAsset(assetKey);
            }
        }

        /// <summary>
        /// Releases an asset across the network when the registry reports that its final live instance is gone.
        /// </summary>
        [Server]
        private void HandleAssetNoLongerActive(AssetKey assetKey)
        {
            SynchronizeUnload(assetKey);
        }

        [ObserversRpc]
        private void RpcSynchronizedUnload([NotNull] string databaseId, [NotNull] string assetId)
        {
            AssetKey assetKey = new(databaseId, assetId);

            if (!IsClient || !assetKey.IsValid)
            {
                return;
            }

            if (TryGetAssetSubSystem(out AssetSubSystem assetSubSystem))
            {
                assetSubSystem.ReleaseNetworkAsset(assetKey);
            }
        }

        /// <summary>
        /// Starts one fixed late-join preload session for the client by merging:
        /// - assets that are already active in the live world
        /// - synchronized loads that are still in progress
        /// Acknowledgements for assets already in that snapshot satisfy both the client preload session
        /// and any matching synchronized load barrier.
        /// </summary>
        [Server]
        private void StartClientPreloadSession([NotNull] NetworkConnection connection)
        {
            int clientId = connection.ClientId;
            ClearClientPreloadState(clientId, cancelSession: true);

            HashSet<AssetKey> preloadAssets = new();

            foreach (KeyValuePair<AssetKey, LoadRequest> pair in _loadRequests.Where(pair => !pair.Value.Task.IsCompleted))
            {
                AssetKey assetKey = pair.Key;
                LoadRequest request = pair.Value;
                request.AddClient(clientId);
                preloadAssets.Add(assetKey);
            }

            foreach (AssetKey assetKey in NetworkAssetRegistry.GetActiveAssets())
            {
                preloadAssets.Add(assetKey);
            }

            ClientPreloadSession preloadSession = new(preloadAssets);
            _clientPreloadSessions[clientId] = preloadSession;

            if (preloadAssets.Count == 0)
            {
                TryFinalizeClientPreloadSession(clientId, preloadSession);

                return;
            }

            Log.Information(this, $"Starting late-join preload for ClientID {clientId} with {preloadSession.PendingCount} addressable assets.");

            foreach (AssetKey assetKey in preloadAssets)
            {
                RpcLoadForClient(connection, assetKey.DatabaseId, assetKey.AssetId);
            }

            MonitorClientPreloadAsync(clientId);
        }

        [Server]
        private void ClearClientPreloadState(int clientId, bool cancelSession)
        {
            _clientsWithCompletedPreload.Remove(clientId);

            if (!_clientPreloadSessions.TryGetValue(clientId, out ClientPreloadSession preloadSession))
            {
                return;
            }

            if (cancelSession)
            {
                preloadSession.TrySetCanceled();
            }

            _clientPreloadSessions.Remove(clientId);
        }

        [Server]
        private void TryFinalizeClientPreloadSession(int clientId, [NotNull] ClientPreloadSession preloadSession)
        {
            if (!preloadSession.Task.IsCompleted
                || !_clientPreloadSessions.TryGetValue(clientId, out ClientPreloadSession currentSession)
                || !ReferenceEquals(currentSession, preloadSession))
            {
                return;
            }

            _clientPreloadSessions.Remove(clientId);

            if (preloadSession.Task.IsCanceled)
            {
                _clientsWithCompletedPreload.Remove(clientId);

                return;
            }

            if (preloadSession.Succeeded)
            {
                _clientsWithCompletedPreload.Add(clientId);
                Log.Information(this, $"Late-join preload completed for ClientID {clientId}.");

                return;
            }

            _clientsWithCompletedPreload.Remove(clientId);
            Log.Error(this, $"Late-join preload failed for ClientID {clientId}.");
        }

        [Server]
        private async void MonitorClientPreloadAsync(int clientId)
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

        private bool TryGetAssetSubSystem([CanBeNull] out AssetSubSystem assetSubSystem)
        {
            assetSubSystem = AssetSubSystem.Instance;

            return assetSubSystem;
        }
    }
}
