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
    [RequireComponent(typeof(NetworkObserver))]
    internal sealed class AssetSynchronizer : NetworkActor
    {
        private sealed class PendingLoad : TaskCompletionSource<bool>
        {
            private bool _failed;

            internal PendingLoad(HashSet<int> pendingClientIds)
                : base(TaskCreationOptions.RunContinuationsAsynchronously)
            {
                PendingClientIds = pendingClientIds;
            }

            internal HashSet<int> PendingClientIds { get; }

            internal void Acknowledge(int clientId, bool loaded)
            {
                if (loaded)
                {
                    PendingClientIds.Remove(clientId);
                }
                else
                {
                    _failed = true;
                }

                UpdateState();
            }

            internal void UpdateState()
            {
                if (_failed)
                {
                    TrySetResult(false);
                }

                if (PendingClientIds.Count == 0)
                {
                    TrySetResult(true);
                }
            }
        }

        private const float ClientWaitResponseGraceSeconds = 1f;
        private const int RetryAttempts = 5;

        private readonly HashSet<(string DatabaseId, string AssetId)> _loadHistory = new();

        private readonly Dictionary<(string DatabaseId, string AssetId), TaskCompletionSource<bool>> _clientWaitRequests = new();

        private readonly Dictionary<(string DatabaseId, string AssetId), PendingLoad> _pendingAssetLoads = new();

        // ReSharper disable Unity.PerformanceAnalysis
        public override void OnStartServer()
        {
            base.OnStartServer();
            SceneManager.OnClientLoadedStartScenes += HandleClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
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

            _loadHistory.Clear();

            foreach (PendingLoad pendingLoad in _pendingAssetLoads.Values)
            {
                pendingLoad.TrySetCanceled();
            }

            _pendingAssetLoads.Clear();
        }

        [ServerRpc]
        internal void SynchronizedLoad(ObjectAssetReference assetReference)
        {
            if (!assetReference)
            {
                Log.Warning(this, "Cannot synchronize asset load because the reference is null.");

                return;
            }

            SynchronizedLoad(assetReference.Database, assetReference.Id);
        }

        [ServerRpc]
        internal void SynchronizedLoad(string databaseId, string assetId)
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

            PendingLoad pendingLoad = new(GetConnectedClientIds());
            _pendingAssetLoads.Add((databaseId, assetId), pendingLoad);

            RpcSynchronizedLoad(databaseId, assetId);
        }

        internal async Task<bool> WaitForLoadAsync(string databaseId, string assetId, float timeoutSeconds = 15f)
        {
            if (IsServer)
            {
                return await WaitForLoadServerAsync(databaseId, assetId, timeoutSeconds);
            }

            return await WaitForLoadClientAsync(databaseId, assetId, timeoutSeconds);
        }

        private static bool IsValidRequest([CanBeNull] string databaseId, [CanBeNull] string assetId) =>
            !string.IsNullOrWhiteSpace(databaseId) && !string.IsNullOrWhiteSpace(assetId);

        [ObserversRpc]
        private void RpcSynchronizedLoad([NotNull] string databaseId, [NotNull] string assetId)
        {
            StartLoadAsync(databaseId, assetId);
        }

        [Server]
        private async Task<bool> WaitForLoadServerAsync(string databaseId, string assetId, float timeoutSeconds)
        {
            if (!IsValidRequest(databaseId, assetId))
            {
                Log.Error(this, $"Can't wait to load asset on server with invalid request. Database: '{databaseId}', Asset: '{assetId}'.");

                return false;
            }

            if (!_pendingAssetLoads.TryGetValue((databaseId, assetId), out PendingLoad pendingLoad))
            {
                SynchronizedLoad(databaseId, assetId);
                pendingLoad = _pendingAssetLoads[(databaseId, assetId)];
            }

            try
            {
                return await pendingLoad.Task.WaitWithTimeout(timeoutSeconds);
            }
            catch (TimeoutException)
            {
                Log.Error(this, $"Timed out waiting for pending load '{databaseId}/{assetId}' after {timeoutSeconds:0.##} seconds.");

                return false;
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed while waiting for pending load '{databaseId}/{assetId}'.");

                return false;
            }
            finally
            {
                _pendingAssetLoads.Remove((databaseId, assetId));
            }
        }

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

        [ServerRpc]
        private void RpcWaitForLoadClient(string databaseId, string assetId, float timeoutSeconds, NetworkConnection connection)
        {
            ProcessLoadResult(databaseId, assetId, connection, timeoutSeconds);
        }

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

        private async void StartLoadAsync([NotNull] string databaseId, [NotNull] string assetId)
        {
            try
            {
                for (int attempt = 0; attempt < RetryAttempts; attempt++)
                {
                    Object asset = await AssetLoader.GetAsync<Object>(databaseId, assetId);

                    if (!asset)
                    {
                        continue;
                    }

                    if (IsClient)
                    {
                        RpcHandleAssetLoaded(databaseId, assetId, asset);
                    }

                    return;
                }

                Log.Error(this, $"Failed to load asset '{databaseId}/{assetId}' after {RetryAttempts} attempts.");
            }
            catch (Exception e)
            {
                Log.Error(this, e, $"Failed to load asset '{databaseId}/{assetId}'.");
            }
        }

        [Server]
        private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs stateData)
        {
            switch (stateData.ConnectionState)
            {
                case RemoteConnectionState.Stopped:
                    foreach (PendingLoad pendingAssetLoad in _pendingAssetLoads.Values)
                    {
                        pendingAssetLoad.PendingClientIds.Remove(connection.ClientId);
                        pendingAssetLoad.UpdateState();
                    }

                    break;
                case RemoteConnectionState.Started:
                    foreach (PendingLoad pendingAssetLoad in _pendingAssetLoads.Values)
                    {
                        pendingAssetLoad.PendingClientIds.Add(connection.ClientId);
                        pendingAssetLoad.UpdateState();
                    }

                    break;
                default:
                    Log.Error(this, $"Unknown connection state for ClientID {connection.ClientId}: {stateData.ConnectionState}");

                    break;
            }
        }

        [Server]
        private void HandleClientLoadedStartScenes([CanBeNull] NetworkConnection connection, bool asServer)
        {
            if (!asServer || connection == null || _loadHistory.Count == 0)
            {
                return;
            }

            foreach ((string DatabaseId, string AssetId) request in _loadHistory)
            {
                RpcLoadForClient(connection, request.DatabaseId, request.AssetId);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        [TargetRpc]
        private void RpcLoadForClient(NetworkConnection connection, [NotNull] string databaseId, [NotNull] string assetId)
        {
            StartLoadAsync(databaseId, assetId);
        }

        [ServerRpc]
        private void RpcHandleAssetLoaded([NotNull] string databaseId, [NotNull] string assetId, bool loaded, [CanBeNull] NetworkConnection connection = null)
        {
            if (connection == null || !IsValidRequest(databaseId, assetId))
            {
                return;
            }

            (string DatabaseId, string AssetId) request = (databaseId, assetId);

            if (!_pendingAssetLoads.TryGetValue(request, out PendingLoad pendingAssetLoad))
            {
                return;
            }

            pendingAssetLoad.Acknowledge(connection.ClientId, loaded);

            if (pendingAssetLoad.Task.IsCompleted)
            {
                _pendingAssetLoads.Remove(request);
            }
        }

        private bool TryRegisterSynchronizedLoad(string databaseId, string assetId) => _loadHistory.Add((databaseId, assetId));

        [NotNull]
        private HashSet<int> GetConnectedClientIds()
        {
            return ServerManager.Clients.Values.Where(connection => connection != null && connection.IsActive).Select(connection => connection.ClientId).ToHashSet();
        }
    }
}