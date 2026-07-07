using Coimbra;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Core;
using SS3D.Engine.Chat;
using SS3D.Logging;
using SS3D.Systems.Rounds.Messages;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Round system base implementation for basic round functionality
    /// </summary>
    public sealed class RoundSubSystem : RoundSubSystemBase
    {
        /// <summary>
        /// Round loop runner
        /// </summary>
        [Server]
        protected override async UniTask ProcessChangeRoundState(ChangeRoundStateMessage m, CancellationToken cancellationToken)
        {
            if (!IsServer)
            {
                return;
            }

            if (m.State)
            {
                await StartRoundSequence(cancellationToken);
            }
            else
            {
                await StopRoundSequence(cancellationToken);
            }
        }

        [Server]
        private async UniTask StartRoundSequence(CancellationToken cancellationToken)
        {
            await StopRound(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            await PrepareRound(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            await ProcessRoundTick(cancellationToken);
        }

        [Server]
        private async UniTask StopRoundSequence(CancellationToken cancellationToken)
        {
            if (RoundState == RoundState.Stopped)
            {
                return;
            }

            if (RoundState != RoundState.Ending)
            {
                await ProcessEndRound(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            await StopRound(cancellationToken);
        }

        /// <summary>
        /// Prepares the round before starting
        /// </summary>
        [Server]
        protected override async UniTask PrepareRound(CancellationToken cancellationToken)
        {
            Log.Information(this, "Preparing round", Logs.ServerOnly);

            RoundState = RoundState.Preparing;

            await UniTask.Delay(System.TimeSpan.FromMilliseconds(500), cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Process the round tick until the round ends
        /// </summary>
        [Server]
        protected override async UniTask ProcessRoundTick(CancellationToken cancellationToken)
        {
            Log.Information(this, "Starting {seconds} seconds warmup tick", Logs.ServerOnly, _warmupSeconds);

            RoundSeconds = _warmupSeconds;
            CancelTick();
            TickCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            System.TimeSpan second = System.TimeSpan.FromSeconds(1);

            RoundState = RoundState.WarmingUp;

            while (IsWarmingUp && RoundSeconds > 0)
            {
                await UniTask.Delay(second, cancellationToken: TickCancellationToken.Token);
                RoundSeconds--;
            }

            cancellationToken.ThrowIfCancellationRequested();

            RoundState = RoundState.Ongoing;
            Log.Information(this, "Starting round tick", Logs.ServerOnly);
            ChatSubSystem chatSystem = SubSystems.Get<ChatSubSystem>();
            ChatChannels chatChannels = ScriptableSettings.GetOrFind<ChatChannels>();
            // TODO: use captain character name here
            chatSystem.SendServerMessage(chatChannels.stationAlertsChannel, "Welcome aboard crew, you're under no captain. Enjoy!");

            while (IsOngoing)
            {
                await UniTask.Delay(second, cancellationToken: TickCancellationToken.Token);
                RoundSeconds++;
            }
        }

        [Server]
        protected override async UniTask ProcessEndRound(CancellationToken cancellationToken)
        {
            RoundState = RoundState.Ending;
            CancelTick();

            await UniTask.Delay(System.TimeSpan.FromSeconds(3), cancellationToken: cancellationToken);
        }

        [Server]
        protected override async UniTask StopRound(CancellationToken cancellationToken)
        {
            if (RoundState == RoundState.Stopped)
            {
                return;
            }

            RoundState = RoundState.Stopped;
            RoundSeconds = 0;

            await UniTask.Delay(System.TimeSpan.FromMilliseconds(500), cancellationToken: cancellationToken);
        }

#if UNITY_EDITOR
        /// <summary>
        /// This method facilitates automated testing, and is not to be used in production.
        /// It simulates a ChangeRoundStateMessage broadcast received from a client, and
        /// is handled normally by the server. Method required because the server cannot
        /// broadcast to itself. Note that authentication in RoundSystemBase has been
        /// bypassed by this method.
        /// </summary>
        /// <param name="m">The ChangeRoundStateMessage apparently broadcast</param>
        [Server]
        public void ChangeRoundStateMessageStubBroadcast(ChangeRoundStateMessage m)
        {
            RequestRoundStateChange(m);
        }
#endif
    }
}
