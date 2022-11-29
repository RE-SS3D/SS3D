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
using UnityEngine.Serialization;

namespace SS3D.Systems.PlayerControl
{
    /// <summary>
    /// Controls the player flux, when users want to authenticate, rejoin the game, leave the game
    /// </summary>
    public sealed class PlayerControlSystem : NetworkedSystem
    {
        [FormerlySerializedAs("_userPrefab")]
        [Header("Settings")]
        [SerializeField] private NetworkObject _unauthorizedUserPrefab;
        [SerializeField] private NetworkObject _soulPrefab;

        [SyncObject] public readonly SyncList<Soul> ServerSouls = new();
        [SyncObject] public readonly SyncList<Soul> OnlineSouls = new();

        protected override void OnStart()
        {
            base.OnStart();
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
            ServerSouls.OnChange += HandleServerSoulsChanged;
            OnlineSouls.OnChange += HandleOnlineSouls;
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

        private void HandleOnlineSouls(SyncListOperation op, int index, Soul oldItem, Soul newItem, bool asServer)
        {
            ChangeType changeType = newItem != null ? ChangeType.Addition : ChangeType.Removal;

            Soul soul = changeType == ChangeType.Addition ? newItem : oldItem;

            OnlineSoulsChanged serverSoulsChanged = new(OnlineSouls.ToList(), changeType, soul);
            serverSoulsChanged.Invoke(this);
        }

        private void HandleServerSoulsChanged(SyncListOperation op, int index, Soul oldItem, Soul newItem, bool asServer)
        {
            ChangeType changeType = newItem != null ? ChangeType.Addition : ChangeType.Removal;

            Soul soul = changeType == ChangeType.Addition ? newItem : oldItem;

            ServerSoulsChanged serverSoulsChanged = new(ServerSouls.ToList(), changeType, soul);
            serverSoulsChanged.Invoke(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conn"></param>
        [Server]
        private void ProcessPlayerJoin(NetworkConnection conn)
        {
            string message = $"Player joined the server - {conn.ClientId} {conn.GetAddress()}";
            Punpun.Say(this, message, Logs.ServerOnly);
            
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

            Soul match = GetSoul(ckey);

            if (match == null)
            {
                Punpun.Say(this, $"No Soul match for {ckey} found, creating a new one", Logs.ServerOnly);

                match = Instantiate(_soulPrefab).GetComponent<Soul>();
                match.UpdateCkey(string.Empty, ckey, true);

                ServerSouls.Add(match);

                ServerManager.Spawn(match.gameObject);
            }
            else
            {
                Punpun.Say(this, $"Soul match for {ckey} found, reassigning to client", Logs.ServerOnly);
            }

            NetworkObject networkObject = match.NetworkObject;
            networkObject.GiveOwnership(conn);
            
            OnlineSouls.Add(match);
        }

        [Server]
        private void ProcessPlayerDisconnect(NetworkConnection conn)
        {
            string message = $"Client {conn.ClientId} {conn.GetAddress()} disconnected"; 
            Punpun.Say(this, message, Logs.ServerOnly);
            
            NetworkObject[] ownedObjects = conn.Objects.ToArray();
            if (ownedObjects.Length == 0)
            {
                Punpun.Panic(this, "No clientOwnedObjects were found", Logs.ServerOnly);
                return;
            }

            foreach (NetworkObject networkIdentity in ownedObjects)
            {
                Punpun.Say(this, $"Client {conn.GetAddress()}'s owned object: {networkIdentity.name}", Logs.ServerOnly);
    
                Soul soul = networkIdentity.GetComponent<Soul>();
                if (soul != null)
                {
                    OnlineSouls.Remove(soul);
                    soul.RemoveOwnership();
                    return;
                }
                networkIdentity.RemoveOwnership();

                Punpun.Say(this, $"Invoking the player server left event: {soul.Ckey}", Logs.ServerOnly);
            }
        }

        public string GetCkey(NetworkConnection conn)
        {
            return ServerSouls.Find(soul => soul.Owner == conn)?.Ckey;
        }

        public Soul GetSoul(string ckey)
        {
            return ServerSouls.Find(soul => soul.Ckey == ckey);
        }

        public Soul GetSoul(NetworkConnection conn)
        {
            return ServerSouls.Find(soul => soul.Owner == conn);
        }
    }
}