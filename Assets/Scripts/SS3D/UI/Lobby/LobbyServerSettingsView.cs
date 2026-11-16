using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Inputs;
using SS3D.Systems.Rounds;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The admin's Server Settings tab (round-config.md): round controls, server name and MOTD, map and mode, the
    /// numeric round fields and the rules that have a consumer in the game (lobby.md §1, §8). Built once; the host
    /// feeds it snapshots, and it never rewrites a field the admin is typing in.
    /// </summary>
    public sealed class LobbyServerSettingsView
    {
        private const float PendingServerTextSeconds = 5f;

        private readonly InputTextEntryScope _textEntry;
        private readonly Dictionary<ServerSetting, (string Value, float Expires)> _pendingServerText = new();
        private readonly HashSet<VisualElement> _editingFields = new();
        private readonly VisualElement _form;

        private ServerSettings _settings = ServerSettings.Default;
        private string[] _maps = Array.Empty<string>();
        private bool _hasSettings;
        private string _status = "Loading server settings…";
        private LobbySessionInfo _session;
        private int _ready;
        private int _online;
        private StationLoadState _stationLoadState;
        private string _stationName = "";

        public event Action<ServerSetting, ServerSettings> Changed;
        public event Action<RoundAdminAction> RoundAdminRequested;

        public VisualElement Root { get; }

        public LobbyServerSettingsView(InputTextEntryScope textEntry)
        {
            _textEntry = textEntry;
            ScrollView scroll = new() { name = "server-settings-scroll" };
            scroll.AddToClassList("lobby-admin__scroll");
            Root = scroll;
            _form = LobbyUi.Box("lobby-admin");
            _form.name = "server-settings-form";
            scroll.Add(_form);
            Build();
            Refresh();
        }

        private void Build()
        {
            VisualElement card = LobbyUi.Box("lobby-admin__card");
            card.Add(LobbyUi.Titling("Round state", "lobby-admin__card-heading"));
            VisualElement body = LobbyUi.Box("lobby-admin__card-body");
            VisualElement state = new();
            Label title = LobbyUi.Titling("", "lobby-admin__round-state");
            title.name = "server-round-state";
            state.Add(title);
            Label detail = LobbyUi.Mono("", "lobby-admin__round-detail");
            detail.name = "server-round-detail";
            state.Add(detail);
            body.Add(state);
            VisualElement ready = new();
            ready.Add(LobbyUi.Text("READY", "lobby-admin__ready-label"));
            Label ratio = LobbyUi.Mono("", "lobby-admin__ready-ratio");
            ratio.name = "server-ready-ratio";
            ready.Add(ratio);
            body.Add(ready);
            card.Add(body);
            _form.Add(card);

            _form.Add(LobbyUi.Titling("Round controls", "lobby-admin__section-title"));
            VisualElement controls = LobbyUi.Box("lobby-admin__controls");
            AddRoundAction(controls, "Start Round", "server-start", RoundAdminAction.Start, true);
            AddRoundAction(controls, "Force Start", "server-force", RoundAdminAction.ForceStart);
            // Only offered after a start found the map unplayable (creative-mode §8's block, with a way back into the editor).
            AddRoundAction(controls, "Edit Map Anyway", "server-edit-anyway", RoundAdminAction.EditMapAnyway);
            AddRoundAction(controls, "Extend Timer", "server-extend", RoundAdminAction.ExtendTimer);
            AddRoundAction(controls, "Cancel Countdown", "server-cancel", RoundAdminAction.CancelCountdown);
            AddRoundAction(controls, "Stop Round", "server-stop", RoundAdminAction.Stop);
            // The 30 s round-end summary: hold it for a discussion, resume to return everyone to the lobby.
            AddRoundAction(controls, "Hold Summary", "server-hold-end", RoundAdminAction.HoldEnd);
            AddRoundAction(controls, "Resume Summary", "server-resume-end", RoundAdminAction.ResumeEnd);
            _form.Add(controls);

            _form.Add(LobbyUi.Titling("Server info", "lobby-admin__section-title"));
            AddServerText("Server Name", "server-name", ServerSetting.ServerName, ServerInfoText.MaxNameLength, false,
                value => _settings.ServerName = value);
            AddServerText("Message of the Day", "server-motd", ServerSetting.Motd, ServerInfoText.MaxMotdLength, true,
                value => _settings.Motd = value);

            _form.Add(LobbyUi.Titling("Map and mode", "lobby-admin__section-title"));
            DropdownField map = new("Map") { name = "server-map" };
            map.AddToClassList("lobby-admin__select");
            map.RegisterValueChangedCallback(e =>
            {
                if (e.target != map) return;
                _settings.Map = e.newValue;
                Save(ServerSetting.Map);
            });
            _form.Add(map);
            Label mapStatus = LobbyUi.Text("", "lobby-admin__map-load");
            mapStatus.name = "server-map-load-status";
            _form.Add(mapStatus);
            DropdownField mode = new("Game Mode", new List<string> { "Secret", "Extended", "Nuclear Emergency" }, 0)
                { name = "server-mode" };
            mode.AddToClassList("lobby-admin__select");
            mode.RegisterValueChangedCallback(e =>
            {
                if (e.target != mode) return;
                _settings.Mode = e.newValue;
                Save(ServerSetting.Mode);
            });
            _form.Add(mode);

            VisualElement numbers = LobbyUi.Box("lobby-admin__numbers");
            AddNumber(numbers, "Player Cap", "server-player-cap", ServerSetting.PlayerCap, v => _settings.PlayerCap = v);
            AddNumber(numbers, "Round Length (min)", "server-round-length", ServerSetting.RoundLengthMinutes, v => _settings.RoundLengthMinutes = v);
            AddNumber(numbers, "Minimum Connected", "server-minimum-connected", ServerSetting.MinimumConnected, v => _settings.MinimumConnected = v);
            AddNumber(numbers, "Countdown (s)", "server-countdown", ServerSetting.CountdownSeconds, v => _settings.CountdownSeconds = v);
            AddNumber(numbers, "Minimum Operatives", "server-minimum-operatives", ServerSetting.MinimumOperatives, v => _settings.MinimumOperatives = v);
            _form.Add(numbers);

            // Only rules with a consumer in the game (lobby.md §8): Random Events, Friendly Fire and Respawn come back
            // when their systems exist.
            _form.Add(LobbyUi.Titling("Round rules", "lobby-admin__section-title"));
            Toggle late = Rule("Allow Late Join", "server-late-join", "");
            late.RegisterValueChangedCallback(e => { _settings.AllowLateJoin = e.newValue; Save(ServerSetting.AllowLateJoin); });
            _form.Add(late);
            Toggle autoStart = Rule("Auto Start Rounds", "server-auto-start",
                "Start a countdown on its own once enough players are connected, and again after every round.");
            autoStart.RegisterValueChangedCallback(e => { _settings.AutoStart = e.newValue; Save(ServerSetting.AutoStart); });
            _form.Add(autoStart);

            Label status = LobbyUi.Text("", "lobby-admin__status");
            status.name = "server-settings-status";
            _form.Add(status);
        }

        public void SetServerSettings(ServerSettings settings, string[] maps, string status)
        {
            _settings = settings;
            ApplyPendingServerText();
            _maps = maps ?? Array.Empty<string>();
            _hasSettings = true;
            _status = status ?? "";
            Refresh();
        }

        public void SetSession(in LobbySessionInfo session, int ready, int online)
        {
            _session = session;
            _ready = ready;
            _online = online;
            Refresh();
        }

        public void SetStationLoadState(StationLoadState state, string stationName)
        {
            _stationLoadState = state;
            _stationName = stationName ?? "";
            Refresh();
        }

        private void Save(ServerSetting field)
        {
            if (!_hasSettings) return;
            if (!_settings.IsValid)
            {
                _status = "Player cap and minimum ready must be 1–256; round length must be 1–1440 minutes.";
                _form.Q<Label>("server-settings-status").text = _status;
                return;
            }

            Changed?.Invoke(field, _settings);
        }

        private void AddRoundAction(VisualElement parent, string text, string name, RoundAdminAction action, bool primary = false)
        {
            Button button = new(() => RoundAdminRequested?.Invoke(action)) { name = name, text = text.ToUpperInvariant() };
            button.AddToClassList("lobby-admin__button");
            button.AddToClassList("lb-titling");
            if (primary) button.AddToClassList("lobby-admin__button--start");
            parent.Add(button);
        }

        private void AddNumber(VisualElement parent, string label, string name, ServerSetting field, Action<int> update)
        {
            VisualElement column = LobbyUi.Box("lobby-admin__number");
            column.Add(LobbyUi.Text(label, "lobby-admin__number-label"));
            IntegerField input = new() { name = name, isDelayed = true };
            input.AddToClassList("lb-mono");
            input.RegisterValueChangedCallback(e => { update(e.newValue); Save(field); });
            LobbyUi.HoldTextEntry(input, _textEntry);
            TrackEditing(input);
            column.Add(input);
            parent.Add(column);
        }

        /// <summary>Commits on Enter (name) or focus loss (both); editable in any round state.</summary>
        private void AddServerText(string label, string name, ServerSetting field, int maxLength, bool multiline,
            Action<string> update)
        {
            // Not isDelayed: multiline delayed fields do not reliably commit on blur, so commit explicitly.
            TextField input = new(label) { name = name, maxLength = maxLength, multiline = multiline };
            input.AddToClassList("lobby-admin__text");
            input.EnableInClassList("lobby-admin__text--multiline", multiline);
            input.RegisterCallback<FocusOutEvent>(_ =>
            {
                if (!_hasSettings || input.value == TextValue(_settings, field)) return;
                _pendingServerText[field] = (input.value, Time.unscaledTime + PendingServerTextSeconds);
                update(input.value);
                Save(field);
            });
            if (!multiline)
                input.RegisterCallback<KeyDownEvent>(e =>
                {
                    if (e.keyCode is KeyCode.Return or KeyCode.KeypadEnter) input.Blur();
                }, TrickleDown.TrickleDown);
            LobbyUi.DarkInput(input);
            LobbyUi.HoldTextEntry(input, _textEntry);
            TrackEditing(input);
            _form.Add(input);
        }

        /// <summary>
        /// Marks a field as being edited between focus in and out, so the periodic refresh never rewrites text under
        /// the caret (rewriting mid-typing reset the text and broke the caret index).
        /// </summary>
        private void TrackEditing(VisualElement field)
        {
            field.RegisterCallback<FocusInEvent>(_ => _editingFields.Add(field));
            field.RegisterCallback<FocusOutEvent>(_ => _editingFields.Remove(field));
        }

        private static string TextValue(ServerSettings settings, ServerSetting field) =>
            field == ServerSetting.ServerName ? settings.ServerName : settings.Motd;

        /// <summary>
        /// Keeps a committed name or MOTD over the periodic admin snapshot until the server echoes it, so a reply that
        /// was already in flight does not snap the field back to the old text.
        /// </summary>
        private void ApplyPendingServerText()
        {
            foreach (ServerSetting field in _pendingServerText.Keys.ToArray())
            {
                (string value, float expires) = _pendingServerText[field];
                string sanitized = field == ServerSetting.ServerName
                    ? ServerInfoText.SanitizeName(value) : ServerInfoText.SanitizeMotd(value);
                if (TextValue(_settings, field) == sanitized || Time.unscaledTime > expires)
                {
                    _pendingServerText.Remove(field);
                    continue;
                }

                if (field == ServerSetting.ServerName) _settings.ServerName = value;
                else _settings.Motd = value;
            }
        }

        private static Toggle Rule(string text, string name, string reason)
        {
            Toggle toggle = new() { name = name, text = text, tooltip = reason };
            toggle.AddToClassList("lobby-admin__toggle");
            return toggle;
        }

        private void Refresh()
        {
            RoundState round = _session.State;
            bool idle = round is RoundState.Stopped or RoundState.Ended;
            bool warming = round == RoundState.WarmingUp;
            int countdown = idle ? _session.AutoStartSecondsLeft : _session.TimerSeconds;
            _form.Q<Label>("server-round-state").text = (warming ? "Starting in" : round switch
            {
                RoundState.Preparing => "Loading station",
                RoundState.Ongoing => "Round in progress",
                RoundState.Ending => "Ending round",
                _ => "Waiting for players",
            }).ToUpperInvariant();
            string detail = "Please wait…";
            if (warming) detail = LobbySidebarModel.Clock(countdown);
            else if (idle && _session.AutoStartSecondsLeft > 0) detail = $"Auto start in {LobbySidebarModel.Clock(countdown)}";
            else if (idle && !_session.AutoStartEnabled) detail = "Manual start";
            else if (idle && _session.AutoStartHeld) detail = "Auto start held. Start resumes it.";
            else if (idle) detail = $"Auto start at {Math.Max(1, _session.MinimumConnected)} connected";
            else if (round == RoundState.Ongoing) detail = $"{LobbySidebarModel.LongClock(_session.TimerSeconds)} elapsed";
            else if (round == RoundState.Ending) detail = RoundSummaryModel.Countdown(_session.EndSecondsLeft, _session.EndHeld, shortForm: true);
            _form.Q<Label>("server-round-detail").text = detail;
            _form.Q<Label>("server-ready-ratio").text = $"{_ready}/{_online}";

            Button start = _form.Q<Button>("server-start");
            start.style.display = idle ? DisplayStyle.Flex : DisplayStyle.None;
            start.SetEnabled(idle && _hasSettings && _maps.Contains(_settings.Map));
            Button editAnyway = _form.Q<Button>("server-edit-anyway");
            bool unplayable = idle && _hasSettings && _stationLoadState == StationLoadState.Unplayable;
            editAnyway.style.display = unplayable ? DisplayStyle.Flex : DisplayStyle.None;
            editAnyway.SetEnabled(unplayable);
            foreach (string name in new string[] { "server-force", "server-extend", "server-cancel" })
            {
                Button action = _form.Q<Button>(name);
                action.style.display = idle || warming ? DisplayStyle.Flex : DisplayStyle.None;
                // Cancel also holds the idle auto-start countdown.
                action.SetEnabled(warming || (name == "server-cancel" && idle && _session.AutoStartSecondsLeft > 0));
            }

            Button stop = _form.Q<Button>("server-stop");
            stop.style.display = round == RoundState.Ongoing ? DisplayStyle.Flex : DisplayStyle.None;
            stop.SetEnabled(round == RoundState.Ongoing);
            bool ending = round == RoundState.Ending;
            Button holdEnd = _form.Q<Button>("server-hold-end");
            holdEnd.style.display = ending && !_session.EndHeld ? DisplayStyle.Flex : DisplayStyle.None;
            holdEnd.SetEnabled(ending && !_session.EndHeld);
            Button resumeEnd = _form.Q<Button>("server-resume-end");
            resumeEnd.style.display = ending && _session.EndHeld ? DisplayStyle.Flex : DisplayStyle.None;
            resumeEnd.SetEnabled(ending && _session.EndHeld);

            DropdownField map = _form.Q<DropdownField>("server-map");
            map.choices = _maps.ToList();
            map.SetValueWithoutNotify(_settings.Map ?? "");
            map.SetEnabled(idle && _hasSettings);
            DropdownField mode = _form.Q<DropdownField>("server-mode");
            mode.SetValueWithoutNotify(_settings.Mode);
            mode.SetEnabled(idle && _hasSettings);

            SetNumber("server-player-cap", _settings.PlayerCap, idle);
            SetNumber("server-round-length", _settings.RoundLengthMinutes, idle);
            SetNumber("server-minimum-connected", _settings.MinimumConnected, idle);
            SetNumber("server-countdown", _settings.CountdownSeconds, idle);
            SetNumber("server-minimum-operatives", _settings.MinimumOperatives, idle);

            Toggle autoStart = _form.Q<Toggle>("server-auto-start");
            autoStart.SetValueWithoutNotify(_settings.AutoStart);
            autoStart.SetEnabled(_hasSettings);
            foreach ((string name, string value) in new (string, string)[] { ("server-name", _settings.ServerName), ("server-motd", _settings.Motd) })
            {
                TextField text = _form.Q<TextField>(name);
                // Same rule as the number fields: never replace text the admin is editing.
                if (!_editingFields.Contains(text)) text.SetValueWithoutNotify(value ?? "");
                text.SetEnabled(_hasSettings);
            }

            Toggle late = _form.Q<Toggle>("server-late-join");
            late.SetValueWithoutNotify(_settings.AllowLateJoin);
            late.SetEnabled(idle && _hasSettings);
            _form.Q<Label>("server-settings-status").text = _status;
            UpdateMapStatus(_form.Q<Label>("server-map-load-status"));
        }

        private void SetNumber(string name, int value, bool idle)
        {
            IntegerField field = _form.Q<IntegerField>(name);
            // Do not replace text while an admin is typing in a delayed field.
            if (!_editingFields.Contains(field)) field.SetValueWithoutNotify(value);
            field.SetEnabled(idle && _hasSettings);
        }

        /// <summary>The line under the map picker spells out why a map can't start.</summary>
        private void UpdateMapStatus(Label label)
        {
            label.text = _stationLoadState switch
            {
                StationLoadState.Loading => $"Loading map: {_stationName}…",
                StationLoadState.Initializing => $"Initializing station: {_stationName}…",
                StationLoadState.Ready => $"Map ready: {_stationName}",
                StationLoadState.Failed => "Map load failed — select a map and retry",
                StationLoadState.Unplayable when !string.IsNullOrEmpty(_status) =>
                    _status + "\nEdit Map Anyway starts an Extended round on it so you can fix it in the Map Editor.",
                StationLoadState.Unplayable => $"{_stationName} can't start a round yet",
                _ => "No map loaded",
            };
            label.EnableInClassList("lobby-admin__map-load--loading",
                _stationLoadState is StationLoadState.Loading or StationLoadState.Initializing);
            label.EnableInClassList("lobby-admin__map-load--ready", _stationLoadState == StationLoadState.Ready);
            label.EnableInClassList("lobby-admin__map-load--failed",
                _stationLoadState is StationLoadState.Failed or StationLoadState.Unplayable);
        }
    }
}
