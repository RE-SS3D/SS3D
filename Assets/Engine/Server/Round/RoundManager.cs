using System;
using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SS3D.Engine.Server.Round
{
    /// <summary>
    ///   <para>Behaviour responsible for syncing timers between server and clients and starting
    ///   and restarting rounds.</para>
    ///   <para>Should be attached to the RoundManager prefab.</para>
    /// </summary>
    public class RoundManager : NetworkBehaviour
    {
        public static RoundManager singleton { get; private set; }

        [SerializeField] private int warmupTimeSeconds = 5;
        [SerializeField] private int roundTimeSeconds = 300;

        private int timerSeconds = 0;
        private bool started = false;
        private Coroutine tickCoroutine;

        public static event System.Action ServerWarmupStarted;
        public static event System.Action ServerRoundStarted;
        public static event System.Action ServerRoundRestarted;
        public static event System.Action<string> ClientTimerUpdated;

        public bool IsRoundStarted => started;

        private void Start()
        {
            InitializeSingleton();

        }

        public void StartWarmup()
        {
            gameObject.SetActive(true);
            timerSeconds = warmupTimeSeconds;
            StartCoroutine(TickWarmup());

            ServerWarmupStarted?.Invoke();
        }

        [ContextMenu("Start Round")]
        [Server]
        public void StartRound()
        {
            gameObject.SetActive(true);
            started = true;
            tickCoroutine = StartCoroutine(Tick());

            Debug.Log("Round Started");
            ServerRoundStarted?.Invoke();
            RpcStartRound();
        }

        [ClientRpc]
        public void RpcStartRound()
        {
            gameObject.SetActive(true);
            started = true;
            tickCoroutine = StartCoroutine(Tick());

            Debug.Log("Round Started");
            ServerRoundStarted?.Invoke();
        }

        public void RestartRound()
        {
            if (!isServer)
            {
                return;
            }

            StopCoroutine(tickCoroutine);
            NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);

            ServerRoundRestarted?.Invoke();
        }

        private IEnumerator TickWarmup()
        {
            while (timerSeconds > 0)
            {
                UpdateClock(GetTimerText());
                Debug.Log("Round start timer:" + timerSeconds);
                timerSeconds--;
                yield return new WaitForSeconds(1);
            }

            StartRound();
        }

        private IEnumerator Tick()
        {
            while (timerSeconds < roundTimeSeconds)
            {
                UpdateClock(GetTimerText());
                timerSeconds++;
                yield return new WaitForSeconds(1);
            }
            
            Debug.Log("Restarting Round");
            RestartRound();
        }

        private void UpdateClock(string text)
        {
            ClientTimerUpdated?.Invoke(text);
            RpcUpdateClientClocks(text);
        }

        [ClientRpc]
        private void RpcUpdateClientClocks(string text)
        {
            ClientTimerUpdated?.Invoke(text);
        }
        
        private string GetTimerText()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timerSeconds);
            string timer = timeSpan.TotalSeconds.ToString();
            return timer;
        }

        public void SetWarmupTime(TMP_InputField newTime)
        {
            warmupTimeSeconds = Int32.Parse(newTime.text);
        } 
        public void SetWarmupTime(int newTime)
        {
            warmupTimeSeconds = newTime;
        }

        void InitializeSingleton()
        {
            if (singleton != null) Destroy(gameObject);
            singleton = this;
        }
    }
}
