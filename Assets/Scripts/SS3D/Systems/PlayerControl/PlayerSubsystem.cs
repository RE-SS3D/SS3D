using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Systems.PlayerControl.Messages;
using UnityEngine;

namespace SS3D.Systems.PlayerControl
{
    /// <summary>
    /// Controls the player flux, when users want to authenticate, rejoin the game, leave the game
    /// </summary>
    public sealed class PlayerSubsystem : NetworkSubsystem
    {
        [Header("Settings")]
        [SerializeField]
        private NetworkObject _unauthorizedUserPrefab;

        [SerializeField]
        private Soul _soulPrefab;

        [SyncObject]
        private readonly SyncDictionary<string, Soul> _serverSouls = new();
        [SyncObject]
        private readonly SyncDictionary<string, Soul> _onlineSouls = new();

        public IEnumerable<Soul> ServerSouls => _serverSouls.Values;
        public IEnumerable<Soul> OnlineSouls => _onlineSouls.Values;

        protected override void OnStart()
        {
            base.OnStart();

            LateSyncOnlineSouls();
            LateSyncServerSouls();

            AddEventListeners();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerManager.RegisterBroadcast<UserAuthorizationMessage>(ProcessAuthorizePlayer);

            SceneManager.OnClientLoadedStartScenes += HandleClientLoadedStartScenes;
            ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
        }

        private void AddEventListeners()
        {
            _serverSouls.OnChange += HandleSyncServerSoulsChanged;
            _onlineSouls.OnChange += HandleSyncOnlineSouls;
        }

        private void HandleRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs remoteConnectionStateArgs)
        {
            if (remoteConnectionStateArgs.ConnectionState == RemoteConnectionState.Stopped)
            {
                ProcessPlayerDisconnect(conn);
            }
        }

        private void HandleClientLoadedStartScenes(NetworkConnection conn, bool asServer)
        {
            ProcessPlayerJoin(conn);
        }

        private void HandleSyncOnlineSouls(SyncDictionaryOperation op, string key, Soul value, bool asServer)
        {
            ChangeType changeType;

            if (key == null)
            {
                return;
            }

            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    changeType = ChangeType.Addition;
                    break;
                case SyncDictionaryOperation.Remove:
                    changeType = ChangeType.Removal;
                    break;
                default:
                    changeType = ChangeType.Addition;
                    break;
            }

            OnlineSoulsChanged serverSoulsChanged = new(_onlineSouls.Values.ToList(), changeType, value, key, asServer);
            serverSoulsChanged.Invoke(this);
        }

        private void LateSyncOnlineSouls()
        {
            foreach (Soul soul in _onlineSouls.Values)
            {
                HandleSyncOnlineSouls(SyncDictionaryOperation.Add, soul.Ckey, soul, false);
            }
        }

        private void HandleSyncServerSoulsChanged(SyncDictionaryOperation op, string key, Soul value, bool asServer)
        {
            ChangeType changeType;

            switch (op)
            {
                case SyncDictionaryOperation.Add:
                    changeType = ChangeType.Addition;
                    break;
                case SyncDictionaryOperation.Remove:
                    changeType = ChangeType.Removal;
                    break;
                default:
                    changeType = ChangeType.Addition;
                    break;
            }

            ServerSoulsChanged serverSoulsChanged = new(_serverSouls.Values.ToList(), changeType, value);
            serverSoulsChanged.Invoke(this);
        }

        private void LateSyncServerSouls()
        {
            foreach (Soul soul in _serverSouls.Values)
            {
                HandleSyncOnlineSouls(SyncDictionaryOperation.Add, soul.Ckey, soul, false);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="conn"></param>
        [Server]
        private void ProcessPlayerJoin(NetworkConnection conn)
        {
            string message = $"Player joined the server - {conn.ClientId} {conn.GetAddress()}";
            Punpun.Information(this, "Player joined the server - {clientId} {connectionAddress}",
                Logs.ServerOnly, conn.ClientId, conn.GetAddress());

            NetworkObject unauthorizedUser = Instantiate(_unauthorizedUserPrefab, Vector3.zero, Quaternion.identity);
            ServerManager.Spawn(unauthorizedUser, conn);
        }

        /// <summary>
        /// Used by the server to validate credentials and reassign souls to clients.
        /// TODO: Actual authentication
        /// </summary>
        /// <param name="conn">Network connection</param>
        /// <param name="userAuthorizationMessage">struct containing the ckey and the connection that sent it</param>
        [Server]
        private void ProcessAuthorizePlayer(NetworkConnection conn, UserAuthorizationMessage userAuthorizationMessage)
        {
            string ckey = userAuthorizationMessage.Ckey;
            bool hasSoul = _serverSouls.TryGetValue(ckey, out Soul soul);

            if (!hasSoul)
            {
                Punpun.Information(this, "No Soul match for {ckey} found, creating a new one", Logs.ServerOnly, ckey);

                soul = Instantiate(_soulPrefab);
                ServerManager.Spawn(soul.gameObject);

                soul.SetCkey(ckey);

                _serverSouls.Add(ckey, soul);
            }
            else
            {
                Punpun.Information(this, "Soul match for {ckey} found, reassigning to client", Logs.ServerOnly, ckey);
            }

            soul.GiveOwnership(conn);
            _onlineSouls.Add(ckey, soul);
        }

        [Server]
        private void ProcessPlayerDisconnect(NetworkConnection conn)
        {
            string message = $"Client {conn.ClientId} {conn.GetAddress()} disconnected";
            Punpun.Information(this, "Client {clientId} {connectionAddress} disconnected", Logs.ServerOnly, conn.ClientId, conn.GetAddress());

            NetworkObject[] ownedObjects = conn.Objects.ToArray();
            if (ownedObjects.Length == 0)
            {
                Punpun.Warning(this, "No clientOwnedObjects were found", Logs.ServerOnly);
                return;
            }

            foreach (NetworkObject networkIdentity in ownedObjects)
            {
                Punpun.Information(this, "Client {connectionAddress}'s owned object: {networkIdentity}",
                    Logs.ServerOnly, conn.GetAddress(), networkIdentity.name);

                Soul soul = networkIdentity.GetComponent<Soul>();
                if (soul != null)
                {
                    _onlineSouls.Remove(soul.Ckey);
                    soul.RemoveOwnership();
                    Punpun.Information(this, "Invoking the player server left event: {ckey}", Logs.ServerOnly, soul.Ckey);

                    return;
                }
                networkIdentity.RemoveOwnership();
            }
        }

        public string GetCkey(NetworkConnection conn)
        {
            return ServerSouls.ToList().Find(soul => soul.Owner == conn)?.Ckey;
        }

        public Soul GetSoul(string ckey)
        {
            return ServerSouls.ToList().Find(soul => soul.Ckey == ckey);
        }

        public Soul GetSoul(NetworkConnection conn)
        {
            return ServerSouls.ToList().Find(soul => soul.Owner == conn);
        }
    }
}