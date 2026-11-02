using System;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Permissions;
using SS3D.Systems.Persistence;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds.Messages;
using SS3D.Systems.Tile.MapEditor;

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
        public string ServerName;
        public string Motd;

        // Server rules with no admin form field yet; the sandbox profile sets them at boot
        // (SandboxProfile). A whole-form admin save keeps the current values.
        /// <summary>Starts a countdown on its own while Stopped once a player is ready (lobby.md §7).</summary>
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

        public static ServerSettings Default => new()
        {
            Map = "", Mode = "Secret", PlayerCap = 32, RoundLengthMinutes = 90, AllowLateJoin = true,
            ServerName = ServerInfoText.DefaultName, Motd = ServerInfoText.DefaultMotd,
            RespawnCooldownSeconds = 30, MapPersistence = true,
        };

        public bool IsValid => PlayerCap is >= 1 and <= 256
            && RoundLengthMinutes is >= 1 and <= 1440
            && RespawnCooldownSeconds is >= 0 and <= 3600;

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
            return result;
        }
    }

    public enum StationLoadState { NotLoaded, Loading, Initializing, Ready, Failed }

    public enum ServerSetting { Map, Mode, PlayerCap, RoundLengthMinutes, AllowLateJoin, ServerName, Motd }

    public enum RoundAdminAction { Start, ForceStart, ExtendTimer, CancelCountdown, Stop }

    public sealed partial class RoundSubSystem
    {
        private ServerSettings _serverSettings = ServerSettings.Default;
        private bool _forceStart;
        private string _serverSettingsError = "";
        // Only the public join policy is replicated; admin configuration stays behind a TargetRpc.
        [SyncVar] private bool _allowLateJoin = true;
        [SyncVar] private StationLoadState _stationLoadState;
        [SyncVar] private string _stationName = "";
        public StationLoadState CurrentStationLoadState => _stationLoadState;
        public string StationName => _stationName;
        public bool AllowLateJoin => _allowLateJoin;
        public int PlayerCap => _serverSettings.PlayerCap;
        public ServerSettings SettingsForServer => _serverSettings;
        public ServerSettings AdminSettings { get; private set; } = ServerSettings.Default;
        public string[] AdminMaps { get; private set; } = Array.Empty<string>();
        public string AdminSettingsStatus { get; private set; } = "Loading server settings…";
        public bool HasAdminSettings { get; private set; }

        private bool IsAdministrator(NetworkConnection sender)
        {
            return sender != null && SubSystems.TryGet(out PlayerSubSystem players)
                && SubSystems.TryGet(out PermissionSubSystem permissions)
                && permissions.IsAtLeast(players.GetCkey(sender), ServerRoleTypes.Administrator);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdReadServerSettings(NetworkConnection sender = null)
        {
            if (IsAdministrator(sender)) SendServerSettings(sender, "");
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdSaveServerSettings(ServerSettings settings, NetworkConnection sender = null)
        {
            SaveServerSettings(settings, sender);
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdUpdateServerSetting(ServerSetting field, ServerSettings value, NetworkConnection sender = null)
        {
            if (!IsAdministrator(sender)) return;
            if (field is ServerSetting.ServerName or ServerSetting.Motd)
            {
                SaveServerInfo(field, value);
                SendServerSettings(sender, "Server info saved.");
                return;
            }
            // Merge only the edited field with current authoritative data, never an old UI snapshot.
            ServerSettings updated = _serverSettings;
            switch (field)
            {
                case ServerSetting.Map: updated.Map = value.Map; break;
                case ServerSetting.Mode: updated.Mode = value.Mode; break;
                case ServerSetting.PlayerCap: updated.PlayerCap = value.PlayerCap; break;
                case ServerSetting.RoundLengthMinutes: updated.RoundLengthMinutes = value.RoundLengthMinutes; break;
                case ServerSetting.AllowLateJoin: updated.AllowLateJoin = value.AllowLateJoin; break;
                default: SendServerSettings(sender, "Unknown setting rejected."); return;
            }
            SaveServerSettings(updated, sender);
        }

        [Server]
        private void SaveServerSettings(ServerSettings settings, NetworkConnection sender)
        {
            if (!IsAdministrator(sender)) return;
            bool idle = CurrentRoundState is RoundState.Stopped or RoundState.Ended;
            string[] availableMaps = SubSystems.Get<PersistenceSubSystem>().ListStationTemplates().ToArray();
            if (string.IsNullOrEmpty(settings.Map)) settings.Map = DefaultStation(availableMaps);
            bool mapValid = availableMaps.Contains(settings.Map);
            if (!idle || !settings.IsValid || !mapValid
                || (settings.Mode != "Secret" && settings.Mode != "Extended" && settings.Mode != "Nuclear Emergency"))
            {
                SendServerSettings(sender, "Settings rejected. Edit valid values while the round is stopped.");
                return;
            }
            _serverSettingsError = "";
            // Name/MOTD have their own path and the server rules have no form fields;
            // a whole-form save must not roll them back.
            _serverSettings = settings.WithRulesOf(_serverSettings);
            _allowLateJoin = settings.AllowLateJoin;
            _publicMode = settings.Mode;
            SendServerSettings(sender, "Settings saved for this server session.");
        }

        /// <summary>Pushes the server rules other systems enforce outside the round subsystem.</summary>
        [Server]
        private void ApplyServerRules()
        {
            MapEditorPermissions.PersistenceAllowed = _serverSettings.MapPersistence;
        }

        [ServerRpc(RequireOwnership = false)]
        public void CmdRoundAdminAction(RoundAdminAction action, NetworkConnection sender = null)
        {
            if (!IsAdministrator(sender)) return;
            switch (action)
            {
                case RoundAdminAction.Start when CurrentRoundState is RoundState.Stopped or RoundState.Ended:
                    SetAutoStartHeld(false, "an admin started a round");
                    RequestRoundStateChange(new ChangeRoundStateMessage(true));
                    break;
                case RoundAdminAction.ForceStart when IsWarmingUp:
                    _forceStart = true;
                    break;
                case RoundAdminAction.ExtendTimer when IsWarmingUp:
                    RoundSeconds = Math.Min(RoundSeconds + 60, 3600);
                    break;
                case RoundAdminAction.CancelCountdown when IsWarmingUp:
                    CancelCountdown();
                    SetAutoStartHeld(true, "an admin cancelled the countdown");
                    break;
                case RoundAdminAction.CancelCountdown when _autoStart.SecondsLeft > 0:
                    SetAutoStartHeld(true, "an admin cancelled the countdown");
                    break;
                case RoundAdminAction.Stop when IsOngoing:
                    RequestPlayerRoundStop(sender);
                    break;
            }
        }

        /// <summary>Also clears a station load the cancelled start left in progress, so the lobby can start again.</summary>
        [Server]
        protected override void CancelCountdown()
        {
            base.CancelCountdown();
            if (_stationLoadState is StationLoadState.Loading or StationLoadState.Initializing)
            {
                _stationLoadState = StationLoadState.NotLoaded;
            }
        }

        [Server]
        private void SendServerSettings(NetworkConnection sender, string status)
        {
            string[] maps = SubSystems.Get<PersistenceSubSystem>().ListStationTemplates().ToArray();
            if (string.IsNullOrEmpty(_serverSettings.Map)) _serverSettings.Map = DefaultStation(maps);
            TargetServerSettings(sender, _serverSettings, maps, string.IsNullOrEmpty(status) ? _serverSettingsError : status);
        }

        private static string DefaultStation(string[] maps) => maps.Contains("ministation")
            ? "ministation" : maps.FirstOrDefault() ?? "";

        [TargetRpc]
        private void TargetServerSettings(NetworkConnection target, ServerSettings settings, string[] maps, string status)
        {
            AdminSettings = settings;
            AdminMaps = maps;
            AdminSettingsStatus = status;
            HasAdminSettings = true;
        }
    }
}
