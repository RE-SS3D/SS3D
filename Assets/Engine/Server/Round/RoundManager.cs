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

        [SyncVar] private bool warmingUp;
        [SerializeField] private int warmupTimeSeconds = 5;
        [SerializeField] private int roundTimeSeconds = 300;
        private Coroutine warmupCoroutine;     
        
        private int timerSeconds = 0;
        [SyncVar] private bool started = false;
        private Coroutine tickCoroutine;

        public static event System.Action ServerWarmupStarted;
        public static event System.Action ServerRoundStarted;
        public static event System.Action ServerRoundRestarted;
        public static event System.Action<int> ClientTimerUpdated;

        public static event System.Action ServerRoundEnded; 

        public bool IsRoundStarted => started;
        public bool IsOnWarmup => warmingUp;

        private void Awake()
        {
            InitializeSingleton();
        }
        
        public void StartWarmup()
        {
            // These activities will happen both on the server and client.
            gameObject.SetActive(true);
            StopAllCoroutines();
            timerSeconds = warmupTimeSeconds;
            ServerWarmupStarted?.Invoke();

            // Only do SyncVar assignments, tick coroutine and the RPC on the server.
            if (isServer)
            {
                started = false;
                warmingUp = true;
                warmupCoroutine = StartCoroutine(TickWarmup());
                RpcStartWarmup();
            }


        }

        [ClientRpc]
        private void RpcStartWarmup()
        {
            // Prevent from running again on server
            if (isServer) return;
            StartWarmup();
        }

        [ContextMenu("Start Round")]
        public void StartRound()
        {
            // These activities will happen both on the server and client.
            gameObject.SetActive(true);

            Debug.Log("Round Started");
            ServerRoundStarted?.Invoke();

            // Only do SyncVar assignments, tick coroutine and the RPC on the server.
            if (isServer)
            {
                started = true;
                warmingUp = false;
                StopCoroutine(warmupCoroutine);
                tickCoroutine = StartCoroutine("Tick");
                RpcStartRound();
            }
        }

        [ClientRpc]
        public void RpcStartRound()
        {
            if (isServer) return;
            StartRound();
        }

        public void EndRound()
        {
            if (!isServer) return;

            // if the round didn't even start we cancel the warmup
            if (warmingUp)
            {
                warmingUp = false;
                StopCoroutine(warmupCoroutine);
            }

            started = false;
            ServerRoundEnded?.Invoke();
            
            StopCoroutine(tickCoroutine);
            SceneLoaderManager.singleton.UnloadSelectedMap();

            RpcEndRound();
        }

        [ClientRpc]
        public void RpcEndRound()
        {
            if (isServer) return;
            // if the round didn't even start we cancel the warmup
            if (warmingUp)
            {
                warmingUp = false;
                StopCoroutine(warmupCoroutine);
            }

            started = false;
            ServerRoundEnded?.Invoke();

            StopCoroutine(tickCoroutine);
            SceneLoaderManager.singleton.UnloadSelectedMap();

            StopCoroutine(tickCoroutine);
        }
        
        // Ignore this for now
        public void RestartRound()
        {
            if (!isServer)
            {
                return;
            }

            if (started) EndRound();
            
            ServerRoundRestarted?.Invoke();
        }

        private IEnumerator TickWarmup()
        {
            while (timerSeconds > 0)
            {
                UpdateClock(GetTimerTextSeconds());
                Debug.Log("Round start timer:" + timerSeconds);
                timerSeconds--;
                yield return new WaitForSeconds(1);
            }

            StartRound();
        }

        private IEnumerator Tick()
        {
            while (started)
            {
                UpdateClock(GetTimerTextSeconds());
                timerSeconds++;
                yield return new WaitForSeconds(1);
            }
            Debug.Log("Coroutine running while round is not started, IsRoundStarted: " + started);
            //RestartRound();
        }

        [Server]
        private void UpdateClock(int time)
        {
            ClientTimerUpdated?.Invoke(time);
            RpcUpdateClientClocks(time);
        }

        [ClientRpc]
        private void RpcUpdateClientClocks(int time)
        {
            ClientTimerUpdated?.Invoke(time);
        }
        
        private int GetTimerTextSeconds()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(timerSeconds);
            int timer = (int)timeSpan.TotalSeconds;
            return timer;
        }

        public void SetWarmupTime(TMP_InputField newTime)
        {
            if (newTime.text == null) return;
            warmupTimeSeconds = Int32.Parse(newTime.text);
        } 
        public void SetWarmupTime(int newTime)
        {
            warmupTimeSeconds = newTime;
        }

        void InitializeSingleton()
        {
            if (singleton != null && singleton != this) { 
                Destroy(gameObject);
            }
            else
            {
                singleton = this;   
            }
        }
    }
}
