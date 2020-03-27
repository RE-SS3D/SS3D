using System;
using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Round
{
    /// <summary>
    /// Behaviour responsible for syncing timers between server and clients.
    /// Also handles starting and restarting rounds.
    /// Should be attached to the RoundManager prefab.
    /// </summary>
    public class RoundManager : NetworkBehaviour
    {
        //How long will the warmup period be. Starts immediately on server start.
        [SerializeField] private int warmupTimeSeconds = 30;
        //Text element that stores the timer
        [SerializeField] private TextMeshProUGUI timerText = null;
        //UI element that contains the round timer
        [SerializeField] private RectTransform timerUi = null;
        //UI element that contains the round controls
        [SerializeField] private RectTransform controlUi = null;

        private int timerSeconds = 0;
        private bool started = false;
        private Coroutine tickCoroutine;
            
        public bool IsRoundStarted => started;

        public void StartWarmup()
        {
            timerSeconds = warmupTimeSeconds;
            StartCoroutine(TickWarmup());
        }

        public void StartRound()
        {
            started = true;
            controlUi.gameObject.SetActive(true);
            tickCoroutine = StartCoroutine(Tick());
        }
        
        public void RestartRound()
        {
            if (!isServer) return;
            
            StopCoroutine(tickCoroutine);
            NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
        }
        
        private IEnumerator TickWarmup()
        {
            while (timerSeconds > 0)
            {
                RpcUpdateClientClocks(GetTimerText());
                timerSeconds--;
                yield return new WaitForSeconds(1);
            }
            
            StartRound();
        }
        
        private IEnumerator Tick()
        {
            //TODO: change this once we have antags/shuttle/proper round endings
            //Restart round every 300 seconds.
            while (timerSeconds < 300)
            {
                RpcUpdateClientClocks(GetTimerText());
                timerSeconds++;
                yield return new WaitForSeconds(1);
            }
            
            RestartRound();
        }

        [ClientRpc]
        private void RpcUpdateClientClocks(string text)
        {
            if(!timerUi.gameObject.activeSelf) timerUi.gameObject.SetActive(true);
            
            timerText.text = text;
        }

        private string GetTimerText()
        {
            //Convert ticks to seconds
            TimeSpan timeSpan = new TimeSpan(timerSeconds * 10000000);
            string timer =  timeSpan.ToString(@"hh\:mm\:ss");
            return IsRoundStarted ? $"Round Time: {timer}" : $"Round Start In: {timer}";
        }
    }
}
