using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using FishNet.Object;
using SS3D.Logging;
using SS3D.Networking;
using UnityEngine;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// Watchdog options for an unattended server (<c>2026-11_sandbox-server.md</c> Decision 13), from the
    /// command line: <c>-heartbeat-file=&lt;path&gt;</c> rewrites a Unix timestamp from the main thread every
    /// <see cref="HeartbeatSeconds"/>, so a container healthcheck can tell a stall from a busy server, and
    /// <c>-exit-after-rounds=&lt;N&gt;</c> quits with code 0 once the Nth round has reset, so the supervisor
    /// starts a fresh process. A crash still exits nonzero.
    /// </summary>
    public readonly struct ServerLifetimeOptions
    {
        public const string HeartbeatFlag = "-heartbeat-file=";
        public const string ExitAfterRoundsFlag = "-exit-after-rounds=";
        public const int HeartbeatSeconds = 5;

        /// <summary>Empty when no heartbeat was asked for.</summary>
        public string HeartbeatPath { get; }

        /// <summary>0 when the server never exits on its own.</summary>
        public int ExitAfterRounds { get; }

        public ServerLifetimeOptions(string heartbeatPath, int exitAfterRounds)
        {
            HeartbeatPath = heartbeatPath ?? string.Empty;
            ExitAfterRounds = Math.Max(0, exitAfterRounds);
        }

        public static ServerLifetimeOptions Parse(string[] args)
        {
            string heartbeat = string.Empty;
            int exitAfter = 0;
            foreach (string arg in args)
            {
                if (arg.StartsWith(HeartbeatFlag, StringComparison.OrdinalIgnoreCase))
                    heartbeat = arg.Substring(HeartbeatFlag.Length).Trim('"');
                else if (arg.StartsWith(ExitAfterRoundsFlag, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(arg.AsSpan(ExitAfterRoundsFlag.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out int rounds))
                    exitAfter = rounds;
            }

            return new ServerLifetimeOptions(heartbeat, exitAfter);
        }

        /// <summary>True once <paramref name="roundsReset"/> reaches the limit.</summary>
        public bool ShouldExit(int roundsReset) => ExitAfterRounds > 0 && roundsReset >= ExitAfterRounds;
    }

    public sealed partial class RoundSubSystem
    {
        private ServerLifetimeOptions _lifetime;
        private CancellationTokenSource _heartbeatCts;

        [Server]
        private void StartServerLifetime()
        {
            _lifetime = ServerLifetimeOptions.Parse(Environment.GetCommandLineArgs());
            if (_lifetime.ExitAfterRounds > 0)
                Log.Information(this, "Server exits after {rounds} rounds", Logs.ServerOnly, _lifetime.ExitAfterRounds);

            if (string.IsNullOrEmpty(_lifetime.HeartbeatPath))
                return;

            _heartbeatCts = new CancellationTokenSource();
            HeartbeatLoop(_lifetime.HeartbeatPath, _heartbeatCts.Token).Forget();
            Log.Information(this, "Writing heartbeat to {path}", Logs.ServerOnly, _lifetime.HeartbeatPath);
        }

        [Server]
        private void StopServerLifetime()
        {
            _heartbeatCts?.Cancel();
            _heartbeatCts?.Dispose();
            _heartbeatCts = null;
        }

        /// <summary>Runs on the main thread (PlayerLoop), so a stalled frame stops the heartbeat.</summary>
        private static async UniTaskVoid HeartbeatLoop(string path, CancellationToken cancellationToken)
        {
            TimeSpan interval = TimeSpan.FromSeconds(ServerLifetimeOptions.HeartbeatSeconds);
            bool warned = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    File.WriteAllText(path, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    if (!warned)
                        Debug.LogWarning($"[Rounds] Could not write heartbeat {path}: {exception.Message}");
                    warned = true;
                }

                if (await UniTask.Delay(interval, ignoreTimeScale: true, cancellationToken: cancellationToken).SuppressCancellationThrow())
                    return;
            }
        }

        /// <summary>After a reset: quits once the configured number of rounds has run.</summary>
        [Server]
        private void ExitIfRoundLimitReached()
        {
            if (!_lifetime.ShouldExit(_roundId))
                return;

            Log.Information(this, "Round limit {rounds} reached; exiting for a fresh process", Logs.ServerOnly, _lifetime.ExitAfterRounds);
            StopServerLifetime();
#if !UNITY_EDITOR
            ConnectionNotices.DisconnectAll(NetworkManager, DisconnectCode.ServerRestarting);
            QuitOnceClientsAreTold().Forget();
#endif
        }

        /// <summary>The notice goes out on a later tick; quitting in this frame would drop the reason.</summary>
        private static async UniTaskVoid QuitOnceClientsAreTold()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(ConnectionNotices.GraceSeconds), ignoreTimeScale: true);
            UnityEngine.Application.Quit(0);
        }
    }
}
