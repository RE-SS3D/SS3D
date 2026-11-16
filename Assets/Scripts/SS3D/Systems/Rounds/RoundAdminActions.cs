using System;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Core;
using SS3D.Systems.PlayerControl;
using SS3D.Systems.Rounds.Messages;

namespace SS3D.Systems.Rounds
{
    /// <summary>
    /// The round's station load state and the admin round controls (start, countdown, stop, the end beat). The
    /// configuration they run on lives in <see cref="ServerSettingsService"/>.
    /// </summary>
    public sealed partial class RoundSubSystem
    {
        private ServerSettingsService _settingsService;
        private bool _forceStart;
        /// <summary>The next start skips the round-start check (<see cref="RoundAdminAction.EditMapAnyway"/>).</summary>
        private bool _editMapAnyway;
        [SyncVar] private StationLoadState _stationLoadState;
        [SyncVar] private string _stationName = "";
        public StationLoadState CurrentStationLoadState => _stationLoadState;
        public string StationName => _stationName;

        /// <summary>Both live on the systems hub, so the service is there whenever the round is.</summary>
        private ServerSettingsService SettingsService
        {
            get
            {
                if (_settingsService == null) _settingsService = SubSystems.Get<ServerSettingsService>();
                return _settingsService;
            }
        }

        /// <summary>Server: the configuration this round runs on.</summary>
        private ServerSettings Settings => SettingsService.Current;

        /// <summary>Server: the configured mode runs Nuke Ops (Secret resolves to it, as in map validation).</summary>
        public bool ModeDrawsOperatives => Settings.Mode is "Secret" or "Nuclear Emergency" && !_roundFellBackToExtended;

        [Server]
        private void SubscribeToSettings()
        {
            SettingsService.ServerChanged += HandleSettingsChanged;
            SettingsService.ServerAutoStartSet += HandleAutoStartSet;
        }

        private void UnsubscribeFromSettings()
        {
            if (_settingsService == null) return;
            _settingsService.ServerChanged -= HandleSettingsChanged;
            _settingsService.ServerAutoStartSet -= HandleAutoStartSet;
        }

        private void HandleSettingsChanged(ServerSettings previous, ServerSettings current)
        {
            // The last start's verdict was about the old map or mode; don't keep offering to fix it.
            if ((current.Map != previous.Map || current.Mode != previous.Mode)
                && _stationLoadState is StationLoadState.Failed or StationLoadState.Unplayable)
                _stationLoadState = StationLoadState.NotLoaded;
        }

        private void HandleAutoStartSet() => _autoStart.Held = false;

        [ServerRpc(RequireOwnership = false)]
        public void CmdRoundAdminAction(RoundAdminAction action, NetworkConnection sender = null)
        {
            if (!ServerSettingsService.IsAdministrator(sender)) return;
            ServerRoundAdminAction(action, sender);
        }

        /// <summary>
        /// Apply an admin round action for an already-authorized caller (the lobby view through
        /// <see cref="CmdRoundAdminAction"/>, or the <c>round</c> console command). False when the round
        /// isn't in a state the action applies to.
        /// </summary>
        [Server]
        public bool ServerRoundAdminAction(RoundAdminAction action, NetworkConnection sender)
        {
            switch (action)
            {
                case RoundAdminAction.Start when CurrentRoundState is RoundState.Stopped or RoundState.Ended:
                    SetAutoStartHeld(false, "an admin started a round");
                    RequestRoundStateChange(new ChangeRoundStateMessage(true));
                    return true;
                case RoundAdminAction.EditMapAnyway when CurrentRoundState is RoundState.Stopped or RoundState.Ended
                    && _stationLoadState == StationLoadState.Unplayable:
                    _editMapAnyway = true;
                    SetAutoStartHeld(false, "an admin started a round to fix the map");
                    RequestRoundStateChange(new ChangeRoundStateMessage(true));
                    return true;
                case RoundAdminAction.ForceStart when IsWarmingUp:
                    _forceStart = true;
                    return true;
                case RoundAdminAction.ExtendTimer when IsWarmingUp:
                    RoundSeconds = Math.Min(RoundSeconds + 60, 3600);
                    return true;
                case RoundAdminAction.CancelCountdown when IsWarmingUp:
                    CancelCountdown();
                    SetAutoStartHeld(true, "an admin cancelled the countdown");
                    return true;
                case RoundAdminAction.CancelCountdown when _autoStart.SecondsLeft > 0:
                    SetAutoStartHeld(true, "an admin cancelled the countdown");
                    return true;
                case RoundAdminAction.Stop when IsOngoing:
                    RequestPlayerRoundStop(sender);
                    return true;
                case RoundAdminAction.HoldEnd when CurrentRoundState == RoundState.Ending:
                    SetEndHeld(true, SubSystems.Get<PlayerSubSystem>().GetCkey(sender));
                    return true;
                case RoundAdminAction.ResumeEnd when CurrentRoundState == RoundState.Ending:
                    SetEndHeld(false, SubSystems.Get<PlayerSubSystem>().GetCkey(sender));
                    return true;
                default:
                    return false;
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

        /// <summary>
        /// An admin's Stop. With unsaved map edits <see cref="MapEditorStopFlow"/> asks Save and stop / Discard /
        /// Cancel first, and ends the round itself once answered.
        /// </summary>
        [Server]
        protected override void RequestPlayerRoundStop(NetworkConnection sender)
        {
            if (IsOngoing && SubSystems.TryGet(out MapEditorStopFlow stopFlow) && stopFlow.TryPrompt(sender))
                return;
            RequestRoundEnd(RoundEndRequest.Admin);
        }
    }
}
