using System;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Networking.PlayerControl.Messages;
using SS3D.Core.Systems.Entities;
using UnityEngine;

namespace SS3D.Core.Networking.PlayerControl
{
    /// <summary>
    /// Controls the player flux, when users want to authenticate, rejoin the game, leave the game
    /// </summary>
    public sealed class PlayerControlSystem : NetworkBehaviour
    {
        [SerializeField] private GameObject _soulPrefab;

        [SyncObject]
        private readonly SyncList<Soul> _serverSouls = new SyncList<Soul>();

        [Serializable]
        public struct PlayerLeftServer
        {
            public Soul Soul;

            public PlayerLeftServer(Soul soul) { Soul = soul; }
        }

        private void Awake()
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

            Soul match = null;
            foreach (Soul soul in _serverSouls.Where((soul) => soul.Ckey == ckey))
            {
                match = soul;
                Debug.Log($"[{nameof(PlayerControlSystem)}] - SERVER - Soul match for {soul} found, reassigning to client");
            }

            if (match == null)
            {
                Debug.Log($"[{nameof(PlayerControlSystem)}] - SERVER - No Soul match for {ckey} found, creating a new one");

                match = Instantiate(_soulPrefab).GetComponent<Soul>();
                match.SetCkey(string.Empty ,ckey, true);
                _serverSouls.Add(match);

                InstanceFinder.ServerManager.Spawn(match.gameObject);
            }

            NetworkObject networkObject = match.gameObject.GetComponent<NetworkObject>();
            networkObject.GiveOwnership(conn);

            UserJoinedServerMessage userJoinedServerMessage = new UserJoinedServerMessage(match.Ckey);
            InstanceFinder.ServerManager.Broadcast(userJoinedServerMessage);         

            Debug.Log($"[{nameof(PlayerControlSystem)}] - SERVER - Handle Authorize Player: {match.Ckey}");
        }
    }
}