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
#pragma warning disable CS1998

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Base for the round system, done here to avoid too much code in the round system 
    /// </summary>
    public class RoundSystemBase : NetworkedSpessBehaviour
    {
        [Header("Round Information")]                                   
        [SyncVar] [SerializeField] private RoundState _roundState;
        /// <summary>
        /// How much time has passed
        /// </summary>
        [SyncVar] [SerializeField] private int _currentTimerSeconds;
        /// <summary>
        /// How many seconds of warmup
        /// </summary>
        [Header("Warmup")] 
        [SyncVar] [SerializeField] protected int _warmupSeconds = 5;

        protected CancellationTokenSource TickCancellationToken;
        private ServerManager _serverManager;

        public RoundState RoundState
        {
            get => _roundState;
            protected set
            {
                _roundState = value;
                UpdateRoundState();
            }
        }

        public int RoundSeconds
        {
            get => _currentTimerSeconds;
            protected set
            {
                _currentTimerSeconds = value; 
                UpdateClock();
            }
        }

        public bool IsWarmingUp => RoundState == RoundState.WarmingUp;
        public bool IsOngoing => RoundState == RoundState.Ongoing;

        public override void OnStartServer()
        {
            base.OnStartServer();

            _serverManager = InstanceFinder.ServerManager;
            ServerSubscribeToEvents();
        }
        
        /// <summary>
        /// Runs on the server to listen to events
        /// </summary>
        [Server]
        private void ServerSubscribeToEvents()
        {
            _serverManager.RegisterBroadcast<RequestStartRoundMessage>(HandleRequestStartRound);
        }

        [Server]
        private void HandleRequestStartRound(NetworkConnection conn, RequestStartRoundMessage _)
        {
            RequestStartRound(conn);
        }

        /// <summary>
        /// Process the start round request
        /// </summary>
        /// <param name="conn">The connection that requested the round start</param>
        [Server]
        private void RequestStartRound(NetworkConnection conn)
        {
            const ServerRoleTypes requiredRole = ServerRoleTypes.Administrator;

            PlayerControlSystem playerControlSystem = GameSystems.Get<PlayerControlSystem>();
            PermissionSystem permissionSystem = GameSystems.Get<PermissionSystem>();

            // Gets the soul that matches the connection, uses the ckey as the user id
            string userCkey = playerControlSystem.GetSoulCkeyByConn(conn);

            // Checks if player can call a round start
            if (permissionSystem.GetUserPermission(userCkey) != requiredRole)
            {
                Debug.Log($"[{nameof(RoundSystemBase)}] - User {userCkey} doesn't have {requiredRole} permission");
            }
            else
            {
                Debug.Log($"[{nameof(RoundSystemBase)}] - User {userCkey} has started the round");
                #pragma warning disable CS4014
                ProcessStartRound();   
                #pragma warning restore CS4014
            }
        }

        /// <summary>
        /// Server method to start the warmup
        /// </summary>
        [Server]
        protected virtual async UniTask ProcessStartRound()
        {
            throw new NotImplementedException("Method is not implemented, please do, you moron 😘");
        }

        [Server]
        protected virtual async UniTask ProcessEndRound()
        {
            throw new NotImplementedException("Method is not implemented, please do, you moron 😘");
        }

        [Server]
        protected virtual async UniTask ProcessRoundTick()
        {
            throw new NotImplementedException("Method is not implemented, please do, you moron 😘");
        }

        [Server]
        protected virtual async UniTask PrepareRound()
        {
            throw new NotImplementedException("Method is not implemented, please do, you moron 😘");
        }

        [Server]
        protected virtual async UniTask StopRound()
        {
            throw new NotImplementedException("Method is not implemented, please do, you moron 😘");
        }

        [Server]
        private void UpdateClock()
        {
            if (!IsServer)
            {
                return;
            }
            
            RoundTickUpdatedMessage roundTickUpdatedMessage = new(_currentTimerSeconds);
            _serverManager.Broadcast(roundTickUpdatedMessage, false);
        }

        [Server]
        protected void UpdateRoundState()
        {
            RoundStateUpdatedMessage roundStateUpdatedMessage = new(_roundState);
            _serverManager.Broadcast(roundStateUpdatedMessage, false);
        }

        protected int GetTimerSeconds()
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(_currentTimerSeconds);
            int timer = (int)timeSpan.TotalSeconds;
            
            return timer;
        }
    }
}                               
