using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Rounds;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    public sealed partial class LobbyShellView
    {
        private StationLoadState _stationLoadState;
        private string _stationName = "";
        private Label _stationLoadLabel;
        private Label _headerMapLabel;

        public void SetStationLoadState(StationLoadState state, string stationName)
        {
            _stationLoadState = state;
            _stationName = stationName ?? "";
            if (_headerMapLabel != null) _headerMapLabel.text = string.IsNullOrEmpty(_stationName) ? "No map loaded" : _stationName;
            Label infoMap = _root?.Q<Label>("server-info-map");
            if (infoMap != null) infoMap.text = string.IsNullOrEmpty(_stationName) ? "No map loaded" : _stationName;
            UpdateStationIndicator(_stationLoadLabel);
            UpdateStationIndicator(_tabContent?.Q<Label>("server-map-load-status"));
        }

        private void UpdateStationIndicator(Label label)
        {
            if (label == null) return;
            label.text = _stationLoadState switch
            {
                StationLoadState.Loading => $"Loading map: {_stationName}…",
                StationLoadState.Initializing => $"Initializing station: {_stationName}…",
                StationLoadState.Ready => $"Map ready: {_stationName}",
                StationLoadState.Failed => "Map load failed — select a map and retry",
                _ => "No map loaded",
            };
            label.EnableInClassList("lobby-shell__map-load-status--loading",
                _stationLoadState is StationLoadState.Loading or StationLoadState.Initializing);
            label.EnableInClassList("lobby-shell__map-load-status--ready", _stationLoadState == StationLoadState.Ready);
            label.EnableInClassList("lobby-shell__map-load-status--failed", _stationLoadState == StationLoadState.Failed);
        }

        private ServerSettings _serverSettings = ServerSettings.Default;
        private string[] _serverMaps = Array.Empty<string>();
        private bool _hasServerSettings;
        private string _serverSettingsStatus = "Loading server settings…";
        private const float PendingServerTextSeconds = 5f;
        private readonly Dictionary<ServerSetting, (string Value, float Expires)> _pendingServerText = new();
        private readonly HashSet<VisualElement> _editingFields = new();
        private bool _endHeld;
        private int _endSecondsLeft;

        /// <summary>The round-end summary beat (Ending); call before <c>SetSession</c>.</summary>
        public void SetRoundEnd(bool held, int secondsLeft)
        {
            _endHeld = held;
            _endSecondsLeft = secondsLeft;
        }
        public event Action<ServerSetting, ServerSettings> ServerSettingsChanged;
        public event Action<RoundAdminAction> RoundAdminRequested;

        public void SetServerSettings(ServerSettings settings, string[] maps, string status)
        {
            _serverSettings = settings;
            ApplyPendingServerText();
            _serverMaps = maps ?? Array.Empty<string>();
            _hasServerSettings = true;
            _serverSettingsStatus = status ?? "";
            RefreshServerSettings();
        }

        private void BuildServerSettingsTab(VisualElement parent)
        {
            _editingFields.Clear();
            ScrollView scroll = new() { name = "server-settings-scroll" };
            scroll.AddToClassList("lobby-shell__server-settings-scroll");
            parent.Add(scroll);
            VisualElement form = new() { name = "server-settings-form" };
            form.AddToClassList("lobby-shell__server-settings");
            scroll.Add(form);

            VisualElement card = new();
            card.AddToClassList("lobby-shell__round-card");
            Label heading = new("●   ROUND STATE");
            heading.AddToClassList("lobby-shell__round-card-heading");
            heading.AddToClassList("font-titling");
            card.Add(heading);
            VisualElement body = new();
            body.AddToClassList("lobby-shell__round-card-body");
            VisualElement state = new();
            Label title = new() { name = "server-round-state" };
            title.AddToClassList("lobby-shell__round-state");
            title.AddToClassList("font-titling");
            state.Add(title);
            Label detail = new() { name = "server-round-detail" };
            detail.AddToClassList("lobby-shell__round-detail");
            detail.AddToClassList("font-terminal");
            state.Add(detail);
            body.Add(state);
            VisualElement ready = new();
            ready.Add(new Label("READY"));
            Label ratio = new() { name = "server-ready-ratio" };
            ratio.AddToClassList("lobby-shell__ready-ratio");
            ratio.AddToClassList("font-terminal");
            ready.Add(ratio);
            body.Add(ready);
            card.Add(body);
            form.Add(card);

            form.Add(SectionTitle("Round Controls"));
            VisualElement controls = new();
            controls.AddToClassList("lobby-shell__admin-controls");
            AddRoundAction(controls, "Start Round", "server-start", RoundAdminAction.Start);
            AddRoundAction(controls, "Force Start", "server-force", RoundAdminAction.ForceStart, true);
            AddRoundAction(controls, "Extend Timer", "server-extend", RoundAdminAction.ExtendTimer, true);
            AddRoundAction(controls, "Cancel Countdown", "server-cancel", RoundAdminAction.CancelCountdown, true);
            // Stop is an in-round extension to the pre-round reference.
            AddRoundAction(controls, "Stop Round", "server-stop", RoundAdminAction.Stop, true);
            // The 30 s round-end summary: hold it for a discussion, resume to return everyone to the lobby.
            AddRoundAction(controls, "Hold Summary", "server-hold-end", RoundAdminAction.HoldEnd, true);
            AddRoundAction(controls, "Resume Summary", "server-resume-end", RoundAdminAction.ResumeEnd, true);
            form.Add(controls);

            form.Add(SectionTitle("Server Info"));
            AddServerText(form, "Server Name", "server-name", ServerSetting.ServerName, ServerInfoText.MaxNameLength, false,
                value => _serverSettings.ServerName = value);
            AddServerText(form, "Message of the Day", "server-motd", ServerSetting.Motd, ServerInfoText.MaxMotdLength, true,
                value => _serverSettings.Motd = value);

            form.Add(SectionTitle("Map & Mode"));
            DropdownField map = new("Map") { name = "server-map" };
            map.AddToClassList("lobby-shell__server-select");
            map.AddToClassList("lobby-shell__server-map");
            Label swatch = new("M") { name = "server-map-initial" };
            swatch.AddToClassList("lobby-shell__server-map-swatch");
            swatch.pickingMode = PickingMode.Ignore;
            map.Add(swatch);
            map.RegisterValueChangedCallback(e =>
            {
                _serverSettings.Map = e.newValue;
                SaveServerSettings(ServerSetting.Map);
            });
            form.Add(map);
            Label mapStatus = new() { name = "server-map-load-status" };
            mapStatus.AddToClassList("lobby-shell__map-load-status");
            form.Add(mapStatus);
            UpdateStationIndicator(mapStatus);
            DropdownField mode = new("Game Mode", new List<string> { "Secret", "Extended", "Nuclear Emergency" }, 0)
                { name = "server-mode" };
            mode.AddToClassList("lobby-shell__server-select");
            mode.RegisterValueChangedCallback(e => { _serverSettings.Mode = e.newValue; SaveServerSettings(ServerSetting.Mode); });
            form.Add(mode);

            VisualElement numbers = new();
            numbers.AddToClassList("lobby-shell__server-numbers");
            AddServerNumber(numbers, "Player Cap", "server-player-cap", ServerSetting.PlayerCap, value => _serverSettings.PlayerCap = value);
            AddServerNumber(numbers, "Round Length (min)", "server-round-length", ServerSetting.RoundLengthMinutes, value => _serverSettings.RoundLengthMinutes = value);
            AddServerNumber(numbers, "Minimum Ready", "server-minimum-ready", ServerSetting.MinimumReady, value => _serverSettings.MinimumReady = value);
            AddServerNumber(numbers, "Minimum Operatives", "server-minimum-operatives", ServerSetting.MinimumOperatives, value => _serverSettings.MinimumOperatives = value);
            form.Add(numbers);
            form.Add(SectionTitle("Round Rules"));
            Toggle late = ServerRule("Allow Late Join", "server-late-join", true, "");
            late.RegisterValueChangedCallback(e => { _serverSettings.AllowLateJoin = e.newValue; SaveServerSettings(ServerSetting.AllowLateJoin); });
            form.Add(late);
            Toggle autoStart = ServerRule("Auto Start Rounds", "server-auto-start", true,
                "Start a countdown on its own once enough players are ready, and again after every round.");
            autoStart.RegisterValueChangedCallback(e => { _serverSettings.AutoStart = e.newValue; SaveServerSettings(ServerSetting.AutoStart); });
            form.Add(autoStart);
            form.Add(ServerRule("Random Events", "server-random-events", false, "Random events are not implemented yet."));
            form.Add(ServerRule("Friendly Fire", "server-friendly-fire", false, "Combat has no friendly-fire policy yet."));
            form.Add(ServerRule("Allow Respawn (Cloning)", "server-respawn", false, "Cloning is not implemented yet."));
            Label status = new() { name = "server-settings-status", enableRichText = false };
            status.AddToClassList("lobby-shell__server-status");
            form.Add(status);
            RefreshServerSettings();
        }

        private void SaveServerSettings(ServerSetting field)
        {
            if (!_hasServerSettings) return;
            if (!_serverSettings.IsValid)
            {
                _serverSettingsStatus = "Player cap and minimum ready must be 1–256; round length must be 1–1440 minutes.";
                _tabContent.Q<Label>("server-settings-status").text = _serverSettingsStatus;
                return;
            }
            ServerSettingsChanged?.Invoke(field, _serverSettings);
        }

        private void AddRoundAction(VisualElement parent, string text, string name, RoundAdminAction action, bool secondary = false)
        {
            Button button = AdminButton(text, () => RoundAdminRequested?.Invoke(action));
            button.name = name;
            button.AddToClassList(secondary ? "lobby-shell__admin-btn--outline" : "lobby-shell__admin-btn--start");
            parent.Add(button);
        }

        private void AddServerNumber(VisualElement parent, string label, string name, ServerSetting field, Action<int> update)
        {
            VisualElement column = new();
            column.AddToClassList("lobby-shell__server-number");
            column.Add(new Label(label.ToUpperInvariant()));
            IntegerField input = new() { name = name, isDelayed = true };
            input.AddToClassList("font-terminal");
            input.RegisterValueChangedCallback(e => { update(e.newValue); SaveServerSettings(field); });
            input.RegisterCallback<FocusInEvent>(_ => _textEntry.Enter());
            input.RegisterCallback<FocusOutEvent>(_ => _textEntry.Exit());
            TrackEditing(input);
            column.Add(input);
            parent.Add(column);
        }

        /// <summary>Commits on Enter (name) or focus loss (both); editable in any round state.</summary>
        private void AddServerText(VisualElement parent, string label, string name, ServerSetting field, int maxLength,
            bool multiline, Action<string> update)
        {
            // Not isDelayed: multiline delayed fields do not reliably commit on blur, so commit explicitly.
            TextField input = new(label) { name = name, maxLength = maxLength, multiline = multiline };
            input.AddToClassList("lobby-shell__server-text");
            input.AddToClassList("font-body");
            input.EnableInClassList("lobby-shell__server-text--multiline", multiline);
            input.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (!_hasServerSettings || input.value == ServerTextValue(_serverSettings, field)) return;
                _pendingServerText[field] = (input.value, Time.unscaledTime + PendingServerTextSeconds);
                update(input.value);
                SaveServerSettings(field);
            });
            if (!multiline)
            {
                input.RegisterCallback<KeyDownEvent>(e =>
                {
                    if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter) input.Blur();
                }, TrickleDown.TrickleDown);
            }
            StyleDarkTextField(input);
            HoldTextEntry(input);
            TrackEditing(input);
            parent.Add(input);
        }

        /// <summary>
        /// Marks a field as being edited between focus in/out (both bubble from the inner text input), so
        /// the per-frame refresh never rewrites text under the caret. focusController.focusedElement was
        /// not reliable for this: rewriting mid-typing reset the text and broke the caret index.
        /// </summary>
        private void TrackEditing(VisualElement field)
        {
            field.RegisterCallback<FocusInEvent>(_ => _editingFields.Add(field));
            field.RegisterCallback<FocusOutEvent>(_ => _editingFields.Remove(field));
        }

        private static string ServerTextValue(ServerSettings settings, ServerSetting field) =>
            field == ServerSetting.ServerName ? settings.ServerName : settings.Motd;

        /// <summary>
        /// Keeps a committed name/MOTD over the periodic admin snapshot until the server echoes it,
        /// so a reply that was already in flight does not snap the field back to the old text.
        /// </summary>
        private void ApplyPendingServerText()
        {
            foreach (ServerSetting field in _pendingServerText.Keys.ToArray())
            {
                (string value, float expires) = _pendingServerText[field];
                string sanitized = field == ServerSetting.ServerName
                    ? ServerInfoText.SanitizeName(value) : ServerInfoText.SanitizeMotd(value);
                if (ServerTextValue(_serverSettings, field) == sanitized || Time.unscaledTime > expires)
                {
                    _pendingServerText.Remove(field);
                    continue;
                }
                if (field == ServerSetting.ServerName) _serverSettings.ServerName = value;
                else _serverSettings.Motd = value;
            }
        }

        private static Toggle ServerRule(string text, string name, bool supported, string reason)
        {
            Toggle toggle = new() { name = name, text = text, tooltip = reason };
            toggle.AddToClassList("lobby-shell__settings-toggle");
            toggle.SetValueWithoutNotify(name == "server-friendly-fire");
            toggle.SetEnabled(supported);
            return toggle;
        }

        private void RefreshServerSettings()
        {
            VisualElement form = _tabContent?.Q("server-settings-form");
            if (form == null) return;
            bool idle = _roundState is RoundState.Stopped or RoundState.Ended;
            bool warming = _roundState == RoundState.WarmingUp;
            form.Q<Label>("server-round-state").text = warming ? "STARTING IN" : _roundState switch
            {
                RoundState.Preparing => "LOADING STATION", RoundState.Ongoing => "ROUND IN PROGRESS",
                RoundState.Ending => "ENDING ROUND", _ => "WAITING FOR PLAYERS",
            };
            string detailText = "Please wait…";
            if (warming) detailText = $"{_secondsLeft / 60:00}:{_secondsLeft % 60:00}";
            else if (idle && _autoStartSeconds > 0) detailText = $"Auto start in {_secondsLeft / 60:00}:{_secondsLeft % 60:00}";
            else if (idle && !_autoStartEnabled) detailText = "Manual start";
            else if (idle && _autoStartHeld) detailText = "Auto start held. Start resumes it.";
            else if (idle) detailText = "Auto start when a player is ready";
            else if (_roundState == RoundState.Ongoing) detailText = $"{_secondsLeft / 60:00}:{_secondsLeft % 60:00} elapsed";
            else if (_roundState == RoundState.Ending) detailText = RoundSummaryModel.Countdown(_endSecondsLeft, _endHeld, shortForm: true);
            form.Q<Label>("server-round-detail").text = detailText;
            form.Q<Label>("server-ready-ratio").text = $"{_players.Count(p => p.Ready)}/{_players.Length}";
            Button start = form.Q<Button>("server-start");
            start.style.display = idle ? DisplayStyle.Flex : DisplayStyle.None;
            start.SetEnabled(idle && _hasServerSettings && _serverMaps.Contains(_serverSettings.Map));
            foreach (string name in new string[] { "server-force", "server-extend", "server-cancel" })
            {
                Button action = form.Q<Button>(name);
                action.style.display = idle || warming ? DisplayStyle.Flex : DisplayStyle.None;
                // Cancel also holds the idle auto-start countdown.
                action.SetEnabled(warming || (name == "server-cancel" && idle && _autoStartSeconds > 0));
            }
            Button stop = form.Q<Button>("server-stop");
            stop.style.display = _roundState == RoundState.Ongoing ? DisplayStyle.Flex : DisplayStyle.None;
            stop.SetEnabled(_roundState == RoundState.Ongoing);
            bool ending = _roundState == RoundState.Ending;
            Button holdEnd = form.Q<Button>("server-hold-end");
            holdEnd.style.display = ending && !_endHeld ? DisplayStyle.Flex : DisplayStyle.None;
            holdEnd.SetEnabled(ending && !_endHeld);
            Button resumeEnd = form.Q<Button>("server-resume-end");
            resumeEnd.style.display = ending && _endHeld ? DisplayStyle.Flex : DisplayStyle.None;
            resumeEnd.SetEnabled(ending && _endHeld);
            DropdownField map = form.Q<DropdownField>("server-map");
            map.choices = _serverMaps.ToList();
            map.SetValueWithoutNotify(_serverSettings.Map ?? "");
            form.Q<Label>("server-map-initial").text = string.IsNullOrEmpty(_serverSettings.Map)
                ? "M" : _serverSettings.Map.Substring(0, 1).ToUpperInvariant();
            map.SetEnabled(idle && _hasServerSettings);
            DropdownField mode = form.Q<DropdownField>("server-mode");
            mode.SetValueWithoutNotify(_serverSettings.Mode);
            mode.SetEnabled(idle && _hasServerSettings);
            IntegerField cap = form.Q<IntegerField>("server-player-cap");
            IntegerField length = form.Q<IntegerField>("server-round-length");
            // Do not replace text while an admin is typing in a delayed field.
            if (!_editingFields.Contains(cap))
                cap.SetValueWithoutNotify(_serverSettings.PlayerCap);
            if (!_editingFields.Contains(length))
                length.SetValueWithoutNotify(_serverSettings.RoundLengthMinutes);
            IntegerField minimumReady = form.Q<IntegerField>("server-minimum-ready");
            if (!_editingFields.Contains(minimumReady))
                minimumReady.SetValueWithoutNotify(_serverSettings.MinimumReady);
            IntegerField minimumOperatives = form.Q<IntegerField>("server-minimum-operatives");
            if (!_editingFields.Contains(minimumOperatives))
                minimumOperatives.SetValueWithoutNotify(_serverSettings.MinimumOperatives);
            cap.SetEnabled(idle && _hasServerSettings);
            length.SetEnabled(idle && _hasServerSettings);
            minimumReady.SetEnabled(idle && _hasServerSettings);
            minimumOperatives.SetEnabled(idle && _hasServerSettings);
            Toggle autoStart = form.Q<Toggle>("server-auto-start");
            autoStart.SetValueWithoutNotify(_serverSettings.AutoStart);
            autoStart.SetEnabled(_hasServerSettings);
            foreach ((string name, string value) in new (string, string)[] { ("server-name", _serverSettings.ServerName), ("server-motd", _serverSettings.Motd) })
            {
                TextField text = form.Q<TextField>(name);
                // Same rule as the number fields: never replace text the admin is editing.
                if (!_editingFields.Contains(text))
                    text.SetValueWithoutNotify(value ?? "");
                text.SetEnabled(_hasServerSettings);
            }
            Toggle late = form.Q<Toggle>("server-late-join");
            late.SetValueWithoutNotify(_serverSettings.AllowLateJoin);
            late.SetEnabled(idle && _hasServerSettings);
            form.Q<Label>("server-settings-status").text = _serverSettingsStatus;
        }
    }
}
