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

        private int timerSeconds = 0;
        private bool started = false;
        private Coroutine tickCoroutine;

        public event System.Action Server_OnWarmupStarted;
        public event System.Action Server_OnRoundStarted;
        public event System.Action Server_OnRoundRestarted;
        public event System.Action<string> Client_OnTimmerUpdated;

        public bool IsRoundStarted => started;

        public void StartWarmup()
        {
            gameObject.SetActive(true);
            timerSeconds = warmupTimeSeconds;
            StartCoroutine(TickWarmup());

            Server_OnWarmupStarted?.Invoke();
        }

        public void StartRound()
        {
            gameObject.SetActive(true);
            started = true;
            tickCoroutine = StartCoroutine(Tick());

            Server_OnRoundStarted?.Invoke();
        }
        
        public void RestartRound()
        {
            if (!isServer)
            {
                return;
            }
            
            StopCoroutine(tickCoroutine);
            NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);

            Server_OnRoundRestarted?.Invoke();
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
            Client_OnTimmerUpdated?.Invoke(text);
        }

        private string GetTimerText()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timerSeconds);
            string timer =  timeSpan.ToString(@"hh\:mm\:ss");
            return IsRoundStarted ? $"Round Time: {timer}" : $"Round Start In: {timer}";
        }

        public void SetWarmupTime(int newTime)
        {
            warmupTimeSeconds = newTime;
        }
    }
}
