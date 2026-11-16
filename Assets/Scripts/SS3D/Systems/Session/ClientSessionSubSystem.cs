using System;
using FishNet;
using FishNet.Connection;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Permissions;
using SS3D.Systems.Comms;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Entities.Character;
using SS3D.Systems.Roles.Messages;
using SS3D.Systems.Rounds;

namespace SS3D.Systems.Session
{
    /// <summary>
    /// Client: what this player tells the server once per connection (tech-debt P6.6), moved out of the lobby UI.
    /// The server keeps job preferences and the appearance per ckey and drops them from unknown senders, so each
    /// goes out once the replicated player record exists, again after a reconnect, and whenever the player changes
    /// it. The OOC history is asked for once per connection. An admin asks for the Server Settings once; after
    /// that the server pushes every change (<see cref="ServerSettingsService"/>), and <see cref="AdminSettingsChanged"/>
    /// tells the lobby.
    /// </summary>
    public sealed class ClientSessionSubSystem : SubSystem
    {
        private NetworkConnection _loggedInAs;
        private bool _preferencesSent;
        private bool _appearanceSent;
        private bool _adminSettingsRequested;
        private ServerSettingsService _settingsService;

        /// <summary>Client: new admin Server Settings arrived (an action reply or a server push).</summary>
        public event Action AdminSettingsChanged;

        /// <summary>The server knows this client's player: preferences sent now reach it.</summary>
        public bool LoggedIn => _loggedInAs != null;

        /// <summary>The local player is an administrator, as far as the replicated permissions say.</summary>
        public static bool LocalPlayerIsAdmin() => SubSystems.TryGet(out PermissionSubSystem permissions)
            && permissions.HasLoadedPermissions
            && !string.IsNullOrEmpty(SS3D.Core.Settings.LocalPlayer.Ckey)
            && permissions.IsAtLeast(SS3D.Core.Settings.LocalPlayer.Ckey, ServerRoleTypes.Administrator);

        /// <summary>
        /// Logged in, not merely connected: a refused admin name stays connected through the disconnect
        /// grace, and the server has no name for that connection to check.
        /// </summary>
        public static bool ShouldRequestAdminSettings(bool isAdmin, bool hasSettingsService, bool loggedIn) =>
            isAdmin && hasSettingsService && loggedIn;

        /// <summary>
        /// Client: the full job preference list, read when it is sent; null while there is none yet (the lobby
        /// sets this).
        /// </summary>
        public Func<JobPreferencesMessage?> JobPreferencesSource { get; set; }

        /// <summary>
        /// Client: the saved character (never unsaved creator edits), read when it is sent; the server applies it at
        /// the next spawn. Null while there is none yet.
        /// </summary>
        public Func<CharacterAppearance?> AppearanceSource { get; set; }

        /// <summary>Client: the job preferences changed; sent now when logged in.</summary>
        public void JobPreferencesChanged()
        {
            _preferencesSent = false;
            SendPending();
        }

        /// <summary>Client: the saved character changed; sent now when logged in.</summary>
        public void AppearanceChanged()
        {
            _appearanceSent = false;
            SendPending();
        }

        protected override void OnDestroyed()
        {
            BindSettingsService(null);
            base.OnDestroyed();
        }

        private void Update()
        {
            NetworkConnection connection = CurrentLogin();
            if (connection != _loggedInAs)
            {
                _loggedInAs = connection;
                _preferencesSent = false;
                _appearanceSent = false;
                _adminSettingsRequested = false;
                if (connection != null && SubSystems.TryGet(out CommsSubSystem comms))
                    comms.RequestOocHistory();
            }

            // The hub (and with it the settings service) is new for every session.
            BindSettingsService(SubSystems.TryGet(out ServerSettingsService service) ? service : null);
            SendPending();
        }

        /// <summary>The connection, once the replicated player record for it exists; else null.</summary>
        private static NetworkConnection CurrentLogin()
        {
            if (InstanceFinder.ClientManager == null || !InstanceFinder.ClientManager.Started
                || !SubSystems.TryGet(out PlayerSubSystem players))
                return null;

            NetworkConnection connection = InstanceFinder.ClientManager.Connection;
            return players.GetPlayer(connection) != null ? connection : null;
        }

        private void SendPending()
        {
            if (_loggedInAs == null)
                return;

            if (!_preferencesSent && JobPreferencesSource?.Invoke() is { } preferences)
            {
                InstanceFinder.ClientManager.Broadcast(preferences);
                _preferencesSent = true;
            }

            if (!_appearanceSent && AppearanceSource?.Invoke() is { } appearance)
            {
                InstanceFinder.ClientManager.Broadcast(new CharacterAppearanceMessage { Appearance = appearance });
                _appearanceSent = true;
            }

            bool admin = LocalPlayerIsAdmin();
            if (!admin)
            {
                // Asked again if promoted later; the server only pushes to current admins.
                _adminSettingsRequested = false;
            }
            else if (!_adminSettingsRequested && ShouldRequestAdminSettings(admin, _settingsService != null, LoggedIn))
            {
                _settingsService.CmdReadServerSettings();
                _adminSettingsRequested = true;
            }
        }

        private void BindSettingsService(ServerSettingsService service)
        {
            if (service == _settingsService)
                return;

            if (_settingsService != null)
                _settingsService.AdminSettingsReceived -= HandleAdminSettingsReceived;
            _settingsService = service;
            _adminSettingsRequested = false;
            if (_settingsService != null)
                _settingsService.AdminSettingsReceived += HandleAdminSettingsReceived;
        }

        private void HandleAdminSettingsReceived() => AdminSettingsChanged?.Invoke();
    }
}
