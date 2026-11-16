using System;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Coimbra;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Management;
using SS3D.Data.Persistence;
using SS3D.Logging;
using SS3D.Networking;
using SS3D.Networking.Settings;
using SS3D.Permissions;
using SS3D.Systems.Persistence;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Tile.MapEditor;
using UnityEngine;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// The server's configuration (tech-debt P6.5): the authoritative <see cref="ServerSettings"/>, the boot profiles
    /// (<c>-sandbox</c>, <c>-nukeops</c>), the saved name and MOTD, the public lobby metadata, and the admin
    /// Server Settings RPCs. <see cref="RoundSubSystem"/> reads <see cref="Current"/> and owns the round state.
    /// An admin asks once (<see cref="CmdReadServerSettings"/>); after that the server pushes every change to every
    /// admin (P6.6), including saved maps that appear on disk.
    /// </summary>
    public sealed class ServerSettingsService : NetworkSubSystem
    {
        /// <summary>How often the server looks for a changed saved-map list while an admin is online.</summary>
        private const float MapScanSeconds = 2f;

        private ServerSettings _settings = ServerSettings.Default;
        private string _statusNotice = "";
        private bool _pushPending;
        private float _nextMapScan;
        private string[] _pushedMaps = Array.Empty<string>();

        // Public lobby metadata. Name/MOTD persist under ServerMeta; the rest describe this server process.
        // Only the public join policy is replicated; admin configuration stays behind a TargetRpc.
        [SyncVar] private bool _allowLateJoin = true;
        [SyncVar] private string _serverName = ServerInfoText.DefaultName;
        [SyncVar] private string _motd = ServerInfoText.DefaultMotd;
        [SyncVar] private string _publicMode = ServerSettings.Default.Mode;
        [SyncVar] private string _serverBuild = "";
        [SyncVar] private long _serverStartedUnix;
        [SyncVar] private bool _antagonistOptInDefault;
        [SyncVar] private int _publicPlayerCap = ServerSettings.Default.PlayerCap;
        [SyncVar] private int _publicMinimumConnected = ServerSettings.Default.MinimumConnected;

        /// <summary>
        /// Server: an admin save replaced the round configuration (previous, current). Name/MOTD and auto start
        /// edits don't raise it.
        /// </summary>
        public event Action<ServerSettings, ServerSettings> ServerChanged;

        /// <summary>Server: an admin set the auto start rule (on or off, even to its current value).</summary>
        public event Action ServerAutoStartSet;

        /// <summary>Server: the authoritative settings.</summary>
        public ServerSettings Current => _settings;
        public int PlayerCap => _settings.PlayerCap;

        /// <summary>Public: the player cap, for the lobby header's player count.</summary>
        public int PublicPlayerCap => _publicPlayerCap;

        /// <summary>Public: connected players the lobby countdown waits for (<see cref="ServerSettings.MinimumConnected"/>).</summary>
        public int PublicMinimumConnected => _publicMinimumConnected;

        public bool AllowLateJoin => _allowLateJoin;
        public string ServerName => _serverName;
        public string Motd => _motd;
        /// <summary>The configured mode (Secret stays secret: it only resolves at round start).</summary>
        public string PublicMode => _publicMode;
        public string ServerBuild => _serverBuild;
        /// <summary>Public: whether an antagonist category a character never set counts as opted in (<see cref="ServerSettings.AntagonistOptInDefault"/>).</summary>
        public bool AntagonistOptInDefault => _antagonistOptInDefault;
        public DateTimeOffset ServerStarted => DateTimeOffset.FromUnixTimeSeconds(_serverStartedUnix);

        public ServerSettings AdminSettings { get; private set; } = ServerSettings.Default;
        public string[] AdminMaps { get; private set; } = Array.Empty<string>();
        public string AdminSettingsStatus { get; private set; } = "Loading server settings…";
        /// <summary>Client: replies to this admin's settings actions so far (not periodic refreshes).</summary>
        public int AdminActionReplies { get; private set; }
        public bool HasAdminSettings { get; private set; }
        /// <summary>Client: counts admin snapshots received, so a view can tell it missed one.</summary>
        public int AdminSettingsVersion { get; private set; }

        /// <summary>Client: new admin settings arrived (an action reply or a push).</summary>
        public event Action AdminSettingsReceived;

        /// <summary>The map a server plays until an admin picks another: Classic MiniStation, else the first one saved.</summary>
        public static string DefaultStation(string[] maps) => maps.Contains("classic-ministation")
            ? "classic-ministation" : maps.FirstOrDefault() ?? "";

        public override void OnStartServer()
        {
            base.OnStartServer();
            SavedServerInfo saved = LocalStorage.LoadObject<SavedServerInfo>(PersistencePaths.ServerInfo);
            _settings.ServerName = ServerInfoText.SanitizeName(saved?.serverName);
            _settings.Motd = ServerInfoText.SanitizeMotd(saved?.motd);
            if (ScriptableSettings.GetOrFind<NetworkSettings>().NetworkType == NetworkType.DedicatedServer)
                _settings = ServerSettings.ForDedicatedServer(_settings);
            if (SandboxProfile.RequestedOnCommandLine)
            {
                // In memory only: the saved server info and settings stay as they are.
                _settings = SandboxProfile.Apply(_settings);
                Log.Information(this, "Sandbox profile applied", Logs.ServerOnly);
                if (NukeOpsProfile.RequestedOnCommandLine)
                    Log.Warning(this, "Both -sandbox and -nukeops given; the sandbox profile wins", Logs.ServerOnly);
            }
            else if (NukeOpsProfile.RequestedOnCommandLine)
            {
                // In memory only, as above.
                _settings = NukeOpsProfile.Apply(_settings);
                Log.Information(this, "Nuke Ops profile applied: {minutes} min rounds, {connected} connected to count down, "
                    + "{ready} ready players for a squad", Logs.ServerOnly, _settings.RoundLengthMinutes,
                    _settings.MinimumConnected, _settings.MinimumReadyForNukeOps);
            }
            ApplyServerRules();
            PublishPublicSettings();
            _serverBuild = BuildInfo.Label;
            _serverStartedUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public override void OnStopServer()
        {
            MapEditorPermissions.PersistenceAllowed = true;
            base.OnStopServer();
        }

        /// <summary>
        /// Server: picks the default station when none is selected yet, and returns the selection (empty when no
        /// station is saved at all).
        /// </summary>
        [Server]
        public string EnsureMap(string[] availableMaps = null)
        {
            if (string.IsNullOrEmpty(_settings.Map))
                _settings.Map = DefaultStation(availableMaps ?? SubSystems.Get<PersistenceSubSystem>().ListStationTemplates().ToArray());
            return _settings.Map;
        }

        /// <summary>
        /// Server: the round's last verdict for admins (a failed start, an Extended fallback); empty clears it.
        /// Shown as the Server Settings status until the next action reply.
        /// </summary>
        [Server]
        public void SetStatusNotice(string notice)
        {
            notice ??= "";
            if (notice == _statusNotice) return;
            _statusNotice = notice;
            _pushPending = true;
        }

        /// <summary>Server: sends pending changes, and a changed saved-map list, to every admin.</summary>
        private void Update()
        {
            if (!IsServer) return;
            if (!_pushPending && Time.unscaledTime < _nextMapScan) return;
            _nextMapScan = Time.unscaledTime + MapScanSeconds;
            bool anyAdmin = false;
            foreach (NetworkConnection connection in ServerManager.Clients.Values)
                anyAdmin |= IsAdministrator(connection);
            if (!anyAdmin)
            {
                _pushPending = false;
                return;
            }

            // Listing also imports archives dropped into the maps folder, so a new map shows up here.
            string[] maps = SubSystems.Get<PersistenceSubSystem>().ListStationTemplates().ToArray();
            if (!_pushPending && maps.SequenceEqual(_pushedMaps)) return;
            _pushPending = false;
            // Only a broadcast moves the baseline: a one-admin reply must not hide a new map from the others.
            _pushedMaps = maps;
            foreach (NetworkConnection connection in ServerManager.Clients.Values)
                if (IsAdministrator(connection)) SendServerSettings(connection, "", refresh: true, maps);
        }

        /// <summary>Server: the sender is an administrator. The admin RPCs of the round services share this check.</summary>
        internal static bool IsAdministrator(NetworkConnection sender)
        {
            return sender != null && SubSystems.TryGet(out PlayerSubSystem players)
                && SubSystems.TryGet(out PermissionSubSystem permissions)
                && permissions.IsAtLeast(players.GetCkey(sender), ServerRoleTypes.Administrator);
        }

        private static bool RoundIsIdle =>
            !SubSystems.TryGet(out RoundSubSystem rounds) || rounds.CurrentRoundState is RoundState.Stopped or RoundState.Ended;

        [ServerRpc(RequireOwnership = false)]
        public void CmdReadServerSettings(NetworkConnection sender = null)
        {
            if (IsAdministrator(sender)) SendServerSettings(sender, "", refresh: true);
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
            if (field == ServerSetting.AutoStart)
            {
                // A server rule, not round config: it applies at once, in any round state.
                _settings.AutoStart = value.AutoStart;
                _pushPending = true;
                ServerAutoStartSet?.Invoke();
                SendServerSettings(sender, value.AutoStart ? "Auto start is on." : "Auto start is off.");
                return;
            }
            // Merge only the edited field with current authoritative data, never an old UI snapshot.
            ServerSettings updated = _settings;
            switch (field)
            {
                case ServerSetting.Map: updated.Map = value.Map; break;
                case ServerSetting.Mode: updated.Mode = value.Mode; break;
                case ServerSetting.PlayerCap: updated.PlayerCap = value.PlayerCap; break;
                case ServerSetting.RoundLengthMinutes: updated.RoundLengthMinutes = value.RoundLengthMinutes; break;
                case ServerSetting.AllowLateJoin: updated.AllowLateJoin = value.AllowLateJoin; break;
                case ServerSetting.MinimumConnected: updated.MinimumConnected = value.MinimumConnected; break;
                case ServerSetting.CountdownSeconds: updated.CountdownSeconds = value.CountdownSeconds; break;
                case ServerSetting.MinimumOperatives: updated.MinimumOperatives = value.MinimumOperatives; break;
                default: SendServerSettings(sender, "Unknown setting rejected."); return;
            }
            SaveServerSettings(updated, sender);
        }

        [Server]
        private void SaveServerSettings(ServerSettings settings, NetworkConnection sender)
        {
            if (!IsAdministrator(sender)) return;
            string[] availableMaps = SubSystems.Get<PersistenceSubSystem>().ListStationTemplates().ToArray();
            if (string.IsNullOrEmpty(settings.Map)) settings.Map = DefaultStation(availableMaps);
            bool mapValid = availableMaps.Contains(settings.Map);
            if (!RoundIsIdle || !settings.IsValid || !mapValid
                || (settings.Mode != "Secret" && settings.Mode != "Extended" && settings.Mode != "Nuclear Emergency"))
            {
                SendServerSettings(sender, "Settings rejected. Edit valid values while the round is stopped.");
                return;
            }
            _statusNotice = "";
            ServerSettings previous = _settings;
            // Name/MOTD have their own path and the server rules have no form fields;
            // a whole-form save must not roll them back.
            _settings = settings.WithRulesOf(_settings);
            PublishPublicSettings();
            _pushPending = true;
            ServerChanged?.Invoke(previous, _settings);
            SendServerSettings(sender, "Settings saved for this server session.");
        }

        /// <summary>Name/MOTD are not round configuration: admins may edit them in any round state.</summary>
        [Server]
        private void SaveServerInfo(ServerSetting field, ServerSettings value)
        {
            if (field == ServerSetting.ServerName) _settings.ServerName = ServerInfoText.SanitizeName(value.ServerName);
            else _settings.Motd = ServerInfoText.SanitizeMotd(value.Motd);
            _serverName = _settings.ServerName;
            _motd = _settings.Motd;
            _pushPending = true;
            bool saved = LocalStorage.SaveObject(PersistencePaths.ServerInfo,
                new SavedServerInfo { serverName = _settings.ServerName, motd = _settings.Motd }, overwrite: true);
            if (!saved) Debug.LogWarning("[Rounds] Could not save server info; the change applies to this session only.");
        }

        /// <summary>Copies the settings players may see into their SyncVars.</summary>
        [Server]
        private void PublishPublicSettings()
        {
            _allowLateJoin = _settings.AllowLateJoin;
            _publicMode = _settings.Mode;
            _antagonistOptInDefault = _settings.AntagonistOptInDefault;
            _publicPlayerCap = _settings.PlayerCap;
            _publicMinimumConnected = _settings.MinimumConnected;
            _serverName = _settings.ServerName;
            _motd = _settings.Motd;
        }

        /// <summary>Pushes the server rules other systems enforce outside the round subsystem.</summary>
        [Server]
        private void ApplyServerRules()
        {
            MapEditorPermissions.PersistenceAllowed = _settings.MapPersistence;
        }

        [Server]
        /// <param name="refresh">A read or push that carries no action result of its own.</param>
        private void SendServerSettings(NetworkConnection sender, string status, bool refresh = false, string[] maps = null)
        {
            maps ??= SubSystems.Get<PersistenceSubSystem>().ListStationTemplates().ToArray();
            EnsureMap(maps);
            TargetServerSettings(sender, _settings, maps, string.IsNullOrEmpty(status) ? _statusNotice : status, refresh);
        }

        [TargetRpc]
        private void TargetServerSettings(NetworkConnection target, ServerSettings settings, string[] maps, string status, bool refresh)
        {
            AdminSettings = settings;
            AdminMaps = maps;
            // A refresh with no server error keeps the last action's result ("Settings rejected…"), which the
            // once-a-second re-read used to wipe before the admin could read it.
            if (!refresh || !string.IsNullOrEmpty(status))
                AdminSettingsStatus = status;
            if (!refresh)
                AdminActionReplies++;
            HasAdminSettings = true;
            AdminSettingsVersion++;
            AdminSettingsReceived?.Invoke();
        }

#if UNITY_EDITOR
        /// <summary>
        /// Automation only: replaces the server settings and rules the way <c>-sandbox</c> does at boot,
        /// so a pilot in the editor can run the sandbox profile without a command-line flag.
        /// </summary>
        [Server]
        public void ApplyServerSettingsForTesting(ServerSettings settings)
        {
            _settings = settings;
            PublishPublicSettings();
            ApplyServerRules();
            _pushPending = true;
        }
#endif
    }
}
