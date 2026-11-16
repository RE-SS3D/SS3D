using System;
using System.Linq;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// The sandbox server profile (<c>sandbox-server.md</c>): a server launched with
    /// <see cref="CommandLineFlag"/> runs short Extended rounds where everyone builds, respawn is on and
    /// nothing is saved. The flag only overlays the in-memory <see cref="ServerSettings"/> at boot and
    /// opens the map editor to every non-Restricted player; it never writes saved settings. Everything else reads the settings,
    /// not the flag, so each rule can be switched on its own.
    /// </summary>
    public static class SandboxProfile
    {
        public const string CommandLineFlag = "-sandbox";

        public const int RoundLengthMinutes = 20;
        public const int PlayerCap = 16;
        public const int RespawnCooldownSeconds = 30;
        /// <summary>The "short countdown" between sandbox rounds (sandbox-server.md); a normal server waits 120 s.</summary>
        public const int CountdownSeconds = 20;

        public const string Motd = "Sandbox server: everything is allowed. Anyone can build with the map editor (press B), "
            + "and you can respawn after dying. The station resets every 20 minutes and nothing is saved. "
            + "Report bugs with /bug <what happened>.";

        /// <summary>Overrides <see cref="RoundLengthMinutes"/>, for tests: <c>-sandbox-round-minutes=1</c>.</summary>
        public const string RoundMinutesFlag = "-sandbox-round-minutes=";

        public static bool RequestedOnCommandLine => RequestedIn(Environment.GetCommandLineArgs());

        public static bool RequestedIn(string[] args) =>
            args != null && args.Any(arg => string.Equals(arg, CommandLineFlag, StringComparison.OrdinalIgnoreCase));

        /// <summary>The round length from <see cref="RoundMinutesFlag"/>, else <see cref="RoundLengthMinutes"/>.</summary>
        public static int RoundMinutesFrom(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith(RoundMinutesFlag, StringComparison.OrdinalIgnoreCase)
                    && int.TryParse(arg.AsSpan(RoundMinutesFlag.Length), out int minutes) && minutes is >= 1 and <= 1440)
                    return minutes;
            }

            return RoundLengthMinutes;
        }

        /// <summary>The sandbox rules over <paramref name="settings"/>; map and server name are kept.</summary>
        public static ServerSettings Apply(ServerSettings settings) =>
            Apply(settings, RoundMinutesFrom(Environment.GetCommandLineArgs()));

        public static ServerSettings Apply(ServerSettings settings, int roundLengthMinutes)
        {
            settings.Mode = "Extended";
            settings.RoundLengthMinutes = roundLengthMinutes;
            settings.AllowLateJoin = true;
            settings.PlayerCap = PlayerCap;
            settings.AutoStart = true;
            settings.CountdownSeconds = CountdownSeconds;
            settings.RespawnEnabled = true;
            settings.RemoveCorpseOnRespawn = true;
            settings.RespawnCooldownSeconds = RespawnCooldownSeconds;
            settings.MapPersistence = false;
            settings.AllConnectedReady = true;
            settings.Motd = Motd;
            return settings;
        }
    }
}
