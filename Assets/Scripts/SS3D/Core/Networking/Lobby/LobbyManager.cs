using System;
using Coimbra;
using Mirror;
using SS3D.Core.Networking.PlayerControl.Messages;
using UnityEngine;

namespace SS3D.Core.Networking.Lobby
{
    /// <summary>
    /// Manages all networked lobby stuff
    /// </summary>
    public sealed class LobbyManager : NetworkBehaviour
    {
        // Current lobby players
        private readonly SyncList<string> _players = new SyncList<string>();

        [Serializable]
        public struct UserJoinedLobby
        {
            public string Ckey;

            public UserJoinedLobby(string ckey)
            {
                Ckey = ckey;
            }
        }

        [Serializable]
        public struct UserLeftLobby
        {
            public string Ckey;

            public UserLeftLobby(string ckey)
            {
                Ckey = ckey;
            }
        }

        private void Start()
        {
            SyncLobbyPlayers();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            NetworkClient.RegisterHandler<UserJoinedServerMessage>(AddLobbyPlayer);
            NetworkClient.RegisterHandler<UserLeftServerMessage>(RemoveLobbyPlayer);
        }

        /// <summary>
        /// Updates the lobby players on Start
        /// </summary>
        public void SyncLobbyPlayers()
        {
            foreach (string player in _players)
            {
                IEventService eventService = ServiceLocator.Shared.Get<IEventService>();
                eventService?.Invoke(null, new UserJoinedLobby(player));
            }                                                                                   
        }

        [Server]
        public void AddLobbyPlayer(UserJoinedServerMessage userJoinedServerMessage)
        {
              _players.Add(userJoinedServerMessage.Ckey);

              RpcAddLobbyPlayer(new UserJoinedLobby(userJoinedServerMessage.Ckey));
              Debug.Log($"[{typeof(LobbyManager)}] - SERVER - Added player to lobby: {userJoinedServerMessage.Ckey}");
        }

        [ClientRpc]
        public void RpcAddLobbyPlayer(UserJoinedLobby userJoinedLobby)
        {
            IEventService eventService = ServiceLocator.Shared.Get<IEventService>();
            eventService?.Invoke(null, userJoinedLobby);
            Debug.Log($"[{typeof(LobbyManager)}] - RPC - Added player to lobby: {userJoinedLobby.Ckey}");
        }

        [Server]
        public void RemoveLobbyPlayer(UserLeftServerMessage userLeftServerMessage)
        {
            _players.Remove(userLeftServerMessage.Ckey);

            RpcRemoveLobbyPlayer(new UserLeftLobby(userLeftServerMessage.Ckey));
            Debug.Log($"[{typeof(LobbyManager)}] - SERVER - Removed player from lobby: {userLeftServerMessage.Ckey}");
        }

        [ClientRpc]
        public void RpcRemoveLobbyPlayer(UserLeftLobby userLeftLobby)
        {
            IEventService eventService = ServiceLocator.Shared.Get<IEventService>();
            eventService?.Invoke(null, userLeftLobby);

            Debug.Log($"[{typeof(LobbyManager)}] - RPC - Removed player from lobby: {userLeftLobby.Ckey}");
        }
    }
}