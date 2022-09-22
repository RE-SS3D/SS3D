using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Round system base implementation for basic round functionality
    /// </summary>
    public class RoundSystem : RoundSystemBase
    {
        /// <summary>
        /// Round loop runner
        /// </summary>
        protected override async UniTask ProcessStartRound()
        {
            if (!IsServer) { return; }

            await StopRound();
            await PrepareRound();
            await ProcessRoundTick();
            await ProcessEndRound();
            await StopRound();
        }

        [Server]
        protected override async UniTask PrepareRound()
        {
            string author = $"[{nameof(RoundSystem)}]".Colorize(LogColors.ServerOnly);
            Debug.Log($"{author} - Preparing round");

            RoundState = RoundState.Preparing;

            await UniTask.WaitUntil(() => RoundState == RoundState.Preparing);
        }

        [Server]
        protected override async UniTask ProcessRoundTick()
        {
            string author = $"[{nameof(RoundSystem)}]".Colorize(LogColors.ServerOnly);
            Debug.Log($"{author} - Starting warmup tick");

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

            Debug.Log($"{author} - Starting round tick");

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

            string author = $"[{nameof(RoundSystem)}]".Colorize(LogColors.ServerOnly);
            Debug.Log($"{author} - Ending round");
        }

        [Server]
        protected override async UniTask StopRound()
        {
            TickCancellationToken?.Cancel();

            RoundState = RoundState.Stopped;
            await UniTask.WaitUntil(() => RoundState == RoundState.Stopped);

            string author = $"[{nameof(RoundSystem)}]".Colorize(LogColors.ServerOnly);
            Debug.Log($"{author} - Round stopped"); 
        }

    }
}