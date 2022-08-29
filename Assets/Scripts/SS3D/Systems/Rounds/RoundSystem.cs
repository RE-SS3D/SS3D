using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds.Messages;
using UnityEngine;

namespace SS3D.Systems.Rounds
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

        private ServerManager _serverManager;

        public bool RoundRunning => _roundState == RoundState.Running;
        public bool RoundStarting => _roundState == RoundState.Starting;
        public bool OnWarmup => _roundState == RoundState.WarmingUp;

        public RoundState RoundState => _roundState;
        public int RoundTime => _currentTimerSeconds;

        public override void OnStartServer()
        {
            base.OnStartServer();

            _serverManager = InstanceFinder.ServerManager;
            
            ServerSubscribeToEvents();
        }
        
        [Server]
        private void ServerSubscribeToEvents()
        {
            _serverManager.RegisterBroadcast<RequestStartRoundMessage>(HandleRequestStartRound);
        }

        private void HandleRequestStartRound(NetworkConnection conn, RequestStartRoundMessage m)
        {
            const ServerRoleTypes requiredRole = ServerRoleTypes.Administrator;

            PlayerControlSystem playerControlSystem = GameSystems.Get<PlayerControlSystem>();
            PermissionSystem permissionSystem = GameSystems.Get<PermissionSystem>();

            string userCkey = playerControlSystem.GetSoulCkeyByConn(conn);
            if (permissionSystem.GetUserPermission(userCkey) != requiredRole)
            {
                Debug.Log($"[{nameof(RoundSystem)}] - User {userCkey} doesn't have {requiredRole} permission");
            }
            else
            {
                Debug.Log($"[{nameof(RoundSystem)}] - User {userCkey} has started the round");
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

            _tickCancellationToken?.Cancel();
            _warmupCoroutine = ProcessWarmupTickCoroutine;

            _warmupCoroutine.Invoke();

            WarmupStartedMessage warmupStartedMessage = new();
            _serverManager.Broadcast(warmupStartedMessage);
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

            _warmupCancellationToken?.Cancel();
            _tickCoroutine = ProcessTickCoroutine;

            _tickCoroutine.Invoke();
            
            _serverManager.Broadcast(new RoundStartedMessage());
        }

        [Server]
        private async void ProcessWarmupTickCoroutine()
        {
            if (!IsServer)
            {
                return;
            }

            _warmupCancellationToken = new CancellationTokenSource();
            TimeSpan second = TimeSpan.FromSeconds(1);

            while (_currentTimerSeconds > 0)
            {
                UpdateClock(GetTimerSeconds());
                _currentTimerSeconds--;

                Debug.Log($"[{nameof(RoundSystem)}] - Start timer: {_currentTimerSeconds}");
                await UniTask.Delay(second, true, PlayerLoopTiming.Update, cancellationToken: _warmupCancellationToken.Token);
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

            _tickCancellationToken = new CancellationTokenSource();
            TimeSpan second = TimeSpan.FromSeconds(1);

            while (RoundRunning)
            {
                UpdateClock(GetTimerSeconds());
                _currentTimerSeconds++;
                await UniTask.Delay(second, true, PlayerLoopTiming.Update, _tickCancellationToken.Token);
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
            _serverManager.Broadcast(roundTickUpdatedMessage, false);
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
            _serverManager.Broadcast(roundStateUpdatedMessage, false);
        }
    }
}                               
