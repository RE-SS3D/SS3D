using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Networking;
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

        // SyncDictionary can briefly contain a null value on a late-joining client before the
        // referenced Player NetworkObject is spawned. Never expose those nulls to callers.
        public IEnumerable<Player> ServerPlayers => _serverPlayers.Values.Where(player => player != null);
        public IEnumerable<Player> OnlinePlayers => _onlinePlayers.Values.Where(player => player != null);

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
            // Host also fires this with asServer=false after the client-side load path.
            // Spawning there runs while LoadedStartScenes(asServer:true) is still false and
            // can create a second UnauthorizedPlayer — match FishNet PlayerSpawner.
            if (!asServer)
            {
                return;
            }

            ProcessPlayerJoin(conn);
        }

        private void HandleSyncOnlinePlayers(SyncDictionaryOperation op, string key, Player value, bool asServer)
        {
            ChangeType changeType;

            // A late-joining client can receive the dictionary entry before the referenced
            // Player NetworkObject is spawned, so value is briefly null. Skip it; the real
            // value arrives through a later sync operation.
            if (key == null || value == null)
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

            OnlinePlayersChanged serverPlayersChanged = new(
                _onlinePlayers.Values.Where(player => player != null).ToList(), changeType, value, key, asServer);
            serverPlayersChanged.Invoke(this);
        }

        private void LateSyncOnlinePlayers()
        {
            foreach (Player player in _onlinePlayers.Values)
            {
                // Initial SyncDictionary replication can expose a null Player before its
                // NetworkObject is spawned on a late-joining client (seen with 8 clients
                // connecting at once); skip it and let the later sync deliver the value.
                if (player == null)
                {
                    continue;
                }

                HandleSyncOnlinePlayers(SyncDictionaryOperation.Add, player.Ckey, player, false);
            }
        }

        private void HandleSyncServerPlayersChanged(SyncDictionaryOperation op, string key, Player value, bool asServer)
        {
            ChangeType changeType;

            if (key == null || value == null)
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

            ServerPlayersChanged serverPlayersChanged = new(
                _serverPlayers.Values.Where(player => player != null).ToList(), changeType, value);
            serverPlayersChanged.Invoke(this);
        }

        private void LateSyncServerPlayers()
        {
            foreach (Player player in _serverPlayers.Values)
            {
                if (player == null)
                {
                    continue;
                }

                HandleSyncServerPlayersChanged(SyncDictionaryOperation.Add, player.Ckey, player, false);
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="conn"></param>
        [Server]
        private void ProcessPlayerJoin(NetworkConnection conn)
        {
            Log.Debug(this, "Player joined the server - {clientId} {connectionAddress}", Logs.ServerOnly, conn.ClientId, conn.GetAddress());

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
            if (SubSystems.TryGet(out SS3D.Systems.Rounds.RoundSubSystem rounds)
                && _onlinePlayers.Count >= rounds.PlayerCap
                && !_onlinePlayers.ContainsKey(userAuthorizationMessage.Ckey))
            {
                ConnectionNotices.Disconnect(conn, DisconnectCode.ServerFull);
                return;
            }
            string ckey = userAuthorizationMessage.Ckey;

            if (!conn.IsHost
                && SubSystems.TryGet(out PermissionSubSystem permissions)
                && permissions.TryGetUserRole(ckey, out ServerRoleTypes role)
                && !AdminTokens.IsAdmitted(role, ckey, userAuthorizationMessage.Token, IsServerOnly, out string reason))
            {
                Log.Warning(this, "Rejected login from client {clientId}: {reason}", Logs.Important, conn.ClientId, reason);
                ConnectionNotices.Disconnect(conn, DisconnectCode.AdminToken);
                return;
            }

            bool playedHasConnectedAlready = _serverPlayers.TryGetValue(ckey, out Player player) && player != null;

            if (playedHasConnectedAlready)
            {
                Log.Debug(this, "Player match for {ckey} found, reassigning to client", Logs.ServerOnly, ckey);
            }
            else
            {
                Log.Debug(this, "No Player match for {ckey} found, creating a new one", Logs.ServerOnly, ckey);

                player = Instantiate(_playerPrefab);
                ServerManager.Spawn(player.gameObject);

                player.SetCkey(ckey);

                if (conn.IsHost && PermissionSettings.AddServerOwnerPermissionToServerHost)
                {
                    Log.Debug(this, $"Adding ServerOwner permission to server owner: {ckey}", Logs.ServerOnly, ckey);

                    SubSystems.Get<PermissionSubSystem>().ChangeUserPermission(ckey, ServerRoleTypes.ServerOwner);
                }

                // A previous sync can leave the key present with a null value; replace it
                // instead of throwing ArgumentException on a duplicate Add.
                if (_serverPlayers.ContainsKey(ckey))
                {
                    _serverPlayers[ckey] = player;
                }
                else
                {
                    _serverPlayers.Add(ckey, player);
                }
            }

            player.GiveOwnership(conn);

            bool hasOnlinePlayer = _onlinePlayers.TryGetValue(ckey, out Player onlinePlayer) && onlinePlayer != null;
            if (!hasOnlinePlayer)
            {
                if (_onlinePlayers.ContainsKey(ckey))
                {
                    _onlinePlayers[ckey] = player;
                }
                else
                {
                    _onlinePlayers.Add(ckey, player);
                }
            }

            // If this player already had a body in the round (e.g. they disconnected and are
            // rejoining), reconnect them to it instead of leaving it stranded, ownerless, in the world.
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            if (entitySystem.IsPlayerSpawned(player))
            {
                entitySystem.TryReclaimEntity(player, conn);
            }

            DespawnUnauthorizedPlayer(conn);
            ConnectionNotices.AcceptLogin(conn);
        }

        [Server]
        private void DespawnUnauthorizedPlayer(NetworkConnection conn)
        {
            // Snapshot: Despawn mutates conn.Objects. FishNet's IsDeinitializing is internal;
            // IsSpawned is the public gate for "safe to despawn".
            foreach (NetworkObject networkObject in conn.Objects.ToArray())
            {
                if (networkObject == null || !networkObject.IsSpawned)
                {
                    continue;
                }

                if (networkObject.GetComponent<UnauthorizedPlayer>() == null)
                {
                    continue;
                }

                ServerManager.Despawn(networkObject);
            }
        }

        [Server]
        private void ProcessPlayerDisconnect(NetworkConnection conn)
        {
            Log.Debug(this, "Client {clientId} {connectionAddress} disconnected", Logs.ServerOnly, conn.ClientId, conn.GetAddress());

            NetworkObject[] ownedObjects = conn.Objects.ToArray();
            if (ownedObjects.Length == 0)
            {
                Log.Warning(this, "No clientOwnedObjects were found", Logs.ServerOnly);
                return;
            }

            foreach (NetworkObject networkIdentity in ownedObjects)
            {
                Log.Debug(this, "Client {connectionAddress}'s owned object: {networkIdentity}",
                    Logs.ServerOnly, conn.GetAddress(), networkIdentity.name);

                // A login that never passed leaves this placeholder. It must not outlive its connection.
                if (networkIdentity.GetComponent<UnauthorizedPlayer>() != null)
                {
                    ServerManager.Despawn(networkIdentity);
                    continue;
                }

                Player player = networkIdentity.GetComponent<Player>();
                if (player != null)
                {
                    _onlinePlayers.Remove(player.Ckey);
                    Log.Debug(this, "Invoking the player server left event: {ckey}", Logs.ServerOnly, player.Ckey);
                }

                // conn.Objects is a HashSet with no defined order, and this connection is disconnecting
                // regardless, so every owned object (Player, spawned Entity/body, held items, etc.) must have
                // its ownership released here - returning early after the first Player match left whichever
                // objects hadn't been enumerated yet (frequently the player's own body) still bound to the
                // now-dead connection.
                networkIdentity.RemoveOwnership();
            }
        }

        public string GetCkey(NetworkConnection conn)
        {
            return ServerPlayers.FirstOrDefault(player => player != null && player.Owner == conn)?.Ckey;
        }

        public Player GetPlayer(string ckey)
        {
            return ServerPlayers.FirstOrDefault(player => player != null && player.Ckey == ckey);
        }

        public Player GetPlayer(NetworkConnection conn)
        {
            return ServerPlayers.FirstOrDefault(player => player != null && player.Owner == conn);
        }
    }
}