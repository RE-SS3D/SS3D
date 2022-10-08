using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Logging;
using SS3D.Systems.Lobby.Messages;
using SS3D.Systems.PlayerControl.Messages;
using UnityEngine;

namespace SS3D.Systems.Lobby
{
    /// <summary>
    /// Manages all networked lobby stuff
    /// </summary>
    public sealed class LobbySystem : NetworkBehaviour
    {
        // Current lobby players
        [SyncObject] private readonly SyncList<string> _players = new();

        public List<string> CurrentLobbyPlayers() => _players.ToList();

        public override void OnStartClient()
        {
            base.OnStartClient();

            ClientAddEventListeners();
        }

        private void ClientAddEventListeners()
        {
            InstanceFinder.ClientManager.RegisterBroadcast<UserJoinedServerMessage>(AddLobbyPlayer);
            InstanceFinder.ClientManager.RegisterBroadcast<UserLeftServerMessage>(RemoveLobbyPlayer);
        }

        [Server]
        private void AddLobbyPlayer(UserJoinedServerMessage userJoinedServerMessage)
        {
            _players.Add(userJoinedServerMessage.Ckey);
                                                 
            UserJoinedLobbyMessage userJoinedLobbyMessage = new(userJoinedServerMessage.Ckey);
            InstanceFinder.ServerManager.Broadcast(userJoinedLobbyMessage);

            string message = $"Added player to lobby: {userJoinedServerMessage.Ckey}";
            Punpun.Say(this, message, Logs.ServerOnly);
        }

        [Server]
        private void RemoveLobbyPlayer(UserLeftServerMessage userLeftServerMessage)
        {
            _players.Remove(userLeftServerMessage.Ckey);

            UserLeftLobbyMessage userLeftLobbyMessage = new(userLeftServerMessage.Ckey);
            InstanceFinder.ServerManager.Broadcast(userLeftLobbyMessage);

            string message = $"Removed player from lobby: {userLeftServerMessage.Ckey}"; 
            Punpun.Say(this, message, Logs.ServerOnly);
        }
    }
}