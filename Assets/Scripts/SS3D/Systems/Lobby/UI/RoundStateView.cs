using Coimbra.Services.Events;
using JetBrains.Annotations;
using SS3D.Attributes;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Rounds;
using SS3D.Systems.Rounds.Messages;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;
using RoundTickUpdated = SS3D.Systems.Rounds.Events.RoundTickUpdated;

namespace SS3D.Systems.Lobby.UI
{
    /// <summary>
    /// Handles the lobby countdown view
    /// </summary>
    public class RoundStateView : Actor
    {
        [JetBrains.Annotations.NotNull]
        [SerializeField]
        private TMP_Text _roundCountdownText;

        private int _seconds;

        private RoundState _roundState;

        [SerializeField]
        private LocalizedStringTable _localizedStringTable;

        private StringTable _currentStringTable;

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

        private IEnumerator LoadStringTable()
        {
            AsyncOperationHandle<StringTable> tableLoading = _localizedStringTable.GetTableAsync();
            while (!tableLoading.IsDone)
            {
                yield return null;
            }

            _currentStringTable = tableLoading.Result;
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
            string localized = _currentStringTable[_roundState.ToString()].LocalizedValue.Trim();
            _roundCountdownText.text = localized + " - " + _seconds;
        }
    }
}
