using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Permissions;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Rounds.Messages;
using UnityEngine;
using RoundStateUpdated = SS3D.Systems.Rounds.Events.RoundStateUpdated;
using RoundTickUpdated = SS3D.Systems.Rounds.Events.RoundTickUpdated;

#pragma warning disable CS1998

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Base for the round system, done here to avoid too much code in the round system 
    /// </summary>
    public class RoundSystemBase : NetworkedSystem
    {
        [Header("Round Information")]                                   
        [SyncVar(OnChange = "SetRoundState")] [SerializeField] private RoundState _roundState;
        /// <summary>
        /// How much time has passed
        /// </summary>
        [SyncVar(OnChange = "SetCurrentTimerSeconds")] [SerializeField] private int _currentTimerSeconds;
        /// <summary>
        /// How many seconds of warmup
        /// </summary>
        [Header("Warmup")] 
        [SyncVar] [SerializeField] protected int _warmupSeconds = 5;

        protected CancellationTokenSource TickCancellationToken;
        private ServerManager _serverManager;

        protected RoundState RoundState
        {
            get => _roundState;
            set => _roundState = value;
        }

        protected int RoundSeconds
        {
            get => _currentTimerSeconds;
            set => _currentTimerSeconds = value;
        }

        protected bool IsWarmingUp => RoundState == RoundState.WarmingUp;
        protected bool IsOngoing => RoundState == RoundState.Ongoing;

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
            _serverManager.RegisterBroadcast<ChangeRoundStateMessage>(HandleRequestStartRound);
        }

        [Server]
        private void HandleRequestStartRound(NetworkConnection conn, ChangeRoundStateMessage m)
        {
            AuthorizeChangeRoundState(conn, m);
        }

        /// <summary>
        /// Process the start round request
        /// </summary>
        /// <param name="conn">The connection that requested the round start</param>
        /// <param name="changeRoundStateMessage"></param>
        [Server]
        private void AuthorizeChangeRoundState(NetworkConnection conn, ChangeRoundStateMessage m)
        {
            const ServerRoleTypes requiredRole = ServerRoleTypes.Administrator;             

            PlayerControlSystem playerControlSystem = SystemLocator.Get<PlayerControlSystem>();
            PermissionSystem permissionSystem = SystemLocator.Get<PermissionSystem>();

            // Gets the soul that matches the connection, uses the ckey as the user id
            string userCkey = playerControlSystem.GetCkey(conn);

            // Checks if player can call a round start
            if (permissionSystem.TryGetUserRole(userCkey, out ServerRoleTypes role) && role != requiredRole)
            {
                string message = $"User {userCkey} doesn't have {requiredRole} permission";
                Punpun.Say(this, message, Logs.ServerOnly);
            }
            else
            {
                string message = $"User {userCkey} has started the round";
                Punpun.Say(this, message, Logs.ServerOnly);

                #pragma warning disable CS4014
                ProcessChangeRoundState(m);   
                #pragma warning restore CS4014
            }
        }

        [Server]
        protected virtual async UniTask ProcessChangeRoundState(ChangeRoundStateMessage changeRoundStateMessage)
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

        private void SetCurrentTimerSeconds(int oldValue, int newValue, bool asServer)
        {
            _currentTimerSeconds = newValue;

            //Punpun.Say(this, _currentTimerSeconds.ToString(), Logs.ServerOnly);

            RoundTickUpdated roundTickUpdated = new(_currentTimerSeconds);
            roundTickUpdated.Invoke(this);
        }

        private void SetRoundState(RoundState oldValue, RoundState newValue, bool asServer)
        {
            _roundState = newValue;
            
            Punpun.Say(this, _roundState.ToString(), Logs.ServerOnly);

            RoundStateUpdated roundStateUpdated = new(_roundState);
            roundStateUpdated.Invoke(this);
        }
    }
}                               
