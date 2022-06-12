using System;
using System.Linq;
using Coimbra;
using Mirror;
using SS3D.Core.Networking.PlayerControl.Messages;
using SS3D.Core.Systems.Entities;
using UnityEngine;

namespace SS3D.Core.Networking.PlayerControl
{
    /// <summary>
    /// Controls the player flux, when users want to authenticate, rejoin the game, leave the game
    /// </summary>
    public sealed class PlayerControlManager : NetworkBehaviour
    {
        [SerializeField] private GameObject _soulPrefab;

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
            NetworkServer.RegisterHandler<UserAuthorizationMessage>(HandleAuthorizePlayer);
        }

        /// <summary>
        /// Used by the server to validate credentials and reassign souls to clients.
        /// TODO: Actual authentication
        /// </summary>
        /// <param name="userAuthorizationMessage">struct containing the ckey and the connection that sent it</param>
        [Server]
        public void HandleAuthorizePlayer(NetworkConnectionToClient conn, UserAuthorizationMessage userAuthorizationMessage)
        {
            string ckey = userAuthorizationMessage.Ckey;

            Soul match = null;
            foreach (Soul soul in _serverSouls.Where((soul) => soul.Ckey == ckey))
            {
                match = soul;
                Debug.Log($"[{typeof(PlayerControlManager)}] - SERVER - Soul match for {soul} found, reassigning to client");
            }

            if (match == null)
            {
                Debug.Log($"[{typeof(PlayerControlManager)}] - SERVER - No Soul match for {ckey} found, creating a new one");

                match = Instantiate(_soulPrefab).GetComponent<Soul>();
                match.SetCkey(string.Empty ,ckey);
                _serverSouls.Add(match);

                NetworkServer.Spawn(match.gameObject);
            }
            NetworkServer.AddPlayerForConnection(conn, match.gameObject);

            UserJoinedServerMessage userJoinedServerMessage = new UserJoinedServerMessage(match.Ckey);
            NetworkServer.SendToAll(userJoinedServerMessage);         

            Debug.Log($"[{typeof(PlayerControlManager)}] - SERVER - Handle Authorize Player: {match.Ckey}");
        }
    }
}