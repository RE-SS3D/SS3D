using System;
using System.Collections;
using Mirror;
using SS3D.Core.Rounds.Messages;
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
        [Header("Round Stats")] 
        [SyncVar(hook = "SetRoundState")] 
        [SerializeField] private RoundStates _roundState;
        
        // How much time has passed
        [SyncVar(hook = "SetCurrentTimerSeconds")] 
        [SerializeField] private int _currentTimerSeconds;
        
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

        private void Start()
        {
            NetworkServer.RegisterHandler<RequestStartRoundMessage>(HandleRequestStartRound);
        }

        private void HandleRequestStartRound(NetworkConnection conn, RequestStartRoundMessage m)
        {
            ServerStartWarmup();
        }

        /// <summary>
        /// Server method to start the warmup
        /// </summary>
        [Server]
        private void ServerStartWarmup()
        {
            if (!isServer)
            {
                return;
            }

            // Starts the warmup
            _currentTimerSeconds = _warmupTimerSeconds;
            UpdateRoundState(RoundStates.WarmingUp);
            _warmupCoroutine = StartCoroutine(TickWarmup());

            WarmupStartedMessage warmupStartedMessage = new();
            NetworkServer.SendToAll(warmupStartedMessage);
        }

        [Server]
        private void HandleStartRound()
        {
            UpdateRoundState(RoundStates.Starting);
            // These activities will happen both on the server and client.

            // Only do SyncVar assignments, tick coroutine and the RPC on the server.
            if (!isServer)
            {
                return;
            }

            UpdateRoundState(RoundStates.Running);
            StopCoroutine(_warmupCoroutine);
            _tickCoroutine = StartCoroutine(Tick());

            Debug.Log($"[{nameof(RoundManager)}] - Round Started");
            NetworkServer.SendToAll(new RoundStartedMessage());
        }

        private IEnumerator TickWarmup()
        {
            while (_currentTimerSeconds > 0)
            {
                UpdateClock(GetTimerSeconds());
                Debug.Log($"[{nameof(RoundManager)}] - Start timer: {_currentTimerSeconds}");
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

            Debug.Log($"[{nameof(RoundManager)}] - Coroutine running while round is not active");
        }

        [Server]
        private void UpdateClock(int time)
        {
            OnTickMessage onTickMessage = new OnTickMessage(time);

            NetworkServer.SendToAll(onTickMessage);
        }

        [ClientRpc]
        private void RpcUpdateClientClocks(int time)
        {
            //OnTick?.Invoke(time);
        }

        private int GetTimerSeconds()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(_currentTimerSeconds);
            int timer = (int)timeSpan.TotalSeconds;
            return timer;
        }

        private void UpdateRoundState(RoundStates newState)
        {
            _roundState = newState;
            
            RoundStateUpdatedMessage roundStateUpdatedMessage = new RoundStateUpdatedMessage(newState);
            NetworkServer.SendToAll(roundStateUpdatedMessage);
        }
        
        /// <summary>
        /// Used by Mirror to sync the round state
        /// </summary>
        private void SetRoundState(RoundStates oldState, RoundStates newState)
        {
            _roundState = newState;

            Debug.Log($"[{nameof(RoundManager)}] - Round state updated: [{newState}]");
        }

        /// <summary>
        /// Used by Mirror to sync the round timer
        /// </summary>
        private void SetCurrentTimerSeconds(int oldSeconds, int newSeconds)
        {
            _currentTimerSeconds = newSeconds;
            Debug.Log($"[{nameof(RoundManager)}] - Round timer updated: [{newSeconds}]");
        }

        /// <summary>
        /// Used by Mirror to sync the warmup timer
        /// </summary>
        /// <param name="newTime"></param>
        public void SetWarmupTimer(int oldTime, int newTime)
        {
            _warmupTimerSeconds = newTime;
        }
    }
}                               
