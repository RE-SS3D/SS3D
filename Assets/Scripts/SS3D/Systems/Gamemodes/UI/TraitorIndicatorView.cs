using Coimbra.Services.Events;
using SS3D.Core.Behaviours;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using SS3D.Utils;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.UI
{
    /// <summary>
    /// "You are the Traitor" indicator. Shown briefly when the local player is
    /// assigned an antagonist objective, then auto-hidden via the shared UiFade
    /// component. Visible without holding the Fade input. If the round stops
    /// while it is still showing, it is hidden immediately.
    /// </summary>
    public class TraitorIndicatorView : View
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private string _message = "You are the Traitor";

        [Header("Auto-hide")]
        [SerializeField] private UiFade _fade;
        [SerializeField] private float _displayDuration = 5f;

        private bool _isShowing;

        protected override void OnAwake()
        {
            base.OnAwake();

            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        /// <summary>
        /// Shows the traitor indicator, then auto-hides it after the configured
        /// display duration. Calling Show again restarts the cycle.
        /// </summary>
        public void Show()
        {
            if (_text == null || _fade == null)
            {
                return;
            }

            _text.SetText(_message);
            CancelInvoke(nameof(ScheduledHide));
            _fade.SetFade(true);
            _isShowing = true;
            Invoke(nameof(ScheduledHide), _displayDuration);
        }

        private void ScheduledHide()
        {
            if (_fade != null)
            {
                _fade.SetFade(false);
            }

            _isShowing = false;
        }

        private void Hide()
        {
            CancelInvoke(nameof(ScheduledHide));

            if (!_isShowing)
            {
                return;
            }

            if (_fade != null)
            {
                _fade.SetFade(false);
            }

            _isShowing = false;
        }

        private void HandleRoundStateUpdated(ref EventContext context, in RoundStateUpdated e)
        {
            if (e.RoundState == RoundState.Stopped)
            {
                Hide();
            }
        }
    }
}