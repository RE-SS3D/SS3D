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

        private const float ClientWaitResponseGraceSeconds = 1f;

        /// <summary>
        /// Server-side synchronized load barriers keyed by logical asset identity.
        /// Entries exist only while a coordinated load is still unresolved.
        /// </summary>
        private readonly Dictionary<(string DatabaseId, string AssetId), LoadRequest> _loadRequests = new();

        /// <summary>
        /// Client-side awaiters used when a client asks the server for the result of an in-flight load barrier.
        /// </summary>
        private readonly Dictionary<(string DatabaseId, string AssetId), TaskCompletionSource<bool>> _clientWaitRequests = new();

        [SerializeField]
        private int _retryAttempts = 5;

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

            _loadRequests.Clear();
            NetworkAssetRegistry.Clear();
        }

        /// <summary>
        /// Ensures an addressable asset is loaded on every connected client before dependent server logic proceeds.
        /// Servers create or join the authoritative barrier, while clients await that barrier through the server.
        /// </summary>
        internal async Task<bool> EnsureLoadedOnAllClientsAsync(string databaseId, string assetId, float timeoutSeconds = 15f)
        {
            if (!IsServer)
            {
                return await WaitForLoadClientAsync(databaseId, assetId, timeoutSeconds);
            }

            StartSynchronizedLoad(databaseId, assetId);

            return await WaitForLoadServerAsync(databaseId, assetId, timeoutSeconds);

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

        private static bool IsValidRequest([CanBeNull] string databaseId, [CanBeNull] string assetId) =>
            !string.IsNullOrWhiteSpace(databaseId) && !string.IsNullOrWhiteSpace(assetId);

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

            StartSynchronizedLoad(assetReference.Database, assetReference.Id);
        }

        /// <summary>
        /// Creates a server-owned load barrier and broadcasts the local load request to all observers.
        /// Duplicate requests reuse the existing barrier instead of re-broadcasting.
        /// </summary>
        [Server]
        private void StartSynchronizedLoad(string databaseId, string assetId)
        {
            if (!IsValidRequest(databaseId, assetId))
            {
                Log.Warning(this, $"Ignoring invalid synchronize request. Database: '{databaseId}', Asset: '{assetId}'.");

                return;
            }

            if (!TryRegisterSynchronizedLoad(databaseId, assetId))
            {
                return;
            }

            RpcSynchronizedLoad(databaseId, assetId);
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
        private async Task<bool> WaitForLoadServerAsync(string databaseId, string assetId, float timeoutSeconds)
        {
            if (!IsValidRequest(databaseId, assetId))
            {
                Log.Error(this, $"Can't wait to load asset on server with invalid request. Database: '{databaseId}', Asset: '{assetId}'.");

                return false;
            }

            if (!_loadRequests.TryGetValue((databaseId, assetId), out LoadRequest request))
            {
                Log.Error(this, $"No load request found for '{databaseId}/{assetId}' on server.");

                return false;
            }

            (string DatabaseId, string AssetId) key = (databaseId, assetId);

            try
            {
                return await request.Task.WaitWithTimeout(timeoutSeconds);
            }
            catch (TimeoutException)
            {
                Log.Error(this, $"Timed out waiting for load request '{databaseId}/{assetId}' after {timeoutSeconds:0.##} seconds.");
                _loadRequests.Remove(key);

                return false;
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed while waiting for load request '{databaseId}/{assetId}'.");
                _loadRequests.Remove(key);

                return false;
            }
            finally
            {
                if (request.Task.IsCompleted)
                {
                    _loadRequests.Remove(key);
                }
            }
        }

        /// <summary>
        /// Waits on a client for the server to report the result of a synchronized load barrier.
        /// </summary>
        [Client]
        private async Task<bool> WaitForLoadClientAsync(string databaseId, string assetId, float timeoutSeconds)
        {
            if (!_clientWaitRequests.TryGetValue((databaseId, assetId), out TaskCompletionSource<bool> completionSource))
            {
                completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

                if (!_clientWaitRequests.TryAdd((databaseId, assetId), completionSource))
                {
                    Log.Error(this, $"Failed to register wait request for '{databaseId}/{assetId}'.");

                    return false;
                }

                RpcWaitForLoadClient(databaseId, assetId, timeoutSeconds, Owner);
            }

            float timeoutWithGrace = timeoutSeconds + ClientWaitResponseGraceSeconds;

            try
            {
                return await completionSource.Task.WaitWithTimeout(timeoutWithGrace);
            }
            catch (TimeoutException)
            {
                Log.Error(this, $"Timed out waiting for load result '{databaseId}/{assetId}' after {timeoutWithGrace:0.##} seconds.");
                completionSource.TrySetResult(false);

                return false;
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Unexpected exception while waiting for load result '{databaseId}/{assetId}'.");
                completionSource.TrySetResult(false);

                return false;
            }
            finally
            {
                _clientWaitRequests.Remove((databaseId, assetId));
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
            try
            {
                bool result = await WaitForLoadServerAsync(databaseId, assetId, timeoutSeconds);

                RpcReceiveLoadResult(connection, databaseId, assetId, result);
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed to process load result for '{databaseId}/{assetId}' from ClientID {connection.ClientId}.");

                RpcReceiveLoadResult(connection, databaseId, assetId, false);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        [TargetRpc]
        private void RpcReceiveLoadResult(NetworkConnection connection, string databaseId, string assetId, bool result)
        {
            if (_clientWaitRequests.TryGetValue((databaseId, assetId), out TaskCompletionSource<bool> waitRequest))
            {
                waitRequest.TrySetResult(result);
            }
        }

        /// <summary>
        /// Loads the requested asset locally on this peer and reports the outcome back to the server.
        /// </summary>
        private async void StartLoadAsync([NotNull] string databaseId, [NotNull] string assetId)
        {
            try
            {
                for (int attempt = 0; attempt < _retryAttempts; attempt++)
                {
                    Object asset = await AssetLoader.GetAsync<Object>(databaseId, assetId);

                    if (!asset)
                    {
                        Log.Warning(this, $"Attempt {attempt + 1} to load asset '{databaseId}/{assetId}' returned null. Retrying...");

                        continue;
                    }

                    if (!IsClient)
                    {
                        return;
                    }

                    RpcHandleAssetLoaded(databaseId, assetId, true);

                    return;
                }

                Log.Error(this, $"Failed to load asset '{databaseId}/{assetId}' after {_retryAttempts} attempts.");
                RpcHandleAssetLoaded(databaseId, assetId, false);
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed to load asset '{databaseId}/{assetId}'.");
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

            // Re-attach the late joiner to any barrier still in progress so the current synchronized load can finish cleanly.
            foreach (((string databaseId, string assetId), LoadRequest request) in _loadRequests)
            {
                request.AddClient(connection.ClientId);
                RpcLoadForClient(connection, databaseId, assetId);
            }

            // Replay the live addressable manifest so currently spawned addressable prefabs can be instantiated on the joining client.
            foreach ((string databaseId, string assetId) in NetworkAssetRegistry.GetActiveAssets())
            {
                RpcLoadForClient(connection, databaseId, assetId);
            }
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
            if (connection == null || !IsValidRequest(databaseId, assetId))
            {
                return;
            }

            (string DatabaseId, string AssetId) key = (databaseId, assetId);

            if (!_loadRequests.TryGetValue(key, out LoadRequest request))
            {
                return;
            }

            request.Acknowledge(connection.ClientId, loaded);

            if (request.Task.IsCompleted)
            {
                _loadRequests.Remove(key);
            }
        }

        /// <summary>
        /// Registers a new server-owned load barrier for the given asset.
        /// </summary>
        /// <returns><see langword="true"/> when a new request was created; otherwise <see langword="false"/>.</returns>
        private bool TryRegisterSynchronizedLoad(string databaseId, string assetId)
        {
            if (_loadRequests.TryGetValue((databaseId, assetId), out LoadRequest request))
            {
                return false;
            }

            request = new(GetConnectedClientIds());
            _loadRequests.Add((databaseId, assetId), request);

            return true;
        }

        [NotNull]
        private HashSet<int> GetConnectedClientIds()
        {
            return ServerManager.Clients.Values.Where(connection => connection != null && connection.IsActive).Select(connection => connection.ClientId).ToHashSet();
        }

        /// <summary>
        /// Broadcasts an unload once the asset falls out of the active world manifest.
        /// </summary>
        [Server]
        internal void SynchronizeUnload(string databaseId, string assetId)
        {
            if (!IsValidRequest(databaseId, assetId))
            {
                return;
            }

            RpcSynchronizedUnload(databaseId, assetId);
            AssetLoader.Unload(assetId);
        }

        /// <summary>
        /// Releases an asset across the network when the registry reports that its final live instance is gone.
        /// </summary>
        [Server]
        private void HandleAssetNoLongerActive(string databaseId, string assetId)
        {
            SynchronizeUnload(databaseId, assetId);
        }

        [ObserversRpc]
        private void RpcSynchronizedUnload([NotNull] string databaseId, [NotNull] string assetId)
        {
            if (!IsClient || !IsValidRequest(databaseId, assetId))
            {
                return;
            }

            AssetLoader.Unload(assetId);
        }
    }
}
