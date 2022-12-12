using Coimbra.Services.Events;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using TMPro;
using UnityEngine;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;
using RoundTickUpdated = SS3D.Systems.Rounds.Events.RoundTickUpdated;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Handles the lobby countdown view
    /// </summary>
    public class RoundStateView : Actor
    {
        [SerializeField][NotNull] private TMP_Text _roundCountdownText;

        private int _seconds;
        private RoundState _roundState;

        protected override void OnStart()
        {
            base.OnStart();

            UpdateRoundCountDownText();
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            AddHandle(RoundStateUpdated.AddListener(HandleRoundStateUpdated));
            AddHandle(RoundTickUpdated.AddListener(HandleRoundTickUpdated));
        }

        private void HandleRoundTickUpdated(ref EventContext context, in RoundTickUpdated roundTickUpdated)
        {
            _seconds = roundTickUpdated.Seconds;

            UpdateRoundCountDownText();
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated roundStateUpdated)
        {
            _roundState = roundStateUpdated.RoundState;

            UpdateRoundCountDownText();
        }

        private void UpdateRoundCountDownText()
        {
            _roundCountdownText.text = $"{_roundState} - {_seconds}";
        }
    }
}
