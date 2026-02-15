using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Permissions;
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
    public class RoundSubSystemBase : NetworkSubSystem
    {
        /// <summary>
        /// The current round state.
        /// </summary>
        [Header("Round Information")]
        [SyncVar(OnChange = nameof(SyncRoundState))] [SerializeField] private RoundState _roundState;

        /// <summary>
        /// How much time has passed.
        /// </summary>
        [SyncVar(OnChange = nameof(SyncCurrentTimerSeconds))] [SerializeField] private int _currentTimerSeconds;

        /// <summary>
        /// How many seconds of warmup.
        /// </summary>
        [Header("Warmup")]
        [SyncVar] [SerializeField]
        protected int _warmupSeconds = 5;

        /// <summary>
        /// The cancellation token for the round system, it cancels the tick count.
        /// </summary>
        protected CancellationTokenSource TickCancellationToken;

        /// <summary>
        /// The current round state.
        /// </summary>
        protected RoundState RoundState
        {
            get => _roundState;
            set => _roundState = value;
        }

        /// <summary>
        /// The current round elapsed seconds.
        /// </summary>
        protected int RoundSeconds
        {
            get => _currentTimerSeconds;
            set => _currentTimerSeconds = value;
        }

        /// <summary>
        /// Shortcut to see if the round is warming up.
        /// </summary>
        protected bool IsWarmingUp => RoundState == RoundState.WarmingUp;

        /// <summary>
        /// Shortcut to see if the round is ongoing.
        /// </summary>
        protected bool IsOngoing => RoundState == RoundState.Ongoing;

        public override void OnStartServer()
        {
            base.OnStartServer();

            ServerSubscribeToEvents();
        }

        /// <summary>
        /// Runs on the server to listen to events
        /// </summary>
        [Server]
        private void ServerSubscribeToEvents()
        {
            ServerManager.RegisterBroadcast<ChangeRoundStateMessage>(HandleRequestStartRound);
        }

        [Server]
        private void HandleRequestStartRound(NetworkConnection conn, ChangeRoundStateMessage m)
        {
            AuthorizeChangeRoundState(conn, m);
        }

        /// <summary>
        /// Process the start round request.
        /// </summary>
        /// <param name="conn">The connection that requested the round start.</param>
        /// <param name="m"></param>
        [Server]
        private void AuthorizeChangeRoundState(NetworkConnection conn, ChangeRoundStateMessage m)
        {
            const ServerRoleTypes requiredRole = ServerRoleTypes.Administrator;

            PlayerSubSystem playerSystem = SubSystems.Get<PlayerSubSystem>();
            PermissionSubSystem permissionSystem = SubSystems.Get<PermissionSubSystem>();

            // Gets the player that matches the connection, uses the ckey as the user id
            string userCkey = playerSystem.GetCkey(conn);

            // Checks if player can call a round start
            if (!permissionSystem.IsAtLeast(userCkey, requiredRole))
            {
                Log.Information(this, "User {ckey} doesn't have {requiredRole} permission", Logs.ServerOnly, userCkey, requiredRole);
            }
            else
            {
                Log.Information(this, "User {ckey} has started the round", Logs.ServerOnly, userCkey);

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

        /// <summary>
        /// Called by fishnet to update the timer.
        /// </summary>
        private void SyncCurrentTimerSeconds(int oldValue, int newValue, bool asServer)
        {
            if (!asServer && IsHost)
            {
                return;
            }

            RoundTickUpdated roundTickUpdated = new(newValue);
            roundTickUpdated.Invoke(this);
        }

        /// <summary>
        /// Called by fishnet to update the round state.
        /// </summary>
        private void SyncRoundState(RoundState oldValue, RoundState newValue, bool asServer)
        {
            if (!asServer && IsHost)
            {
                return;
            }

            Log.Information(this, _roundState.ToString(), Logs.ServerOnly);

            RoundStateUpdated roundStateUpdated = new(newValue);
            roundStateUpdated.Invoke(this);
        }
    }
}
