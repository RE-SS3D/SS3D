using System;
using System.Collections;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Rounds.Messages;
using UnityEngine;

namespace SS3D.Core.Systems.Rounds
{
    /// <summary>
    ///   <para>
    ///     Behaviour responsible for syncing timers between server and clients and starting
    ///     and restarting rounds.
    /// </para>
    /// </summary>
    public class RoundSystem : NetworkBehaviour
    {
        [Header("Round Stats")] 
        [SyncVar(OnChange = "SetRoundState")] 
        [SerializeField] private RoundState _roundState;
        
        // How much time has passed
        [SyncVar(OnChange = "SetCurrentTimerSeconds")] 
        [SerializeField] private int _currentTimerSeconds;

        // How many seconds of warmup
        [Header("Warmup")]
        [SyncVar(OnChange = "SetWarmupTimer")] 
        [SerializeField] private int _warmupTimerSeconds = 5;

        private Coroutine _warmupCoroutine;
        private Coroutine _tickCoroutine;

        public bool RoundRunning => _roundState == RoundState.Running;
        public bool RoundStarting => _roundState == RoundState.Starting;
        public bool OnWarmup => _roundState == RoundState.WarmingUp;

        public RoundState RoundState => _roundState;
        public int RoundTime => _currentTimerSeconds;

        public override void OnStartServer()
        {
            base.OnStartServer();
            
            ServerSubscribeToEvents();
        }
        
        [Server]
        private void ServerSubscribeToEvents()
        {
            InstanceFinder.ServerManager.RegisterBroadcast<RequestStartRoundMessage>(HandleRequestStartRound);
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
            if (!IsServer)
            {
                return;
            }

            // Starts the warmup
            _currentTimerSeconds = _warmupTimerSeconds;
            UpdateRoundState(RoundState.WarmingUp);
            
            _warmupCoroutine = StartCoroutine(TickWarmup());

            WarmupStartedMessage warmupStartedMessage = new();
            InstanceFinder.ServerManager.Broadcast(warmupStartedMessage);
        }

        [Server]
        private void HandleStartRound()
        {
            // Only do SyncVar assignments, tick coroutine and the RPC on the server.
            if (!IsServer)
            {
                return;
            }

            if (RoundRunning)
            {
                Debug.Log($"[{nameof(RoundSystem)}] - Can't start round as round is already running");
                return;
            }
            
            UpdateRoundState(RoundState.Starting);
            
            UpdateRoundState(RoundState.Running);
            StopCoroutine(_warmupCoroutine);
            _tickCoroutine = StartCoroutine(Tick());
            
            InstanceFinder.ServerManager.Broadcast(new RoundStartedMessage());
        }

        [Server]
        private IEnumerator TickWarmup()
        {
            while (_currentTimerSeconds > 0)
            {
                UpdateClock(GetTimerSeconds());
                Debug.Log($"[{nameof(RoundSystem)}] - Start timer: {_currentTimerSeconds}");
                _currentTimerSeconds--;
                yield return new WaitForSeconds(1);
            }

            HandleStartRound();
        }

        [Server]
        private IEnumerator Tick()
        {
            if (!IsServer)
            {
                yield break;
            }
            
            while (RoundRunning)
            {
                UpdateClock(GetTimerSeconds());
                _currentTimerSeconds++;
                yield return new WaitForSeconds(1);
            }

            Debug.Log($"[{nameof(RoundSystem)}] - Coroutine running while round is not active");
        }

        [Server]
        private void UpdateClock(int time)
        {
            if (!IsServer)
            {
                return;
            }
            
            RoundTickUpdatedMessage roundTickUpdatedMessage = new(time);
            InstanceFinder.ServerManager.Broadcast(roundTickUpdatedMessage);
        }

        private int GetTimerSeconds()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(_currentTimerSeconds);
            int timer = (int)timeSpan.TotalSeconds;
            
            return timer;
        }

        [Server]
        private void UpdateRoundState(RoundState newState)
        {
            _roundState = newState;
            
            Debug.Log($"[{nameof(RoundSystem)}] - Round state updated: [{newState}]");
            
            RoundStateUpdatedMessage roundStateUpdatedMessage = new(newState);
            InstanceFinder.ServerManager.Broadcast(roundStateUpdatedMessage);
        }
        
        /// <summary>
        /// Used by Mirror to sync the round state
        /// </summary>
        private void SetRoundState(RoundState oldState, RoundState newState, bool AsServer)
        {
            _roundState = newState;
        }

        /// <summary>
        /// Used by Mirror to sync the round timer
        /// </summary>
        private void SetCurrentTimerSeconds(int oldSeconds, int newSeconds, bool AsServer)
        {
            _currentTimerSeconds = newSeconds;
            Debug.Log($"[{nameof(RoundSystem)}] - Round timer updated: [{newSeconds}]");
        }

        /// <summary>
        /// Used by Mirror to sync the warmup timer
        /// </summary>
        /// <param name="newTime"></param>
        public void SetWarmupTimer(int oldTime, int newTime, bool AsServer)
        {
            _warmupTimerSeconds = newTime;
        }
    }
}                               
