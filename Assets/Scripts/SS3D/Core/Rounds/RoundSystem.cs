using System;
using System.Collections;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Rounds.Messages;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.Core.Rounds
{
    /// <summary>
    ///   <para>
    ///     Behaviour responsible for syncing timers between server and clients and starting
    ///     and restarting rounds.
    /// </para>
    /// </summary>
    public class RoundSystem : NetworkBehaviour
    {
        [FormerlySerializedAs("_roundState")]
        [Header("Round Stats")] 
        [SyncVar(OnChange = "SetRoundState")] 
        [SerializeField] private RoundState roundState;
        
        // How much time has passed
        [SyncVar(OnChange = "SetCurrentTimerSeconds")] 
        [SerializeField] private int _currentTimerSeconds;
        
        // How many seconds until the round ends
        [SerializeField] private int _roundTotalSeconds = 300;

        [Header("Warmup")]
        // How many seconds of warmup
        [SyncVar(OnChange = "SetWarmupTimer")] 
        [SerializeField] private int _warmupTimerSeconds = 5;

        private Coroutine _warmupCoroutine;
        private Coroutine _tickCoroutine;

        public bool RoundRunning => roundState == RoundState.Running;
        public bool RoundStarting => roundState == RoundState.Starting;
        public bool OnWarmup => roundState == RoundState.WarmingUp;

        private void Start()
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
            UpdateRoundState(RoundState.Starting);
            // These activities will happen both on the server and client.

            // Only do SyncVar assignments, tick coroutine and the RPC on the server.
            if (!IsServer)
            {
                return;
            }

            UpdateRoundState(RoundState.Running);
            StopCoroutine(_warmupCoroutine);
            _tickCoroutine = StartCoroutine(Tick());

            Debug.Log($"[{nameof(RoundSystem)}] - Round Started");
            
            InstanceFinder.ServerManager.Broadcast(new RoundStartedMessage());
        }

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

        private IEnumerator Tick()
        {
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
            RoundTickUpdatedMessage roundTickUpdatedMessage = new RoundTickUpdatedMessage(time);

            InstanceFinder.ServerManager.Broadcast(roundTickUpdatedMessage);
        }

        [ObserversRpc]
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

        private void UpdateRoundState(RoundState newState)
        {
            roundState = newState;
            
            RoundStateUpdatedMessage roundStateUpdatedMessage = new RoundStateUpdatedMessage(newState);
            InstanceFinder.ServerManager.Broadcast(roundStateUpdatedMessage);
        }
        
        /// <summary>
        /// Used by Mirror to sync the round state
        /// </summary>
        private void SetRoundState(RoundState oldState, RoundState newState, bool AsServer)
        {
            roundState = newState;

            Debug.Log($"[{nameof(RoundSystem)}] - Round state updated: [{newState}]");
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
