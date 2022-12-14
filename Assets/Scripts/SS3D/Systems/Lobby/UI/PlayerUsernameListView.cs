using System;
using System.Collections.Generic;
using System.Linq;
using Coimbra;
using Coimbra.Services.Events;
using SS3D.Attributes;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Systems.Entities;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.PlayerControl.Events;
using SS3D.Systems.Rounds.Events;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Controls the player list in the lobby
    /// </summary>
    public sealed class PlayerUsernameListView : NetworkedSpessBehaviour
    {
        // The UI element this is linked to
        [SerializeField] [NotNull] private Transform _root;
        
        // Username list, local list that is "networked" by the SyncList on LobbyManager
        [SerializeField] [NotNull] private List<PlayerUsernameView> _playerUsernames;
        
        // The username panel prefab
        [SerializeField] [NotNull] private GameObject _uiPrefab;
        [SerializeField] private Color _userReadyColor = PaletteColors.LightBlue;

        protected override void OnAwake()
        {
            base.OnAwake();

            ReadyPlayersChanged.AddListener(HandleReadyPlayersChanged);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            OnlineSoulsChanged.AddListener(HandleOnlineSoulsChanged);
        }

        private void HandleOnlineSoulsChanged(ref EventContext context, in OnlineSoulsChanged e)
        {
            Soul soul = e.Changed;

            if (soul == null)
            {
                return;
            }

            ChangeType changeType = e.ChangeType;
            string ckey = soul.Ckey;

            switch (changeType)
            {
                case ChangeType.Addition:
                    AddUsernameUI(ckey);
                    break;
                case ChangeType.Removal:
                    RemoveUsernameUI(ckey);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleReadyPlayersChanged(ref EventContext context, in ReadyPlayersChanged e)
        {
            List<string> readyPlayers = e.ReadyPlayers;

            foreach (PlayerUsernameView username in _playerUsernames)
            {
                username.UpdateNameColor(readyPlayers.Contains(username.Name) ? _userReadyColor : PaletteColors.White);
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

            if (removedUsername != null)
            {
                _playerUsernames.Remove(removedUsername);
                removedUsername.gameObject.Destroy();
            }
        }
    }
}