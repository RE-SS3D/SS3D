using Coimbra.Services.Events;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Localization;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;
using RoundTickUpdated = SS3D.Systems.Rounds.Events.RoundTickUpdated;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Handles the lobby countdown view
    /// </summary>
    public class RoundStateView : Actor
    {
        [NotNull]
        [SerializeField]
        private TMP_Text _roundCountdownText;

        private int _seconds;
        private RoundState _roundState;
        [SerializeField] private LocalizedStringTable _localizedStringTable;

        public void UpdateLocalization()
        {
            StartCoroutine(LoadStringTable());
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            AddEventListeners();
            StartCoroutine(LoadStringTable());
        }

        protected override void OnEnabled()
        {
            base.OnEnabled();
            LocalizedTextService.EnsureInitialized();
            LocalizedTextService.LocaleChanged += HandleLocaleChanged;
        }

        protected override void OnDisabled()
        {
            base.OnDisabled();
            LocalizedTextService.LocaleChanged -= HandleLocaleChanged;
        }

        private void HandleLocaleChanged()
        {
            StartCoroutine(LoadStringTable());
        }

        private IEnumerator LoadStringTable()
        {
            yield return LocalizedTextService.PreloadTableAsync(_localizedStringTable);
            UpdateRoundCountDownText();
        }

        private void AddEventListeners()
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
            string localized = LocalizedTextService
                .GetString(_localizedStringTable, _roundState.ToString(), _roundState.ToString())
                .Trim();
            _roundCountdownText.text = localized + " - " + _seconds;
        }
    }
}
