using FishNet;
using FishNet.Object;
using SS3D.Core.Rounds.Messages;
using SS3D.Core.Systems.Rounds;
using TMPro;
using UnityEngine;

namespace SS3D.Core.Networking.Lobby.View
{
    // Handles the lobby countdown view
    public class LobbyCountdownView : NetworkBehaviour
    {
        [SerializeField] private TMP_Text _roundCountdownText;

        private int _roundSeconds;
        private RoundState _roundState;
        
        private void Start()
        {
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            InstanceFinder.ClientManager.RegisterBroadcast<RoundStateUpdatedMessage>(HandleRoundStateUpdated);
            InstanceFinder.ClientManager.RegisterBroadcast<RoundTickUpdatedMessage>(HandleRoundTickUpdated);
        }

        private void HandleRoundTickUpdated(RoundTickUpdatedMessage m)
        {
            _roundSeconds = m.Seconds;

            UpdateRoundCountDownText();
        }

        private void HandleRoundStateUpdated(RoundStateUpdatedMessage m)
        {
            _roundState = m.RoundState;
            
            UpdateRoundCountDownText();
        }

        private void UpdateRoundCountDownText()
        {
            _roundCountdownText.text = $"{_roundState} - {_roundSeconds}";
        }
    }
}
