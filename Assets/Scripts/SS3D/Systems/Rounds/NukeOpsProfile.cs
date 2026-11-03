using System;
using System.Linq;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// The 24/7 Nuke Ops server profile (test-server.md T8; effort <c>2026-11_round-end-loop</c> P4): a server
    /// launched with <see cref="CommandLineFlag"/> runs Secret rounds on its own, 30 minutes each, and only
    /// draws a squad once <see cref="MinimumReadyPlayers"/> players are ready. Unlike <see cref="SandboxProfile"/>
    /// it keeps the normal server rules: no open build, no respawn, map persistence on, players ready
    /// themselves. Like it, the flag only overlays the in-memory <see cref="ServerSettings"/> at boot and
    /// never writes saved settings.
    /// </summary>
    public static class NukeOpsProfile
    {
        public const string CommandLineFlag = "-nukeops";

        public const int RoundLengthMinutes = 30;
        /// <summary>Also the <see cref="ServerSettings.Default"/> threshold: Secret below this many ready players runs Extended.</summary>
        public const int MinimumReadyPlayers = 4;

        /// <summary>Overrides <see cref="RoundLengthMinutes"/>, for tests: <c>-nukeops-round-minutes=1</c>.</summary>
        public const string RoundMinutesFlag = "-nukeops-round-minutes=";
        /// <summary>Overrides <see cref="MinimumReadyPlayers"/>, for tests: <c>-nukeops-min-ready=1</c>.</summary>
        public const string MinimumReadyFlag = "-nukeops-min-ready=";

        public static bool RequestedOnCommandLine =>
            Environment.GetCommandLineArgs().Any(arg => string.Equals(arg, CommandLineFlag, StringComparison.OrdinalIgnoreCase));

        public static string MotdFor(int minimumReady) =>
            "Nuke Ops server: rounds start on their own and last 30 minutes. "
            + $"With {minimumReady} or more ready players, a squad of Nuclear Operatives is sent to blow up the station; "
            + "fewer runs a quiet round. Everyone is in the operative draw unless they opt out in the Jobs tab. "
            + "Report bugs with /bug <what happened>.";

        /// <summary>The round length from <see cref="RoundMinutesFlag"/>, else <see cref="RoundLengthMinutes"/>.</summary>
        public static int RoundMinutesFrom(string[] args) =>
            IntFlag(args, RoundMinutesFlag, 1, 1440) ?? RoundLengthMinutes;

        /// <summary>The ready-player threshold from <see cref="MinimumReadyFlag"/>, else <see cref="MinimumReadyPlayers"/>.</summary>
        public static int MinimumReadyFrom(string[] args) =>
            IntFlag(args, MinimumReadyFlag, 1, 256) ?? MinimumReadyPlayers;

        /// <summary>The Nuke Ops rules over <paramref name="settings"/>; map and server name are kept.</summary>
        public static ServerSettings Apply(ServerSettings settings)
        {
            string[] args = Environment.GetCommandLineArgs();
            return Apply(settings, RoundMinutesFrom(args), MinimumReadyFrom(args));
        }

        public static ServerSettings Apply(ServerSettings settings, int roundLengthMinutes, int minimumReady)
        {
            settings.Mode = "Secret";
            settings.RoundLengthMinutes = roundLengthMinutes;
            settings.MinimumReadyForNukeOps = minimumReady;
            settings.AllowLateJoin = true;
            settings.AutoStart = true;
            settings.AllConnectedReady = false;
            settings.RespawnEnabled = false;
            settings.RemoveCorpseOnRespawn = false;
            settings.MapPersistence = true;
            settings.Motd = MotdFor(minimumReady);
            return settings;
        }

        private static int? IntFlag(string[] args, string prefix, int min, int max)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(arg.AsSpan(prefix.Length), out int value) && value >= min && value <= max)
                    return value;
            }

            return null;
        }
    }
}
