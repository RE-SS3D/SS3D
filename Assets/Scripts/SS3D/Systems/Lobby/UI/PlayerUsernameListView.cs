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
using UnityEngine;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Controls the player list in the lobby
    /// </summary>
    public sealed class PlayerUsernameListView : NetworkActor
    {
        // The UI element this is linked to
        [SerializeField]
        [NotNull]
        private Transform _root;

        // Username list, local list that is "networked" by the SyncList on LobbyManager
        [SerializeField]
        [NotNull]
        private List<PlayerUsernameView> _playerUsernames;

        // The username panel prefab
        [SerializeField]
        [NotNull]
        private GameObject _uiPrefab;

        [SerializeField]
        private Color _userReadyColor = PaletteColors.LightBlue;

        public override void OnStartNetwork()
        {
            base.OnStartNetwork();
            SubscribeToEvents();
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            AddHandle(ReadyPlayersChanged.AddListener(HandleReadyPlayersChanged));
        }

        private void SubscribeToEvents()
        {
            AddHandle(OnlinePlayersChanged.AddListener(HandleOnlinePlayersChanged));
        }

        private void HandleOnlinePlayersChanged(ref EventContext context, in OnlinePlayersChanged e)
        {
            string ckey = e.ChangedCkey;

            if (ckey == null)
            {
                return;
            }

            ChangeType changeType = e.ChangeType;

            switch (changeType)
            {
                case ChangeType.Addition:
                {
                    AddUsernameUI(ckey);
                    break;
                }

                case ChangeType.Removal:
                {
                    RemoveUsernameUI(ckey);
                    break;
                }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleReadyPlayersChanged(ref EventContext context, in ReadyPlayersChanged e)
        {
            List<Player> readyPlayers = e.ReadyPlayers;

            foreach (PlayerUsernameView username in _playerUsernames)
            {
                username.UpdateNameColor(readyPlayers.Find(player => player.Ckey == username.Name) ? _userReadyColor : PaletteColors.White);
            }
        }

        /// <summary>
        /// Adds the new Username to the player list
        /// </summary>
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
        private void RemoveUsernameUI(string ckey)
        {
            PlayerUsernameView removedUsername = null;

            foreach (PlayerUsernameView playerUsernameUI in _playerUsernames.Where(playerUsernameUI => playerUsernameUI.Name.Equals(ckey)))
            {
                removedUsername = playerUsernameUI;
                playerUsernameUI.gameObject.Dispose(true);
            }

            if (removedUsername != null)
            {
                _playerUsernames.Remove(removedUsername);
                removedUsername.gameObject.Dispose(true);
            }
        }
    }
}