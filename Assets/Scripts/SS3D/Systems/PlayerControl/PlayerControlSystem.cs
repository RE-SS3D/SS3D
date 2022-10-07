using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl.Messages;
using UnityEngine;

namespace SS3D.Systems.PlayerControl
{
    /// <summary>
    /// Controls the player flux, when users want to authenticate, rejoin the game, leave the game
    /// </summary>
    public sealed class PlayerControlSystem : NetworkedSystem
    {
        [SerializeField] private GameObject _soulPrefab;

        // TODO: SyncVar because client admins will probably need these, we can later restrict them from the normal user with the permissions system
        [SyncObject]
        private readonly SyncList<Soul> _serverSouls = new();

        public string GetSoulCkey(NetworkConnection conn) => _serverSouls.SingleOrDefault(soul => soul.Owner == conn)?.Ckey;
        public Soul GetSoul(string ckey) => _serverSouls.SingleOrDefault(soul => soul.Ckey == ckey);

        private void Start()
        {
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            InstanceFinder.ServerManager.RegisterBroadcast<UserAuthorizationMessage>(HandleAuthorizePlayer);
        }

        /// <summary>
        /// Used by the server to validate credentials and reassign souls to clients.
        /// TODO: Actual authentication
        /// </summary>
        /// <param name="conn">Network connection</param>
        /// <param name="userAuthorizationMessage">struct containing the ckey and the connection that sent it</param>
        [Server]
        private void HandleAuthorizePlayer(NetworkConnection conn, UserAuthorizationMessage userAuthorizationMessage)
        {
            string ckey = userAuthorizationMessage.Ckey;

            Soul match = GetSoul(ckey);

            if (match != null)
            {
                Debug.Log($"[{nameof(PlayerControlSystem)}] - SERVER - Soul match for {ckey} found, reassigning to client");
            }
            else
            {
                Debug.Log($"[{nameof(PlayerControlSystem)}] - SERVER - No Soul match for {ckey} found, creating a new one");

                match = Instantiate(_soulPrefab).GetComponent<Soul>();
                match.SetCkey(string.Empty ,ckey, true);
                _serverSouls.Add(match);

                InstanceFinder.ServerManager.Spawn(match.gameObject);
            }

            NetworkObject networkObject = match.gameObject.GetComponent<NetworkObject>();
            networkObject.GiveOwnership(conn);

            UserJoinedServerMessage userJoinedServerMessage = new(match.Ckey);
            InstanceFinder.ServerManager.Broadcast(userJoinedServerMessage);         

            Debug.Log($"[{nameof(PlayerControlSystem)}] - SERVER - Handle Authorize Player: {match.Ckey}");
        }
    }
}