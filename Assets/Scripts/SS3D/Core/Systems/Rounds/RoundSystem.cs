using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core.Networking.PlayerControl;
using SS3D.Core.Rounds.Messages;
using SS3D.Core.Systems.Permissions;
using SS3D.Core.Systems.Rounds.Messages;
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
        [SyncVar] 
        [SerializeField] private RoundState _roundState;
        
        // How much time has passed
        [SyncVar] 
        [SerializeField] private int _currentTimerSeconds;

        // How many seconds of warmup
        [Header("Warmup")]
        [SyncVar] 
        [SerializeField] private int _warmupTimerSeconds = 5;

        private Action _warmupCoroutine;
        private Action _tickCoroutine;

        private CancellationTokenSource _warmupCancellationToken;
        private CancellationTokenSource _tickCancellationToken;

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
            const ServerRoleTypes requiredRole = ServerRoleTypes.Administrator;

            PlayerControlSystem playerControlSystem = GameSystems.PlayerControlSystem;
            PermissionSystem permissionSystem = GameSystems.PermissionSystem;

            string userCkey = playerControlSystem.GetSoulCkeyByConn(conn);
            if (permissionSystem.GetUserPermission(userCkey) != requiredRole)
            {
                Debug.Log($"[{nameof(RoundSystem)}] - User {userCkey} doesn't have {requiredRole} permission");
            }
            else
            {
                ServerStartWarmup();   
            }
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

            _tickCancellationToken.Cancel();
            _warmupCoroutine = ProcessWarmupTickCoroutine;

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

            _warmupCancellationToken.Cancel();
            _tickCoroutine = ProcessTickCoroutine;
            
            InstanceFinder.ServerManager.Broadcast(new RoundStartedMessage());
        }

        [Server]
        private async void ProcessWarmupTickCoroutine()
        {
            if (!IsServer)
            {
                return;
            }

            TimeSpan second = TimeSpan.FromSeconds(1);

            while (_currentTimerSeconds > 0)
            {
                UpdateClock(GetTimerSeconds());
                _currentTimerSeconds--;

                Debug.Log($"[{nameof(RoundSystem)}] - Start timer: {_currentTimerSeconds}");
                await UniTask.Delay(second, true, PlayerLoopTiming.FixedUpdate, cancellationToken: _tickCancellationToken.Token);
            }

            HandleStartRound();
        }

        [Server]
        private async void ProcessTickCoroutine()
        {
            if (!IsServer)
            {
                return;
            }

            TimeSpan second = TimeSpan.FromSeconds(1);

            while (RoundRunning)
            {
                UpdateClock(GetTimerSeconds());
                _currentTimerSeconds++;
                await UniTask.Delay(second, true, PlayerLoopTiming.FixedUpdate, _warmupCancellationToken.Token);
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
    }
}                               
