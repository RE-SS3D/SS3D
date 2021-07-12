using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using SS3D.Content;
using SS3D.Engine.Server.Gamemode;
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

        // manager that handles the current round's gamemode
        public GamemodeManager gamemodeManager;
        // WARMUP
        
        // is it starting up?
        private bool warmingUp;
        // how much time we will wait until the round starts
        [SerializeField] private int warmupTimeSeconds = 5;
        // how much time the round will last, hopefully we can disable this
        [SerializeField] private int roundTimeSeconds = 300;
        // the coroutine that handles the warmup time
        private Coroutine warmupCoroutine;
        
        // END ROUND
        
        // how much time takes for the round to end after we called the EndRound function
        // its useful for the  stats stuff and the good 'ol end of the round killin' we all love
        public int endRoundDelay = 5;
        // keeps track of the seconds to end the round
        public int currentEndRoundDelay = 0;
        
        // TIMER
        
        // how much time the round is on for
        private int timerSeconds = 0;
        // has the round started
        private bool started = false;
        // the coroutine that counts how much time has passed since round start
        private Coroutine tickCoroutine;

        // EVENTS
        public static event System.Action ServerWarmupStarted;
        public static event System.Action ServerRoundStarted;
        public static event System.Action ServerRoundRestarted;
        public static event System.Action ServerRoundEnded;
        // we fire this to update client's clocks with the round time
        public static event System.Action<int> ClientTimerUpdated;

        // PLAYER MANAGEMENT

        // players that have joined the round
        public List<Entity> roundPlayers;
        
        // do we want the round to end when there's a nuclear explosion
        public bool endOnNuclearExplosion;
        public bool IsRoundStarted => started;
        public bool IsOnWarmup => warmingUp;

        private void Awake()
        {
            InitializeSingleton();
        }
            
        [Command(ignoreAuthority = true)]
        private void CmdSetPlayerReadyState(bool state, NetworkConnectionToClient sender = null)
        {
            if (state)
            {
                roundPlayers.Add(LoginNetworkManager.singleton.GetSoul(sender).GetComponent<Entity>());
            }
            else
            {
                roundPlayers.Remove(LoginNetworkManager.singleton.GetSoul(sender).GetComponent<Entity>());
            }
        }
        
        // WARMUP - START - END
        public void StartWarmup()
        {
            gameObject.SetActive(true);

            started = false;
            StopAllCoroutines();
            
            timerSeconds = warmupTimeSeconds;

            warmupCoroutine = StartCoroutine(TickWarmup());

            warmingUp = true;
            ServerWarmupStarted?.Invoke();
            RpcStartWarmup();
        }
        
        [ClientRpc]
        private void RpcStartWarmup()
        {
            if (isServer) return;
            gameObject.SetActive(true);

            started = false;
            StopAllCoroutines();
            
            timerSeconds = warmupTimeSeconds;

            warmupCoroutine = StartCoroutine(TickWarmup());

            warmingUp = true;
            ServerWarmupStarted?.Invoke();
        }

        // Asks the server to start the round
        [Command(ignoreAuthority = true)]
        public void CmdStartRound(NetworkConnectionToClient sender = null)
        {
            StartWarmup(); 
        }
        
        [Server]
        [ContextMenu("Start Round")]
        public void StartRound()
        {
            gameObject.SetActive(true);
            started = true;
            warmingUp = false;
            
            StopCoroutine(warmupCoroutine);
            
            tickCoroutine = StartCoroutine("Tick");

            Debug.Log("Round Started");
            ServerRoundStarted?.Invoke();

            // handles setting up the gamemode and objectives
            GamemodeManager.singleton.InitiateGamemode();
            
            RpcStartRound();
        }

        [ClientRpc]
        public void RpcStartRound()
        {
            if (isServer) return;
            
            gameObject.SetActive(true);
            started = true;
            warmingUp = false;
            
            if (warmupCoroutine != null) StopCoroutine(warmupCoroutine); 
            
            tickCoroutine = StartCoroutine("Tick");
            
            Debug.Log("Round Started");
            ServerRoundStarted?.Invoke();
        }

        [Command(ignoreAuthority = true)]
        public void CmdEndRound(NetworkConnectionToClient sender = null)
        {
            EndRound();
        }

        // TODO: Timer to actually end the round
        // TODO: Stats
        [Server]
        public void EndRound()
        {
            // Spawn the stats screen
            currentEndRoundDelay = endRoundDelay;
            // Set up the timer
            StartCoroutine(EndRoundDelay());
        }

        private IEnumerator EndRoundDelay()
        {
            while (currentEndRoundDelay > 0)
            {
                currentEndRoundDelay--;
                yield return new WaitForSeconds(1);
            }

            // Ends the round for real
            EndRoundImmediate();
        }
        
        [Server]
        public void EndRoundImmediate()
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
