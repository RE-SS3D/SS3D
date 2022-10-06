using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Logging;
using LogType = SS3D.Logging.LogType;

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
        [Server]
        protected override async UniTask ProcessStartRound()
        {
            if (!IsServer) { return; }

            await StopRound();
            await PrepareRound();
            await ProcessRoundTick();
            await ProcessEndRound();
            await StopRound();
        }

        /// <summary>
        /// Prepares the round before starting
        /// </summary>
        [Server]
        protected override async UniTask PrepareRound()
        {
            Punpun.Say(this, "Preparing round", LogType.ServerOnly);
            
            RoundState = RoundState.Preparing;

            await UniTask.WaitUntil(() => RoundState == RoundState.Preparing);
        }

        /// <summary>
        /// Process the round tick until the round ends
        /// </summary>
        [Server]
        protected override async UniTask ProcessRoundTick()
        {
            Punpun.Say(this, "Starting warmup tick", LogType.ServerOnly);

            RoundSeconds = _warmupSeconds;
            TickCancellationToken = new CancellationTokenSource();
            TimeSpan second = TimeSpan.FromSeconds(1);

            RoundState = RoundState.WarmingUp;

            while (IsWarmingUp && RoundSeconds > 0)
            {
                await UniTask.Delay(second);

                RoundSeconds--;

                if (RoundSeconds == 0)
                {
                    RoundState = RoundState.Ongoing;
                }
            }

            Punpun.Say(this, "Starting round tick", LogType.ServerOnly);

            while (IsOngoing)
            {
                await UniTask.Delay(second);

                RoundSeconds++;
            }
        }

        [Server]
        protected override async UniTask ProcessEndRound()
        {
            RoundState = RoundState.Ending;
            await UniTask.WaitUntil(() => RoundState == RoundState.Ending);

            Punpun.Say(this, "Ending round", LogType.ServerOnly);
        }

        [Server]
        protected override async UniTask StopRound()
        {
            TickCancellationToken?.Cancel();

            RoundState = RoundState.Stopped;
            await UniTask.WaitUntil(() => RoundState == RoundState.Stopped);

            Punpun.Say(this, "Round stopped", LogType.ServerOnly); 
        }
    }
}