using System;
using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SS3D.Engine.Server.Round
{
    /// <summary>
    ///   <para>Behaviour responsible for syncing timers between server and clients and starting
    ///   and restarting rounds.</para>
    ///   <para>Should be attached to the RoundManager prefab.</para>
    /// </summary>
    public class RoundManager : NetworkBehaviour
    {
        [SerializeField] private int warmupTimeSeconds = 5;
        [SerializeField] private int roundTimeSeconds = 300;
        [SerializeField] private TextMeshProUGUI timerText = null;
        [SerializeField] private RectTransform timerUi = null;
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
            if (!isServer)
            {
                return;
            }
            
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
            while (timerSeconds < roundTimeSeconds)
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
            if (!timerUi.gameObject.activeSelf)
            {
                timerUi.gameObject.SetActive(true);
            }

            timerText.text = text;
        }

        private string GetTimerText()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timerSeconds);
            string timer =  timeSpan.ToString(@"hh\:mm\:ss");
            return IsRoundStarted ? $"Round Time: {timer}" : $"Round Start In: {timer}";
        }
    }
}
