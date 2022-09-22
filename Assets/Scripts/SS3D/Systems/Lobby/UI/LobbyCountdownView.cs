using System;
using FishNet;
using FishNet.Object;
using SS3D.Core;
using SS3D.Core.Attributes;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Handles the lobby countdown view
    /// </summary>
    public class LobbyCountdownView : NetworkedSpessBehaviour
    {
        [SerializeField][NotNull] private TMP_Text _roundCountdownText;

        private RoundSystem _roundSystem;

        public override void OnStartClient()
        {
            base.OnStartClient();

            UpdateRoundCountDownText();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            InstanceFinder.ClientManager.RegisterBroadcast<RoundStateUpdatedMessage>(HandleRoundStateUpdated);
            InstanceFinder.ClientManager.RegisterBroadcast<RoundTickUpdatedMessage>(HandleRoundTickUpdated);
        }

        private void HandleRoundTickUpdated(RoundTickUpdatedMessage m)
        {
            UpdateRoundCountDownText();
        }

        private void HandleRoundStateUpdated(RoundStateUpdatedMessage m)
        {
            UpdateRoundCountDownText();
        }

        private void UpdateRoundCountDownText()
        {
            if (_roundSystem == null)
            {
                _roundSystem = GameSystems.Get<RoundSystem>();
            }

            if (_roundSystem != null)
            {
                _roundCountdownText.text = $"{_roundSystem.RoundState} - {_roundSystem.RoundSeconds}";
            }
        }
    }
}
