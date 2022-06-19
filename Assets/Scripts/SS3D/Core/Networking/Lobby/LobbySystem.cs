using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Networking.Lobby.Events;
using SS3D.Core.Networking.PlayerControl.Messages;
using UnityEngine;

namespace SS3D.Core.Networking.Lobby
{
    /// <summary>
    /// Manages all networked lobby stuff
    /// </summary>
    public sealed class LobbySystem : NetworkBehaviour
    {
        // Current lobby players
        [SyncObject]
        private readonly SyncList<string> _players = new();

        private void Start()
        {
            SyncLobbyPlayers();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            InstanceFinder.ClientManager.RegisterBroadcast<UserJoinedServerMessage>(AddLobbyPlayer);
            InstanceFinder.ClientManager.RegisterBroadcast<UserLeftServerMessage>(RemoveLobbyPlayer);
        }

        /// <summary>
        /// Updates the lobby players on Start
        /// </summary>
        private void SyncLobbyPlayers()
        {
            foreach (string player in _players)
            {
                //IEventService eventService = ServiceLocator.Shared.Get<IEventService>();
                //eventService?.Invoke(null, new UserJoinedLobby(player));
            }                                                                                   
        }

        [Server]
        private void AddLobbyPlayer(UserJoinedServerMessage userJoinedServerMessage)
        {
              _players.Add(userJoinedServerMessage.Ckey);

              RpcAddLobbyPlayer(new UserJoinedLobbyEvent(userJoinedServerMessage.Ckey));
              Debug.Log($"[{nameof(LobbySystem)}] - SERVER - Added player to lobby: {userJoinedServerMessage.Ckey}");
        }

        [ObserversRpc]
        private void RpcAddLobbyPlayer(UserJoinedLobbyEvent userJoinedLobby)
        {
            //IEventService eventService = ServiceLocator.Shared.Get<IEventService>();
            //eventService?.Invoke(null, userJoinedLobby);
            Debug.Log($"[{nameof(LobbySystem)}] - RPC - Added player to lobby: {userJoinedLobby.Ckey}");
        }

        [Server]
        private void RemoveLobbyPlayer(UserLeftServerMessage userLeftServerMessage)
        {
            _players.Remove(userLeftServerMessage.Ckey);

            RpcRemoveLobbyPlayer(new UserLeftLobbyEvent(userLeftServerMessage.Ckey));
            Debug.Log($"[{nameof(LobbySystem)}] - SERVER - Removed player from lobby: {userLeftServerMessage.Ckey}");
        }

        [ObserversRpc]
        private void RpcRemoveLobbyPlayer(UserLeftLobbyEvent userLeftLobby)
        {
            //IEventService eventService = ServiceLocator.Shared.Get<IEventService>();
            //eventService?.Invoke(null, userLeftLobby);

            Debug.Log($"[{nameof(LobbySystem)}] - RPC - Removed player from lobby: {userLeftLobby.Ckey}");
        }
    }
}