using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Logging;
using SS3D.Systems.Rounds.Messages;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Round system base implementation for basic round functionality
    /// </summary>
    public sealed class RoundSystem : RoundSystemBase
    {
        /// <summary>
        /// Round loop runner
        /// </summary>
        /// <param name="changeRoundStateMessage"></param>
        [Server]
        protected override async UniTask ProcessChangeRoundState(ChangeRoundStateMessage m)
        {
            if (!IsServer) { return; }

            if (m.State)
            {
                await StopRound();
                await PrepareRound();
                await ProcessRoundTick();
                await ProcessEndRound();
                await StopRound();
            }
            else
            {
                await ProcessEndRound();
                await StopRound();
            }
        }

        /// <summary>
        /// Prepares the round before starting
        /// </summary>
        [Server]
        protected override async UniTask PrepareRound()
        {
            Punpun.Say(this, "Preparing round", Logs.ServerOnly);
            
            RoundState = RoundState.Preparing;

            TimeSpan second = TimeSpan.FromMilliseconds(500);
            await UniTask.Delay(second);
        }

        /// <summary>
        /// Process the round tick until the round ends
        /// </summary>
        [Server]
        protected override async UniTask ProcessRoundTick()
        {
            Punpun.Say(this, $"Starting {_warmupSeconds} seconds warmup tick", Logs.ServerOnly);

            RoundSeconds = _warmupSeconds;
            TickCancellationToken = new CancellationTokenSource();
            TimeSpan second = TimeSpan.FromSeconds(1);

            RoundState = RoundState.WarmingUp;

            while (IsWarmingUp && RoundSeconds > 0)
            {
                await UniTask.Delay(second, cancellationToken: TickCancellationToken.Token);
                RoundSeconds--;
            }

            RoundState = RoundState.Ongoing;
            Punpun.Say(this, "Starting round tick", Logs.ServerOnly);

            while (IsOngoing)
            {
                await UniTask.Delay(second, cancellationToken: TickCancellationToken.Token);

                RoundSeconds++;
            }
        }

        [Server]
        protected override async UniTask ProcessEndRound()
        {
            RoundState = RoundState.Ending;
            TickCancellationToken?.Cancel();

            TimeSpan second = TimeSpan.FromSeconds(3);
            await UniTask.Delay(second);
        }

        [Server]
        protected override async UniTask StopRound()
        {
            if (RoundState == RoundState.Stopped)
            {
                return;
            }

            RoundState = RoundState.Stopped;
            RoundSeconds = 0;

            TimeSpan second = TimeSpan.FromMilliseconds(500);
            await UniTask.Delay(second);
        }
    }
}