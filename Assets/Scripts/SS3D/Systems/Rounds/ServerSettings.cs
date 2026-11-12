using System;

namespace SS3D.Systems.Rounds
{
    [Serializable]
    public struct ServerSettings
    {
        // Empty means no preference yet; the server selects an available saved template.
        public string Map;
        public string Mode;
        public int PlayerCap;
        public int RoundLengthMinutes;
        public bool AllowLateJoin;
        /// <summary>Ready players needed before the auto-start countdown runs (lobby.md §7).</summary>
        public int MinimumReady;
        /// <summary>Nuclear Operative opt-ins a Nuke Ops round needs among ready players, 1–5; fewer runs Extended (Decision 10).</summary>
        public int MinimumOperatives;
        public string ServerName;
        public string Motd;

        // Server rules with no admin form field yet; the sandbox and Nuke Ops profiles set them at boot
        // (SandboxProfile, NukeOpsProfile). A whole-form admin save keeps the current values.
        /// <summary>
        /// Starts a countdown on its own while Stopped once <see cref="MinimumReady"/> players are ready
        /// (lobby.md §7). On by default for a dedicated server (<see cref="ForDedicatedServer"/>).
        /// </summary>
        public bool AutoStart;
        /// <summary>At round start, every connected player who isn't observing counts as ready.</summary>
        public bool AllConnectedReady;
        /// <summary>Ghosts may respawn as a fresh body after <see cref="RespawnCooldownSeconds"/>.</summary>
        public bool RespawnEnabled;
        /// <summary>Despawn the player's previous corpse after a successful respawn.</summary>
        public bool RemoveCorpseOnRespawn;
        /// <summary>Seconds from death until respawn is allowed.</summary>
        public int RespawnCooldownSeconds;
        /// <summary>Map editor save, export, load, delete, new map and the round-end autosave.</summary>
        public bool MapPersistence;
        /// <summary>
        /// Ready players Secret needs before it runs Nuke Ops; fewer runs Extended (round-end loop Decision 23).
        /// An explicit Nuclear Emergency skips the check. The <c>-nukeops</c> profile sets it (<see cref="NukeOpsProfile"/>).
        /// </summary>
        public int MinimumReadyForNukeOps;

        public static ServerSettings Default => new()
        {
            Map = "", Mode = "Secret", PlayerCap = 32, RoundLengthMinutes = 30, AllowLateJoin = true, MinimumReady = 1,
            MinimumOperatives = 1,
            ServerName = ServerInfoText.DefaultName, Motd = ServerInfoText.DefaultMotd,
            RespawnCooldownSeconds = 30, MapPersistence = true, MinimumReadyForNukeOps = NukeOpsProfile.MinimumReadyPlayers,
        };

        /// <summary>A dedicated server runs rounds on its own (round-end.md §6, Q16); a listen host starts them by hand.</summary>
        public static ServerSettings ForDedicatedServer(ServerSettings settings)
        {
            settings.AutoStart = true;
            return settings;
        }

        public bool IsValid => PlayerCap is >= 1 and <= 256
            && MinimumReady is >= 1 and <= 256
            && MinimumOperatives is >= 1 and <= Roles.SquadDraw.MaxSize
            && RoundLengthMinutes is >= 1 and <= 1440
            && RespawnCooldownSeconds is >= 0 and <= 3600
            && MinimumReadyForNukeOps is >= 1 and <= 256;

        /// <summary>These form values with the name, MOTD and server rules of <paramref name="current"/>, which the admin form does not edit.</summary>
        public ServerSettings WithRulesOf(ServerSettings current)
        {
            ServerSettings result = this;
            result.ServerName = current.ServerName;
            result.Motd = current.Motd;
            result.AutoStart = current.AutoStart;
            result.AllConnectedReady = current.AllConnectedReady;
            result.RespawnEnabled = current.RespawnEnabled;
            result.RemoveCorpseOnRespawn = current.RemoveCorpseOnRespawn;
            result.RespawnCooldownSeconds = current.RespawnCooldownSeconds;
            result.MapPersistence = current.MapPersistence;
            result.MinimumReadyForNukeOps = current.MinimumReadyForNukeOps;
            return result;
        }
    }

    /// <summary><see cref="Unplayable"/>: the map loaded but fails the round-start check (creative-mode §8).</summary>
    public enum StationLoadState { NotLoaded, Loading, Initializing, Ready, Failed, Unplayable }

    public enum ServerSetting { Map, Mode, PlayerCap, RoundLengthMinutes, AllowLateJoin, ServerName, Motd, MinimumReady, AutoStart, MinimumOperatives }

    /// <summary>
    /// <see cref="EditMapAnyway"/> starts a round on a map that failed the round-start check, so it can be fixed in the
    /// Map Editor: Extended, and bodies without a spawn marker start on the nearest deck.
    /// </summary>
    public enum RoundAdminAction { Start, ForceStart, ExtendTimer, CancelCountdown, Stop, HoldEnd, ResumeEnd, EditMapAnyway }
}
