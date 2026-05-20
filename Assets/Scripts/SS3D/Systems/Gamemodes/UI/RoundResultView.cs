using Coimbra.Services.Events;
using SS3D.Core.Behaviours;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Events;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Gamemodes.UI
{
    /// <summary>
    /// Persistent round result banner (for example "The traitors have won!").
    /// Driven by a server gamemode announcement and visible without holding the
    /// Fade input. Clears itself when the round stops.
    /// </summary>
    public class RoundResultView : View
    {
        [SerializeField] private TMP_Text _text;

        protected override void OnAwake()
        {
            base.OnAwake();

            RoundStateUpdated.AddListener(HandleRoundStateUpdated);
        }

        protected override void OnStart()
        {
            base.OnStart();

            Hide();
        }

        /// <summary>
        /// Shows the given round result message to the player.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void Show(string message)
        {
            if (_text == null)
            {
                return;
            }

            _text.SetText(message);
            _text.gameObject.SetActive(true);
        }

        private void Hide()
        {
            if (_text == null)
            {
                return;
            }

            _text.gameObject.SetActive(false);
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
