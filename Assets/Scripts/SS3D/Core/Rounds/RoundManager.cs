using System;
using System.Collections;
using Mirror;
using UnityEngine;

namespace SS3D.Core.Rounds
{
    /// <summary>
    ///   <para>
    ///     Behaviour responsible for syncing timers between server and clients and starting
    ///     and restarting rounds.
    /// </para>
    /// </summary>
    public class RoundManager : NetworkBehaviour
    {
        public static event Action WarmupStarted;
        public static event Action RoundStarted;
        public static event Action<int> OnTick;

        [Header("Round Stats")] 
        [SyncVar(hook = "SetRoundState")] 
        [SerializeField] private RoundStates _roundState;
        // How much time has passed
        [SyncVar(hook = "SetCurrentTimerSeconds")] 
        [SerializeField] private int _currentTimerSeconds = 0;
        // How many seconds until the round ends
        [SerializeField] private int _roundTotalSeconds = 300;

        [Header("Warmup")]
        // How many seconds of warmup
        [SyncVar(hook = "SetWarmupTimer")] 
        [SerializeField] private int _warmupTimerSeconds = 5;

        private Coroutine _warmupCoroutine;
        private Coroutine _tickCoroutine;

        public bool RoundRunning => _roundState == RoundStates.Running;
        public bool RoundStarting => _roundState == RoundStates.Starting;
        public bool OnWarmup => _roundState == RoundStates.WarmingUp;

        /// <summary>
        /// Server method to start the warmup
        /// </summary>
        [Server]
        public void ServerStartWarmup()
        {
            if (!isServer)
            {
                return;
            }

            // Starts the warmup
            _currentTimerSeconds = _warmupTimerSeconds;
            _roundState = RoundStates.WarmingUp;
            _warmupCoroutine = StartCoroutine(TickWarmup());

            WarmupStarted?.Invoke();
        }

        [ClientRpc] 
        public void RpcHandleStartWarmup()
        {
            if (isServer)
            {
                return;
            }
            WarmupStarted?.Invoke();
        }

        [Server]
        public void HandleStartRound()
        {
            _roundState = RoundStates.Starting;
            // These activities will happen both on the server and client.

            // Only do SyncVar assignments, tick coroutine and the RPC on the server.
            if (!isServer)
            {
                return;
            }

            _roundState = RoundStates.Running;
            StopCoroutine(_warmupCoroutine);
            _tickCoroutine = StartCoroutine(Tick());

            Debug.Log("Round Started");
            RoundStarted?.Invoke();
        }

        private IEnumerator TickWarmup()
        {
            while (_currentTimerSeconds > 0)
            {
                UpdateClock(GetTimerSeconds());
                Debug.Log("Round start timer:" + _currentTimerSeconds);
                _currentTimerSeconds--;
                yield return new WaitForSeconds(1);
            }

            HandleStartRound();
        }

        private IEnumerator Tick()
        {
            while (RoundRunning)
            {
                UpdateClock(GetTimerSeconds());
                _currentTimerSeconds++;
                yield return new WaitForSeconds(1);
            }

            Debug.Log($"[{typeof(RoundManager)}] - Coroutine running while round is not active");
        }

        [Server]
        private void UpdateClock(int time)
        {
            OnTick?.Invoke(time);
            RpcUpdateClientClocks(time);
        }

        [ClientRpc]
        private void RpcUpdateClientClocks(int time)
        {
            OnTick?.Invoke(time);
        }

        private int GetTimerSeconds()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(_currentTimerSeconds);
            int timer = (int)timeSpan.TotalSeconds;
            return timer;
        }

        /// <summary>
        /// Used by Mirror to sync the round state
        /// </summary>
        private void SetRoundState(RoundStates oldState, RoundStates newState)
        {
            _roundState = newState;
            Debug.Log($"[{typeof(RoundManager)}] - Round state updated: [{newState}]");
        }

        /// <summary>
        /// Used by Mirror to sync the round timer
        /// </summary>
        private void SetCurrentTimerSeconds(int oldSeconds, int newSeconds)
        {
            _currentTimerSeconds = newSeconds;
            Debug.Log($"[{typeof(RoundManager)}] - Round timer updated: [{newSeconds}]");
        }

        /// <summary>
        /// Used by Mirror to sync the warmuo timer
        /// </summary>
        /// <param name="newTime"></param>
        public void SetWarmupTimer(int oldTime, int newTime)
        {
            _warmupTimerSeconds = newTime;
        }
    }
}                               
