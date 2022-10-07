using Coimbra.Services.Events;
using FishNet;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using SS3D.UI;
using SS3D.UI.Buttons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Controls a button that starts or stops a round
    /// </summary>
    public class ChangeRoundStateView : SpessBehaviour
    {
        [SerializeField][NotNull] private ToggleLabelButton _startRoundButton;

        private void Start()
        {
            AddEventListeners();
        }

        private void AddEventListeners()
        {
            _startRoundButton.OnPressedDown += HandleEmbarkButtonPress;

            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            bool roundStopped = e.RoundState == RoundState.Stopped;
            bool roundOngoing = e.RoundState == RoundState.Ongoing;
            bool roundWarmingUp = e.RoundState == RoundState.WarmingUp; 

            if (!roundStopped || !roundOngoing || !roundWarmingUp)
            {
                _startRoundButton.Disabled = true;
                _startRoundButton.Pressed = !roundStopped;
            }

            if (roundStopped || roundOngoing || roundWarmingUp)
            {
                _startRoundButton.Disabled = false;
                _startRoundButton.Pressed = !roundStopped;
            }
        }

        private void HandleEmbarkButtonPress(bool state)
        {
            ChangeRoundState(state);
        }

        private void ChangeRoundState(bool state)
        {
            ChangeRoundStateMessage changeRoundStateMessage = new(state);
            InstanceFinder.ClientManager.Broadcast(changeRoundStateMessage);
        }
    }
}
