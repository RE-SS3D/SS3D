using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Logging;
using SS3D.Systems.Comms;
using SS3D.Systems.Rounds.Messages;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// The lobby's self-starting countdown (<c>lobby.md</c> §7), one tick per second. While the round is
    /// idle and enough players are ready, it counts down and then asks for a start. An admin hold
    /// (cancelling a countdown, or a failed station load) stops it until an admin starts a round.
    /// </summary>
    public sealed class AutoStartCountdown
    {
        public const int CountdownSeconds = 20;

        /// <summary>Seconds until the start request; 0 when not counting.</summary>
        public int SecondsLeft { get; private set; }

        public bool Held { get; set; }

        /// <summary>Advances one second; true exactly when the round should start now.</summary>
        public bool Tick(bool enabled, bool idle, int readyCount, int minimumReady = 1)
        {
            if (!enabled || Held || !idle || readyCount < Math.Max(1, minimumReady))
            {
                SecondsLeft = 0;
                return false;
            }

            if (SecondsLeft == 0)
            {
                SecondsLeft = CountdownSeconds;
                return false;
            }

            SecondsLeft--;
            return SecondsLeft == 0;
        }
    }

    /// <summary>Warnings before a timed round ends (<c>sandbox-server.md</c> §3).</summary>
    public static class RoundEndWarnings
    {
        /// <summary>The warning for this many seconds left, or null when none is due.</summary>
        public static string For(int secondsLeft) => secondsLeft switch
        {
            300 => "The round ends in 5 minutes.",
            60 => "The round ends in 1 minute.",
            _ => null,
        };
    }

    public sealed partial class RoundSubSystem
    {
        private readonly AutoStartCountdown _autoStart = new();
        private CancellationTokenSource _autoStartCts;

        [SyncVar] private bool _autoStartEnabled;
        [SyncVar] private bool _autoStartHeld;
        [SyncVar] private int _autoStartSeconds;

        /// <summary>The server starts rounds on its own (<see cref="ServerSettings.AutoStart"/>).</summary>
        public bool AutoStartEnabled => _autoStartEnabled;
        /// <summary>An admin hold or failed load paused auto-start until an admin starts a round.</summary>
        public bool AutoStartHeld => _autoStartHeld;
        /// <summary>Seconds left on the lobby auto-start countdown; 0 when not counting.</summary>
        public int AutoStartSecondsLeft => _autoStartSeconds;

        [Server]
        private void StartAutoStartLoop()
        {
            StopAutoStartLoop();
            _autoStartCts = new CancellationTokenSource();
            AutoStartLoop(_autoStartCts.Token).Forget();
        }

        [Server]
        private void StopAutoStartLoop()
        {
            _autoStartCts?.Cancel();
            _autoStartCts?.Dispose();
            _autoStartCts = null;
        }

        private async UniTaskVoid AutoStartLoop(CancellationToken cancellationToken)
        {
            TimeSpan second = TimeSpan.FromSeconds(1);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (await UniTask.Delay(second, ignoreTimeScale: true, cancellationToken: cancellationToken)
                        .SuppressCancellationThrow())
                    return;

                TickAutoStart();
            }
        }

        [Server]
        private void TickAutoStart()
        {
            bool idle = CurrentRoundState is RoundState.Stopped or RoundState.Ended;
            int ready = SubSystems.TryGet(out ReadyPlayersSubSystem readyPlayers)
                ? readyPlayers.EffectiveReadyCount(_serverSettings.AllConnectedReady)
                : 0;
            bool start = _autoStart.Tick(_serverSettings.AutoStart, idle, ready, _serverSettings.MinimumReady);
            _autoStartEnabled = _serverSettings.AutoStart;
            _autoStartHeld = _autoStart.Held;
            _autoStartSeconds = _autoStart.SecondsLeft;
            if (!start)
                return;

            Log.Information(this, "Auto-starting round with {ready} ready players", Logs.ServerOnly, ready);
            RequestRoundStateChange(new ChangeRoundStateMessage(true));
        }

        /// <summary>An admin start lifts a hold; an admin cancel or a failed load sets one.</summary>
        [Server]
        private void SetAutoStartHeld(bool held, string reason)
        {
            if (!_serverSettings.AutoStart || _autoStart.Held == held)
                return;

            _autoStart.Held = held;
            _autoStartHeld = held;
            _autoStartSeconds = held ? 0 : _autoStartSeconds;
            Log.Information(this, held ? "Auto-start held: {reason}" : "Auto-start resumed: {reason}", Logs.ServerOnly, reason);
            if (held && SubSystems.TryGet(out CommsSubSystem comms))
                comms.PostOocSystem(OocChannel.Admin, $"Auto-start is held ({reason}). Start a round to resume it.");
        }

        /// <summary>Posts the 5- and 1-minute warnings during an Ongoing round.</summary>
        [Server]
        private void PostRoundEndWarning(int secondsLeft)
        {
            string warning = RoundEndWarnings.For(secondsLeft);
            if (warning == null || !SubSystems.TryGet(out CommsSubSystem comms))
                return;

            Log.Information(this, warning, Logs.ServerOnly);
            comms.SendAnnouncement(warning);
            comms.PostOocSystem(OocChannel.Ooc, warning);
        }
    }
}
