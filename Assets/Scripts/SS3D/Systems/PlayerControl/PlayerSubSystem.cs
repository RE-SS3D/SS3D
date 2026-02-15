using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Permissions;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Systems.PlayerControl.Messages;
using UnityEngine;

namespace SS3D.Systems.PlayerControl
{
    /// <summary>
    /// Controls the player flux, when users want to authenticate, rejoin the game, leave the game
    /// </summary>
    public sealed class PlayerSubSystem : NetworkSubSystem
    {
        [Header("Settings")]
        [SerializeField]
        private NetworkObject _unauthorizedUserPrefab;

        [SerializeField]
        private Player _playerPrefab;

        [SyncObject]
        private readonly SyncDictionary<string, Player> _serverPlayers = new();
        [SyncObject]
        private readonly SyncDictionary<string, Player> _onlinePlayers = new();

        public IEnumerable<Player> ServerPlayers => _serverPlayers.Values;
        public IEnumerable<Player> OnlinePlayers => _onlinePlayers.Values;

        protected override void OnStart()
        {
            base.OnStart();

            LateSyncOnlinePlayers();
            LateSyncServerPlayers();

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
            _serverPlayers.OnChange += HandleSyncServerPlayersChanged;
            _onlinePlayers.OnChange += HandleSyncOnlinePlayers;
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

        private void HandleSyncOnlinePlayers(SyncDictionaryOperation op, string key, Player value, bool asServer)
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

            OnlinePlayersChanged serverPlayersChanged = new(_onlinePlayers.Values.ToList(), changeType, value, key, asServer);
            serverPlayersChanged.Invoke(this);
        }

        private void LateSyncOnlinePlayers()
        {
            foreach (Player player in _onlinePlayers.Values)
            {
                HandleSyncOnlinePlayers(SyncDictionaryOperation.Add, player.Ckey, player, false);
            }
        }

        private void HandleSyncServerPlayersChanged(SyncDictionaryOperation op, string key, Player value, bool asServer)
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

            ServerPlayersChanged serverPlayersChanged = new(_serverPlayers.Values.ToList(), changeType, value);
            serverPlayersChanged.Invoke(this);
        }

        private void LateSyncServerPlayers()
        {
            foreach (Player player in _serverPlayers.Values)
            {
                HandleSyncOnlinePlayers(SyncDictionaryOperation.Add, player.Ckey, player, false);
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
            Log.Information(this, "Player joined the server - {clientId} {connectionAddress}", Logs.ServerOnly, conn.ClientId, conn.GetAddress());

            NetworkObject unauthorizedUser = Instantiate(_unauthorizedUserPrefab, Vector3.zero, Quaternion.identity);
            ServerManager.Spawn(unauthorizedUser, conn);
        }

        /// <summary>
        /// Used by the server to validate credentials and reassign players to clients.
        /// TODO: Actual authentication
        /// </summary>
        /// <param name="conn">Network connection</param>
        /// <param name="userAuthorizationMessage">struct containing the ckey and the connection that sent it</param>
        [Server]
        private void ProcessAuthorizePlayer(NetworkConnection conn, UserAuthorizationMessage userAuthorizationMessage)
        {
            string ckey = userAuthorizationMessage.Ckey;
            bool playedHasConnectedAlready = _serverPlayers.TryGetValue(ckey, out Player player);

            if (playedHasConnectedAlready)
            {
                Log.Information(this, "Player match for {ckey} found, reassigning to client", Logs.ServerOnly, ckey);
            }
            else
            {
                Log.Information(this, "No Player match for {ckey} found, creating a new one", Logs.ServerOnly, ckey);

                player = Instantiate(_playerPrefab);
                ServerManager.Spawn(player.gameObject);

                player.SetCkey(ckey);

                if (conn.IsHost && PermissionSettings.AddServerOwnerPermissionToServerHost)
                {
                    Log.Information(this, $"Adding ServerOwner permission to server owner: {ckey}", Logs.ServerOnly, ckey);

                    SubSystems.Get<PermissionSubSystem>().ChangeUserPermission(ckey, ServerRoleTypes.ServerOwner);
                }

                _serverPlayers.Add(ckey, player);
            }

            player.GiveOwnership(conn);

            bool hasOnlinePlayer = _onlinePlayers.TryGetValue(ckey, out Player onlinePlayer);
            if (!hasOnlinePlayer)
            {
                _onlinePlayers.Add(ckey, player);
            }
        }

        [Server]
        private void ProcessPlayerDisconnect(NetworkConnection conn)
        {
            string message = $"Client {conn.ClientId} {conn.GetAddress()} disconnected";
            Log.Information(this, "Client {clientId} {connectionAddress} disconnected", Logs.ServerOnly, conn.ClientId, conn.GetAddress());

            NetworkObject[] ownedObjects = conn.Objects.ToArray();
            if (ownedObjects.Length == 0)
            {
                Log.Warning(this, "No clientOwnedObjects were found", Logs.ServerOnly);
                return;
            }

            foreach (NetworkObject networkIdentity in ownedObjects)
            {
                Log.Information(this, "Client {connectionAddress}'s owned object: {networkIdentity}",
                    Logs.ServerOnly, conn.GetAddress(), networkIdentity.name);

                Player player = networkIdentity.GetComponent<Player>();
                if (player != null)
                {
                    _onlinePlayers.Remove(player.Ckey);
                    player.RemoveOwnership();
                    Log.Information(this, "Invoking the player server left event: {ckey}", Logs.ServerOnly, player.Ckey);

                    return;
                }
                networkIdentity.RemoveOwnership();
            }
        }

        public string GetCkey(NetworkConnection conn)
        {
            return ServerPlayers.ToList().Find(player => player.Owner == conn)?.Ckey;
        }

        public Player GetPlayer(string ckey)
        {
            return ServerPlayers.ToList().Find(player => player.Ckey == ckey);
        }

        public Player GetPlayer(NetworkConnection conn)
        {
            return ServerPlayers.ToList().Find(player => player.Owner == conn);
        }
    }
}