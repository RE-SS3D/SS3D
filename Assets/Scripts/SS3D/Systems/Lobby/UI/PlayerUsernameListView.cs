using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Data;
using SS3D.Systems.Lobby.Messages;
using SS3D.Systems.Rounds.Events;
using UnityEngine;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Controls the player list in the lobby
    /// </summary>
    public sealed class PlayerUsernameListView : NetworkBehaviour
    {
        // The UI element this is linked to
        [SerializeField] [NotNull] private Transform _root;
        
        // Username list, local list that is "networked" by the SyncList on LobbyManager
        [SerializeField] [NotNull] private List<PlayerUsernameView> _playerUsernames;
        
        // The username panel prefab
        [SerializeField] [NotNull] private GameObject _uiPrefab;
        [SerializeField] private Color _userReadyColor = PaletteColors.LightBlue;

        public override void OnStartClient()
        {
            base.OnStartClient();

            Setup();
            SubscribeToEvents();
        }

        private void Setup()
        {
            SyncLobbyPlayers();
        }

        // Generic method to agglomerate all event managing
        private void SubscribeToEvents()
        {
            ClientManager.RegisterBroadcast<UserJoinedLobbyMessage>(HandleUserJoinedLobby);
            ClientManager.RegisterBroadcast<UserLeftLobbyMessage>(HandleUserLeftLobby);
            ClientManager.RegisterBroadcast<ReadyPlayersChanged>(HandleReadyPlayersChanged);
        }

        private void HandleReadyPlayersChanged(ReadyPlayersChanged m)
        {
            List<string> readyPlayers = m.ReadyPlayers;

            foreach (PlayerUsernameView username in _playerUsernames)
            {
                if (readyPlayers.Contains(username.Name))
                {
                    username.UpdateNameColor(_userReadyColor);
                }
                else
                {
                    username.UpdateNameColor(PaletteColors.White);
                }
            }
        }

        private void HandleUserLeftLobby(UserLeftLobbyMessage m)
        {
            string ckey = m.Ckey;

            RemoveUsernameUI(ckey);
        }

        private void HandleUserJoinedLobby(UserJoinedLobbyMessage m)
        {
            string ckey = m.Ckey;

            AddUsernameUI(ckey);
        }

        /// <summary>
        /// Makes sure the players are shown correct with a late join
        /// </summary>
        private async void SyncLobbyPlayers()
        {
            await UniTask.WaitUntil(() => GameSystems.Get<LobbySystem>() != null);

            LobbySystem lobby = GameSystems.Get<LobbySystem>();

            List<string> lobbyPlayers = lobby.CurrentLobbyPlayers() != null ? lobby.CurrentLobbyPlayers() : new List<string>();

            foreach (string lobbyPlayer in lobbyPlayers)
            {
                AddUsernameUI(lobbyPlayer);
            }
        }

        /// <summary>
        /// Adds the new Username to the player list
        /// </summary>
        /// <param name="sender">Required by the ServiceLocator, unused in this function</param>
        /// <param name="data">A PlayerJoinedLobby event, that simply carries the Username</param>
        private void AddUsernameUI(string ckey)
        {
            // if this Username already exists we return
            if (_playerUsernames.Exists((player) => ckey == player.Name))
            {
                return;
            }
            
            // adds the UI element and updates the text
            GameObject uiInstance = Instantiate(_uiPrefab, _root);

            PlayerUsernameView playerUsernameView = uiInstance.GetComponent<PlayerUsernameView>();
            playerUsernameView.UpdateNameText(ckey);
            _playerUsernames.Add(playerUsernameView);
        }
        
        /// <summary>
        /// Removes the player from the list based on the Username
        /// </summary>
        /// <param name="sender">Required by the ServiceLocator, unused in this function</param>
        /// <param name="data">A PlayerJoinedLobby event, that simply carries the Username</param>
        private void RemoveUsernameUI(string ckey)
        {
            PlayerUsernameView removedUsername = null;

            foreach (PlayerUsernameView playerUsernameUI in _playerUsernames.Where(playerUsernameUI => playerUsernameUI.Name.Equals(ckey)))
            {
                removedUsername = playerUsernameUI;
                playerUsernameUI.gameObject.Destroy();
            }

            _playerUsernames.Remove(removedUsername);
            removedUsername!.gameObject.Destroy();
        }
    }
}