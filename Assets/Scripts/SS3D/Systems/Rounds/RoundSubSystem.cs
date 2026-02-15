using Coimbra;
using System;
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
            Log.Information(this, "Preparing round", Logs.ServerOnly);

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
            Log.Information(this, "Starting {seconds} seconds warmup tick", Logs.ServerOnly, _warmupSeconds);

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
            #pragma warning disable CS4014
            ProcessChangeRoundState(m);
            #pragma warning restore CS4014
        }
#endif
    }
}