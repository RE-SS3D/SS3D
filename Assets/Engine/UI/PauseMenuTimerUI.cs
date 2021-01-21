using System;
using SS3D.Engine.Server.Round;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;


namespace SS3D.UI
{
    /// <summary>
    /// UI controller for the timer UI in the pause menu
    /// </summary>
    public class PauseMenuTimerUI : MonoBehaviour
    {
        private RoundManager roundManager;

        [SerializeField] private TextMeshProUGUI timerText = null;
        [SerializeField] private RectTransform timerUi = null;

        private void Start()
        {
            roundManager = RoundManager.singleton;
            
            // why do we subscribe UI class to server events?
            // doesn't make much sense
            RoundManager.ServerWarmupStarted += OnWarmupStarted;
            RoundManager.ServerRoundStarted += OnRoundStarted;
            RoundManager.ClientTimerUpdated += OnTimerUpdated;
        }

        private void OnTimerUpdated(int time)
        {
            if (!timerUi.gameObject.activeSelf)
            {
                timerUi.gameObject.SetActive(true);
            }

            timerText.text = TimeSpan.FromSeconds(time).ToString("hh\\:mm\\:ss");
        }

        private void OnRoundStarted()
        {
            gameObject.SetActive(true);
        }

        private void OnWarmupStarted()
        {
            gameObject.SetActive(true);
        }

        public void OnEndRoundButtonPressed()
        {
            RoundManager.ServerWarmupStarted -= OnWarmupStarted;
            RoundManager.ServerRoundStarted -= OnRoundStarted;
            RoundManager.ClientTimerUpdated -= OnTimerUpdated;
            roundManager?.EndRound();
        }

        private void OnDestroy()
        {
            RoundManager.ServerWarmupStarted -= OnWarmupStarted;
            RoundManager.ServerRoundStarted -= OnRoundStarted;
            RoundManager.ClientTimerUpdated -= OnTimerUpdated;
        }
    }
}