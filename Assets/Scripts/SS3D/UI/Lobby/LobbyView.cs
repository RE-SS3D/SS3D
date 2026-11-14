using System;
using System.Collections.Generic;
using SS3D.Systems.Comms;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inputs;
using SS3D.Systems.Rounds;
using SS3D.UI.Shell;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// The lobby shell (lobby.md §8, lobby effort Phase 4): the sidebar with the round state, the selected character,
    /// Ready / Join, Observe, the music row and the exits; the header strip; and the tabs Server Info, Chat, Settings,
    /// Last Round and, for admins, Server Settings. Built from <c>Lobby.uxml</c>; it is also the in-round Escape
    /// menu. <see cref="LobbyUiSubSystem"/> supplies every snapshot and acts on every event.
    /// </summary>
    public sealed class LobbyView : IUiSurface
    {
        public const string TabServerInfo = "server-info";
        public const string TabChat = "chat";
        public const string TabSettings = "settings";
        public const string TabLastRound = "last-round";
        public const string TabServerSettings = "server-settings";

        /// <summary>Below this panel width the shell takes the 1366 × 768 layout (Decision 19).</summary>
        public const float CompactWidth = 1600f;

        private const long QuitArmMilliseconds = 3000;

        private static readonly string[] TabOrder = { TabServerInfo, TabChat, TabSettings, TabLastRound, TabServerSettings };

        private readonly LobbyAssetCatalog _catalog;
        private readonly InputTextEntryScope _textEntry = new();
        private readonly Dictionary<string, Button> _tabs = new();

        private VisualElement _root;
        private VisualElement _tabContent;
        private LobbyServerInfoView _serverInfo;
        private LobbyChatView _chat;
        private LobbyServerSettingsView _serverSettings;
        private LobbyLastRoundView _lastRound;
        private LobbyMapStopPrompt _mapStop;
        private VisualElement _settingsHost;

        private VisualElement _roundSection;
        private VisualElement _roundDot;
        private Label _roundStatus;
        private Label _roundCaption;
        private VisualElement _timerWrap;
        private Label _timer;
        private VisualElement _roundNote;
        private Label _roundNoteText;
        private Label _mapLoad;
        private Label _characterName;
        private VisualElement _highSwatch;
        private Label _highPrefix;
        private Label _highJob;
        private Label _characterCount;
        private VisualElement _previewImage;
        private Button _ready;
        private Label _readyTitle;
        private Label _readySubtitle;
        private VisualElement _readyNote;
        private Label _readyNoteText;
        private Button _observe;
        private Label _observeTitle;
        private Label _observeSubtitle;
        private VisualElement _music;
        private Button _musicToggle;
        private Label _musicTitle;
        private Slider _musicVolume;
        private bool _musicDragging;
        private Button _quit;
        private Label _quitLabel;
        private Label _serverName;
        private Label _headerMap;
        private Label _headerMode;
        private Label _headerPlayers;
        private VisualElement _headerRoundDot;
        private Label _headerRound;
        private Button _backToRound;
        private VisualElement _admin;
        private Button _adminSwitch;

        private string _activeTab = TabServerInfo;
        private bool _isAdmin;
        private bool _adminActive;
        private bool _quitArmed;
        private IVisualElementScheduledItem _quitDisarm;
        private LobbySessionInfo _session;
        private int _lastCountdownSeconds;
        private StationLoadState _stationLoadState;
        private string _stationName = "";
        private Texture _previewTexture;
        private int _readyCount;
        private int _onlineCount;

        public event Action CharacterCreatorRequested;
        /// <summary>"Switch character". The Phase 5 switcher pops over here; until then it opens the character window.</summary>
        public event Action SwitchCharacterRequested;
        /// <summary>Ready before the round (a toggle), Join during it.</summary>
        public event Action ReadyRequested;
        /// <summary>Observe: a ghost at once, before or during the round (Decision 9).</summary>
        public event Action ObserveRequested;
        public event Action QuitRequested;
        /// <summary>"Report a bug": the host opens the bug report dialog (<c>sandbox-server.md</c> §7).</summary>
        public event Action BugReportRequested;
        /// <summary>"Back to round" in the in-round Escape menu.</summary>
        public event Action BackToRoundRequested;
        public event Action MusicToggleRequested;
        public event Action MusicNextRequested;
        public event Action<float> MusicVolumeChanged;
        public event Action<OocChannel, string> ChatSubmitted;
        public event Action<ServerSetting, ServerSettings> ServerSettingsChanged;
        public event Action<RoundAdminAction> RoundAdminRequested;
        public event Action<int, MapStopChoice, string> MapStopResolved;
        /// <summary>The shell was hidden; the Settings screen stops listening for a key.</summary>
        public event Action Hidden;

        public LobbyView(LobbyAssetCatalog catalog)
        {
            _catalog = catalog;
        }

        /// <summary>The shell's own root, which covers the screen: the Settings prompt sits on it.</summary>
        public VisualElement Root => _root;

        /// <summary>
        /// The element the shared Settings screen fills (LobbyUiSubSystem mounts it). The Settings tab re-parents it,
        /// so the screen keeps its sub-tab and state across tab switches.
        /// </summary>
        public VisualElement SettingsHost => _settingsHost ??= CreateSettingsHost();

        public bool IsVisible => _root != null && _root.resolvedStyle.display != DisplayStyle.None;

        public string ActiveTab => _activeTab;

        public bool HasLastRound => _lastRound?.Model != null;

        private static VisualElement CreateSettingsHost()
        {
            VisualElement host = new() { name = "lobby-settings-host" };
            host.AddToClassList("lobby__settings-host");
            return host;
        }

        public void Attach(VisualElement layerRoot)
        {
            VisualTreeAsset template = _catalog != null ? _catalog.LobbyTemplate : null;
            if (template == null)
            {
                Debug.LogError("[Lobby] LobbyAssetCatalog has no lobby template. Run SS3D → Data → Rebuild All UI Catalogs.");
                return;
            }

            TemplateContainer tree = template.CloneTree();
            _root = tree.Q("lobby-shell-surface");
            _root.RemoveFromHierarchy();
            _root.pickingMode = PickingMode.Position;
            BindSidebar();
            BindMain();

            _serverInfo = new LobbyServerInfoView();
            _chat = new LobbyChatView(_textEntry);
            _chat.Submitted += (channel, text) => ChatSubmitted?.Invoke(channel, text);
            _serverSettings = new LobbyServerSettingsView(_textEntry);
            _serverSettings.Changed += (field, settings) => ServerSettingsChanged?.Invoke(field, settings);
            _serverSettings.RoundAdminRequested += action => RoundAdminRequested?.Invoke(action);
            _lastRound = new LobbyLastRoundView();
            _mapStop = new LobbyMapStopPrompt(_textEntry);
            _mapStop.Resolved += (token, choice, name) => MapStopResolved?.Invoke(token, choice, name);
            _root.Add(_mapStop.Root);

            _root.RegisterCallback<GeometryChangedEvent>(e =>
                _root.EnableInClassList("lobby--compact", e.newRect.width > 0f && e.newRect.width < CompactWidth));
            _root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            layerRoot.Add(_root);
            RefreshTabs();
            ShowTab(_activeTab);
            ApplyPreviewTexture();
            SetVisible(true);
        }

        public void Detach()
        {
            CharacterCreatorRequested = null;
            SwitchCharacterRequested = null;
            ReadyRequested = null;
            ObserveRequested = null;
            QuitRequested = null;
            BugReportRequested = null;
            BackToRoundRequested = null;
            MusicToggleRequested = null;
            MusicNextRequested = null;
            MusicVolumeChanged = null;
            ChatSubmitted = null;
            ServerSettingsChanged = null;
            RoundAdminRequested = null;
            MapStopResolved = null;
            Hidden = null;
            _mapStop?.Reset();
            _settingsHost?.RemoveFromHierarchy();
            _textEntry.Exit();
            _root?.RemoveFromHierarchy();
            _root = null;
        }

        public void SetVisible(bool visible)
        {
            if (_root == null) return;
            bool wasVisible = _root.style.display != DisplayStyle.None;
            if (!visible)
            {
                _textEntry.Exit();
                Disarm();
                Hidden?.Invoke();
            }

            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
            // Focus starts on Ready (or Observe when Ready can't be pressed), per the canvas annotations.
            if (visible && !wasVisible) _root.schedule.Execute(FocusEntry).ExecuteLater(1);
        }

        private void FocusEntry()
        {
            if (_root == null || _mapStop.IsOpen) return;
            if (_ready.enabledSelf) _ready.Focus();
            else if (_observe.enabledSelf) _observe.Focus();
        }

        // ---- Binding ----

        private void BindSidebar()
        {
            _roundSection = _root.Q("lobby-round");
            _roundDot = _root.Q("lobby-round-dot");
            _roundStatus = _root.Q<Label>("lobby-round-status");
            _roundCaption = _root.Q<Label>("lobby-round-caption");
            _timerWrap = _root.Q("lobby-round-timer-wrap");
            _timer = _root.Q<Label>("lobby-round-timer");
            _roundNote = _root.Q("lobby-round-note");
            _roundNoteText = _root.Q<Label>("lobby-round-note-text");
            _mapLoad = _root.Q<Label>("lobby-map-load-status");

            _root.Q<Button>("lobby-character").clicked += () => CharacterCreatorRequested?.Invoke();
            _root.Q<Button>("lobby-switch-character").clicked += () => SwitchCharacterRequested?.Invoke();
            _characterName = _root.Q<Label>("lobby-character-name");
            _highSwatch = _root.Q("lobby-high-swatch");
            _highPrefix = _root.Q<Label>("lobby-high-prefix");
            _highJob = _root.Q<Label>("lobby-high-job");
            _characterCount = _root.Q<Label>("lobby-character-count");
            _previewImage = _root.Q("lobby-preview-image");

            _ready = _root.Q<Button>("lobby-ready");
            _ready.clicked += () => ReadyRequested?.Invoke();
            _readyTitle = _root.Q<Label>("lobby-ready-title");
            _readySubtitle = _root.Q<Label>("lobby-ready-subtitle");
            _readyNote = _root.Q("lobby-ready-note");
            _readyNoteText = _root.Q<Label>("lobby-ready-note-text");
            _observe = _root.Q<Button>("lobby-observe");
            _observe.clicked += () => ObserveRequested?.Invoke();
            _observeTitle = _root.Q<Label>("lobby-observe-title");
            _observeSubtitle = _root.Q<Label>("lobby-observe-subtitle");

            _music = _root.Q("lobby-music");
            _musicToggle = _root.Q<Button>("lobby-music-toggle");
            _musicToggle.clicked += () => MusicToggleRequested?.Invoke();
            _musicTitle = _root.Q<Label>("lobby-music-title");
            _root.Q<Button>("lobby-music-next").clicked += () => MusicNextRequested?.Invoke();
            _musicVolume = _root.Q<Slider>("lobby-music-volume");
            _musicVolume.RegisterValueChangedCallback(e => MusicVolumeChanged?.Invoke(e.newValue));
            _musicVolume.RegisterCallback<PointerDownEvent>(_ => _musicDragging = true, TrickleDown.TrickleDown);
            _musicVolume.RegisterCallback<PointerUpEvent>(_ => _musicDragging = false, TrickleDown.TrickleDown);
            _musicVolume.RegisterCallback<PointerCaptureOutEvent>(_ => _musicDragging = false);

            _root.Q<Button>("lobby-report-bug").clicked += () => BugReportRequested?.Invoke();
            _quit = _root.Q<Button>("lobby-quit");
            _quitLabel = _root.Q<Label>("lobby-quit-label");
            _quit.clicked += OnQuitClicked;
            _quit.RegisterCallback<FocusOutEvent>(_ => Disarm());
            _quit.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Escape || !_quitArmed) return;
                Disarm();
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
        }

        private void BindMain()
        {
            _serverName = _root.Q<Label>("lobby-server-name");
            _headerMap = _root.Q<Label>("lobby-header-map");
            _headerMode = _root.Q<Label>("lobby-header-mode");
            _headerPlayers = _root.Q<Label>("lobby-header-players");
            _headerRoundDot = _root.Q("lobby-header-round-dot");
            _headerRound = _root.Q<Label>("lobby-header-round");
            _backToRound = _root.Q<Button>("lobby-back-to-round");
            _backToRound.clicked += () => BackToRoundRequested?.Invoke();
            _tabContent = _root.Q("lobby-tab-content");
            foreach (string key in TabOrder)
            {
                Button tab = _root.Q<Button>("tab-" + key);
                string captured = key;
                tab.clicked += () => SelectTab(captured);
                _tabs[key] = tab;
            }

            _admin = _root.Q("lobby-admin");
            _adminSwitch = _root.Q<Button>("lobby-admin-switch");
            _adminSwitch.clicked += ToggleAdmin;
            SetServerInfo(null, "", null, "", null);
        }

        // ---- Quit: two clicks within 3 s ----

        private void OnQuitClicked()
        {
            if (_quitArmed)
            {
                Disarm();
                QuitRequested?.Invoke();
                return;
            }

            _quitArmed = true;
            _quitLabel.text = "CLICK AGAIN TO QUIT";
            _quit.AddToClassList("lobby__quit--armed");
            _quitDisarm?.Pause();
            _quitDisarm = _quit.schedule.Execute(Disarm);
            _quitDisarm.ExecuteLater(QuitArmMilliseconds);
        }

        private void Disarm()
        {
            if (!_quitArmed || _quit == null) return;
            _quitArmed = false;
            _quitDisarm?.Pause();
            _quitLabel.text = "QUIT TO MENU";
            _quit.RemoveFromClassList("lobby__quit--armed");
        }

        // ---- Keys ----

        private void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Tab || !e.ctrlKey || _mapStop.IsOpen) return;
            int index = Array.IndexOf(TabOrder, _activeTab);
            for (int step = 1; step <= TabOrder.Length; step++)
            {
                string next = TabOrder[(index + (e.shiftKey ? -step : step) % TabOrder.Length + TabOrder.Length) % TabOrder.Length];
                if (!TabShown(next)) continue;
                SelectTab(next);
                _tabs[next].Focus();
                break;
            }

            e.StopPropagation();
        }

        // ---- Tabs ----

        private bool TabShown(string key) => key switch
        {
            TabLastRound => HasLastRound,
            TabServerSettings => _isAdmin && _adminActive,
            _ => true,
        };

        public void SelectTab(string key)
        {
            if (!TabShown(key)) key = TabServerInfo;
            _activeTab = key;
            RefreshTabs();
            ShowTab(key);
        }

        private void RefreshTabs()
        {
            if (_root == null) return;
            foreach ((string key, Button tab) in _tabs)
            {
                tab.EnableInClassList("lobby__tab--hidden", !TabShown(key));
                tab.EnableInClassList("lobby__tab--active", key == _activeTab);
            }

            _admin.EnableInClassList("lobby__admin--hidden", !_isAdmin);
            _adminSwitch.EnableInClassList("lobby__switch-toggle--on", _adminActive);
        }

        private void ShowTab(string key)
        {
            if (_tabContent == null) return;
            // A focused field removed from the panel gets no FocusOut.
            _textEntry.Exit();
            _tabContent.Clear();
            _tabContent.Add(key switch
            {
                TabChat => _chat.Root,
                TabSettings => SettingsHost,
                TabLastRound => _lastRound.Root,
                TabServerSettings => _serverSettings.Root,
                _ => _serverInfo.Root,
            });
        }

        private void ToggleAdmin()
        {
            _adminActive = !_adminActive;
            // On adds Server Settings and opens it; off falls back to Server Info if it was open.
            if (_adminActive) _activeTab = TabServerSettings;
            else if (_activeTab == TabServerSettings) _activeTab = TabServerInfo;
            RefreshTabs();
            ShowTab(_activeTab);
        }

        // ---- Snapshots from the host ----

        public void SetSession(in LobbySessionInfo info, bool admin)
        {
            if (_root == null) return;
            _session = info;
            if (info.AutoStartSecondsLeft > 0) _lastCountdownSeconds = info.AutoStartSecondsLeft;
            if (admin != _isAdmin)
            {
                _isAdmin = admin;
                if (!admin) _adminActive = false;
                if (!TabShown(_activeTab)) _activeTab = TabServerInfo;
                RefreshTabs();
                ShowTab(_activeTab);
            }

            LobbyRoundStatus round = LobbySidebarModel.Round(info, _lastCountdownSeconds);
            Tone(_roundDot, "lobby__dot", round.Tone);
            _roundStatus.text = round.Status;
            Tone(_roundStatus, "lobby__status", round.Tone);
            _roundCaption.text = round.Caption;
            _roundSection.EnableInClassList("lobby__round--waiting", round.Timer.Length == 0);
            _timer.text = round.Timer;
            _timerWrap.style.display = round.Timer.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _timerWrap.EnableInClassList("lobby__timer-wrap--held", round.Held);
            _roundNoteText.text = round.Note;
            _roundNote.style.display = round.Note.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _roundNote.EnableInClassList("lobby__note--warning", round.NoteTone == LobbyTone.Warning);
            _roundNote.Q("lobby-round-note-icon").style.display = round.Held ? DisplayStyle.Flex : DisplayStyle.None;

            _headerRound.text = round.Status;
            Tone(_headerRoundDot, "lobby__dot", round.Tone);

            LobbyEntryButton entry = LobbySidebarModel.Entry(info);
            _readyTitle.text = entry.Title.ToUpperInvariant();
            _readySubtitle.text = entry.Subtitle;
            _ready.SetEnabled(entry.Enabled);
            _ready.EnableInClassList("lobby__ready--confirmed", entry.Look == LobbyButtonLook.Confirmed);
            _readyNoteText.text = entry.Note;
            _readyNote.EnableInClassList("lobby__entry-note--shown", entry.Note.Length > 0);

            LobbyEntryButton observe = LobbySidebarModel.Observe(info);
            _observeTitle.text = observe.Title.ToUpperInvariant();
            _observeSubtitle.text = observe.Subtitle;
            _observe.SetEnabled(observe.Enabled);

            _backToRound.EnableInClassList("lobby__back--shown", info.Spawned);
            _serverSettings.SetSession(info, _readyCount, _onlineCount);
            _serverInfo.RefreshUptime();
        }

        private static void Tone(VisualElement element, string block, LobbyTone tone)
        {
            element.EnableInClassList(block + "--live", tone == LobbyTone.Live);
            element.EnableInClassList(block + "--warning", tone == LobbyTone.Warning);
        }

        /// <summary>Online and ready players, and the server's player cap.</summary>
        public void SetPlayers(int online, int ready, int cap)
        {
            _onlineCount = online;
            _readyCount = ready;
            if (_headerPlayers != null) _headerPlayers.text = cap > 0 ? $"{online}/{cap}" : online.ToString();
        }

        public void SetServerInfo(string name, string motd, string mode, string build, DateTimeOffset? started)
        {
            if (_root == null) return;
            _serverName.text = (string.IsNullOrEmpty(name) ? "Connecting…" : name).ToUpperInvariant();
            _headerMode.text = string.IsNullOrEmpty(mode) ? "—" : mode;
            _serverInfo?.SetServer(motd, mode, build, started);
        }

        public void SetRoundId(int roundId) => _serverInfo?.SetRound(roundId);

        public void SetStationLoadState(StationLoadState state, string stationName)
        {
            if (_root == null) return;
            _stationLoadState = state;
            _stationName = stationName ?? "";
            _headerMap.text = string.IsNullOrEmpty(_stationName) ? "No map" : _stationName;
            _serverInfo.SetMap(_stationName);
            _serverSettings.SetStationLoadState(state, _stationName);
            // The sidebar mentions the map only while it is loading or can't be played.
            _mapLoad.text = state switch
            {
                StationLoadState.Loading => $"Loading map: {_stationName}…",
                StationLoadState.Initializing => $"Initializing station: {_stationName}…",
                StationLoadState.Failed => "The map failed to load",
                StationLoadState.Unplayable => $"{_stationName} can't start a round yet",
                _ => "",
            };
            _mapLoad.style.display = _mapLoad.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            _mapLoad.EnableInClassList("lobby__map-load--loading", state is StationLoadState.Loading or StationLoadState.Initializing);
            _mapLoad.EnableInClassList("lobby__map-load--failed", state is StationLoadState.Failed or StationLoadState.Unplayable);
        }

        /// <summary>
        /// The selected character: its name, its High job and that job's department (none when nothing is High), and
        /// how many characters the player keeps.
        /// </summary>
        public void SetCharacter(string name, string highJob, Department department, int count, int cap)
        {
            if (_root == null) return;
            _characterName.text = string.IsNullOrWhiteSpace(name) ? "Unnamed" : name.Trim();
            bool hasHigh = !string.IsNullOrEmpty(highJob);
            _highSwatch.style.display = hasHigh ? DisplayStyle.Flex : DisplayStyle.None;
            _highPrefix.text = hasHigh ? "High:" : "No job preferences";
            _highJob.text = hasHigh ? highJob : "";
            foreach (Department each in Enum.GetValues(typeof(Department)))
            {
                string cls = DepartmentClass(each);
                bool on = hasHigh && each == department && cls != null;
                if (cls == null) continue;
                _highSwatch.EnableInClassList(cls, on);
                _highJob.EnableInClassList(cls, on);
            }

            _characterCount.text = $"{count}/{cap}";
            _root.Q<Button>("lobby-character").tooltip = $"Edit character {_characterName.text}";
        }

        /// <summary>The token class for a department's colour (<c>LobbyTokens.uss</c>); null for none.</summary>
        public static string DepartmentClass(Department department) =>
            department == Department.None ? null : "lb-dept--" + department.ToString().ToLowerInvariant();

        public void SetPreviewTexture(Texture texture)
        {
            _previewTexture = texture;
            ApplyPreviewTexture();
        }

        private void ApplyPreviewTexture()
        {
            if (_previewImage == null) return;
            _previewImage.style.backgroundImage = _previewTexture switch
            {
                RenderTexture rt => Background.FromRenderTexture(rt),
                Texture2D tex => new StyleBackground(tex),
                _ => StyleKeyword.None,
            };
        }

        /// <summary>
        /// The music row (Decision 15). It's hidden in the in-round Escape menu: the music has faded and the game
        /// audio has taken over.
        /// </summary>
        public void SetMusic(bool visible, bool on, string title, float volume)
        {
            if (_root == null) return;
            _music.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _music.EnableInClassList("lobby__music--off", !on);
            _musicToggle.tooltip = on ? "Turn music off" : "Turn music on";
            string shown = string.IsNullOrEmpty(title) ? "—" : title;
            _musicTitle.text = on ? shown : "Off";
            if (!_musicDragging && Mathf.Abs(_musicVolume.value - volume) > 0.001f) _musicVolume.SetValueWithoutNotify(volume);
        }

        public void SetChat(IEnumerable<OocChatMessage> log, int version, bool admin, bool connected) =>
            _chat?.SetChat(log, version, admin, connected);

        public void SetServerSettings(ServerSettings settings, string[] maps, string status) =>
            _serverSettings?.SetServerSettings(settings, maps, status);

        /// <summary>Shows the admin's Stop confirmation while the map editor has unsaved edits (token 0 hides it).</summary>
        public void SetMapStopPrompt(int token, string editingMap, string error) => _mapStop?.Set(token, editingMap, error);

        /// <summary>
        /// Supplies the last outcome as this viewer sees it; null hides the tab. <paramref name="select"/> switches to
        /// it (once per round, chosen by the host), except from Server Settings or Chat.
        /// </summary>
        public void SetLastRound(RoundSummaryModel model, StyleSheet style, bool select)
        {
            if (_lastRound == null) return;
            bool changed = !ReferenceEquals(_lastRound.Model?.Outcome, model?.Outcome);
            // Never pull an admin out of the settings form or a player out of a chat line; the tab still appears.
            select &= _activeTab is not (TabServerSettings or TabChat);
            _lastRound.Set(model, style);
            if (!changed && !select) return;
            if (select && model != null)
            {
                SelectTab(TabLastRound);
                return;
            }

            if (!TabShown(_activeTab)) _activeTab = TabServerInfo;
            RefreshTabs();
            ShowTab(_activeTab);
        }
    }
}
