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
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Controls a button that starts or stops a round
    /// </summary>
    public class ChangeRoundStateView : Actor
    {
        [NotNull]
        [SerializeField]
        private ToggleLabelButton _startRoundButton;

        protected override void OnAwake()
        {
            base.OnAwake();

            AddEventListeners();
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            _startRoundButton.OnPressedDown -= HandleEmbarkButtonPress;
        }

        private void AddEventListeners()
        {
            _startRoundButton.OnPressedDown += HandleEmbarkButtonPress;

            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            RoundState roundState = e.RoundState;

            bool roundStopped = roundState == RoundState.Stopped;
            bool roundOngoing = roundState == RoundState.Ongoing;
            bool roundWarmingUp = roundState == RoundState.WarmingUp;
            bool roundEnding = roundState == RoundState.Ending;
            bool roundPreparing = roundState == RoundState.Preparing;

            if (roundStopped)
            {
                _startRoundButton.Pressed = false;
                _startRoundButton.Disabled = false;
            }

            if (roundOngoing || roundWarmingUp)
            {
                _startRoundButton.Pressed = true;
                _startRoundButton.Disabled = false;
            }
            else if (roundEnding || roundPreparing)
            {
                _startRoundButton.Disabled = true;
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