using SS3D.Engine.Server.Round;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SS3D.UI
{
    public class RoundManagerUI : MonoBehaviour
    {
        public RoundManager roundManager;

        [SerializeField] private TextMeshProUGUI timerText = null;
        [SerializeField] private RectTransform timerUi = null;
        [SerializeField] private RectTransform controlUi = null;

        private void Awake()
        {
            if (roundManager)
            {
                // why do we subscribe UI class to server events?
                // doesn't make much sense
                roundManager.Server_OnWarmupStarted += OnWarmupStarted;
                roundManager.Server_OnRoundStarted += OnRoundStarted;
                roundManager.Client_OnTimmerUpdated += OnTimmerUpdated;
            }
        }

        private void OnTimmerUpdated(string text)
        {
            if (!timerUi.gameObject.activeSelf)
            {
                timerUi.gameObject.SetActive(true);
            }

            timerText.text = text;
        }

        private void OnRoundStarted()
        {
            gameObject.SetActive(true);
            controlUi?.gameObject.SetActive(true);
        }

        private void OnWarmupStarted()
        {
            gameObject.SetActive(true);
        }

        public void OnRestartRoundButtonPressed()
        {
            roundManager?.RestartRound();
        }
    }
}