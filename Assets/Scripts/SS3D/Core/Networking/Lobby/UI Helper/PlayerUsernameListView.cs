using System.Collections.Generic;
using System.Linq;
using Coimbra;
using UnityEngine;

namespace SS3D.Core.Networking.Lobby.UI_Helper
{
    /// <summary>
    /// Controls the player list in the lobby
    /// </summary>
    public sealed class PlayerUsernameListView : MonoBehaviour
    {
        // The UI element this is linked to
        [SerializeField] private Transform _root;
        
        // Username list, local list that is "networked" by the SyncList on LobbyManager
        [SerializeField] private List<PlayerUsernameView> _playerUsernames;
        
        // The username panel prefab
        [SerializeField] private GameObject _uiPrefab;

        private void Awake()
        {
            SubscribeToEvents();
        }

        // Generic method to agglomerate all event managing
        public void SubscribeToEvents()
        {
            // Uses the event service to listen to lobby events
            IEventService eventService = ServiceLocator.Shared.Get<IEventService>();
            
            eventService!.AddListener<LobbySystem.UserJoinedLobby>(AddUsernameUI);
            eventService!.AddListener<LobbySystem.UserLeftLobby>(RemoveUsernameUI);
        }

        /// <summary>
        /// Adds the new Username to the player list
        /// </summary>
        /// <param name="sender">Required by the ServiceLocator, unused in this function</param>
        /// <param name="data">A PlayerJoinedLobby event, that simply carries the Username</param>
        private void AddUsernameUI(object sender, LobbySystem.UserJoinedLobby data)
        {
            // if this Username already exists we return
            if (_playerUsernames.Exists((player) => data.Ckey == player.Name))
            {
                return;
            }
            
            // adds the UI element and updates the text
            GameObject uiInstance = Instantiate(_uiPrefab, _root);

            PlayerUsernameView playerUsernameView = uiInstance.GetComponent<PlayerUsernameView>();
            playerUsernameView.UpdateNameText(data.Ckey);
            _playerUsernames.Add(playerUsernameView);
        }
        
        /// <summary>
        /// Removes the player from the list based on the Username
        /// </summary>
        /// <param name="sender">Required by the ServiceLocator, unused in this function</param>
        /// <param name="data">A PlayerJoinedLobby event, that simply carries the Username</param>
        private void RemoveUsernameUI(object sender,  LobbySystem.UserLeftLobby data)
        {
            PlayerUsernameView removedUsername = null;
            foreach (PlayerUsernameView playerUsernameUI in _playerUsernames.Where(playerUsernameUI => playerUsernameUI.Name.Equals(data.Ckey)))
            {
                removedUsername = playerUsernameUI;
                Destroy(playerUsernameUI.gameObject);
            }

            _playerUsernames.Remove(removedUsername);
            Destroy(removedUsername != null ? removedUsername.gameObject : null);
        }
    }
}