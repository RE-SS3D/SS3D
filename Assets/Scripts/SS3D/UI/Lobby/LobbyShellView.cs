using System;
using System.Collections.Generic;
using System.Linq;
using SS3D.Permissions;
using SS3D.UI.Shell;
using SS3D.Systems.Comms;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inputs;
using SS3D.Systems.Roles;
using SS3D.Systems.Rounds;
using SS3D.Systems.Settings;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.Lobby
{
    /// <summary>
    /// Pre-round lobby shell. Session actions and snapshots are supplied by LobbyUiSubSystem.
    /// </summary>
    public sealed partial class LobbyShellView : IUiSurface
    {
        private const string TabServerInfo = "server-info";
        private const string TabJobs = "jobs";
        private const string TabChat = "chat";
        private const string TabPlayers = "players";
        private const string TabSettings = "settings";
        private const string TabServerSettings = "server-settings";

        private readonly LobbyAssetCatalog _catalog;

        private VisualElement _root;
        private VisualElement _tabBar;
        private VisualElement _tabContent;
        private VisualElement _roundDot;
        private Label _countdownLabel;
        private Label _countdownSub;
        private Button _readyButton;
        private Button _adminButton;
        private VisualElement _previewImage;
        private Label _characterNameLabel;
        private Texture _livePreviewTexture;

        private string _activeTab = TabServerInfo;
        private bool _ready = true;
        private bool _adminActive;
        private bool _countingDown;
        private int _secondsLeft = 4 * 60 + 12;
        private string _playerSearch = string.Empty;
        private string _settingsSubTab = "display";
        private OocChannel _chatChannel = OocChannel.Ooc;
        private OocChatMessage[] _chat = Array.Empty<OocChatMessage>();
        private int _chatVersion = -1;
        private bool _chatAdmin;
        private ScrollView _chatLog;
        private string _openPlayerId;
        // PlayerSettingsSubSystem's Controls state; the capture itself runs in InputSubSystem.
        private IReadOnlyList<KeybindRow> _keybindRows = Array.Empty<KeybindRow>();
        private string _capturingRowId;
        private int _controlsNoticeVersion;
        private Label _toast;
        private IVisualElementScheduledItem _toastHide;
        // The Settings scroll view is rebuilt with the tab; keep the reader's place within one sub-tab.
        private ScrollView _settingsScroll;
        private string _settingsScrollSubTab;
        private bool _confirmingControlsReset;
        // PlayerSettingsSubSystem's live display values and what the machine offers.
        private PlayerDisplaySettings _display = new();
        private Vector2Int[] _resolutions = Array.Empty<Vector2Int>();
        private Vector2Int _currentResolution;
        private bool _stpSupported = true;
        private bool _confirmingDisplayReset;
        private bool _raisingDisplayIntent;
        private VisualElement _displayPrompt;
        private Label _displayPromptText;
        // PlayerSettingsSubSystem's live audio values; the view only reads them and raises intents.
        private PlayerAudioSettings _audio = new();
        private bool _raisingAudioIntent;
        private bool _confirmingAudioReset;
        private static readonly (PlayerVolume Volume, string Key, string Title)[] VolumeSliders =
        {
            (PlayerVolume.Master, "masterVolume", "Master Volume"),
            (PlayerVolume.Music, "musicVolume", "Music Volume"),
            (PlayerVolume.Sfx, "sfxVolume", "SFX Volume"),
            (PlayerVolume.Ambience, "ambienceVolume", "Ambience Volume"),
            (PlayerVolume.Personal, "personalVolume", "Personal Volume"),
        };

        // Missing jobs are Never: an empty list is the explicit "no preference" (lobby.md §3).
        private readonly Dictionary<string, JobPriority> _priorities = new();
        private readonly HashSet<string> _antagSelected = new();
        private JobSlotInfo[] _jobs = Array.Empty<JobSlotInfo>();
        private readonly HashSet<string> _collapsedDepts = new();

        public event Action CharacterCreatorRequested;
        public event Action ReadyRequested;
        public event Action QuitRequested;

        /// <summary>"Report a bug": the host opens the bug report dialog (<c>sandbox-server.md</c> §7).</summary>
        public event Action BugReportRequested;
        public event Action<bool> RoundChangeRequested;
        public event Action JobPreferencesChanged;
        public event Action<OocChannel, string> ChatSubmitted;
        public event Action<PlayerVolume, float> VolumeChanged;
        public event Action<bool> MuteAllChanged;
        public event Action<bool> MuteWhenUnfocusedChanged;
        public event Action AudioResetRequested;
        public event Action<Vector2Int> ResolutionChanged;
        public event Action<PlayerWindowMode> WindowModeChanged;
        public event Action<bool> VSyncChanged;
        public event Action<int> FrameCapChanged;
        public event Action<PlayerQualityTier> QualityChanged;
        public event Action<float> RenderScaleChanged;
        public event Action<PlayerUpscaler> UpscalerChanged;
        public event Action<PlayerAntiAliasing> AntiAliasingChanged;
        public event Action<float> UiScaleChanged;
        public event Action<bool> ShowFpsChanged;
        public event Action DisplayResetRequested;
        public event Action DisplayChangeKept;
        public event Action DisplayChangeReverted;
        public event Action<string> KeyCaptureRequested;
        public event Action KeyCaptureCancelled;
        public event Action ControlsResetRequested;

        /// <summary>Supplies the Controls tab's rows and state; rebuilds an open Controls tab.</summary>
        public void SetControls(IReadOnlyList<KeybindRow> rows, string capturingRowId, string notice, int noticeVersion)
        {
            _keybindRows = rows ?? Array.Empty<KeybindRow>();
            _capturingRowId = capturingRowId;
            if (noticeVersion != _controlsNoticeVersion)
            {
                _controlsNoticeVersion = noticeVersion;
                if (!string.IsNullOrEmpty(notice)) ShowToast(notice);
            }
            if (_activeTab == TabSettings && _settingsSubTab == "keyboard" && _tabContent != null)
                ShowTab(TabSettings);
        }

        /// <summary>A short message over the shell that fades on its own ("Use is now Q").</summary>
        private void ShowToast(string text)
        {
            if (_toast == null) return;
            _toast.text = text;
            _toast.style.display = DisplayStyle.Flex;
            _toastHide?.Pause();
            _toastHide = _toast.schedule.Execute(() => _toast.style.display = DisplayStyle.None).StartingIn(3500);
        }

        private void CancelKeyCapture()
        {
            if (_capturingRowId != null) KeyCaptureCancelled?.Invoke();
        }

        /// <summary>Supplies the display settings and the machine's choices; rebuilds an open Display tab.</summary>
        public void SetDisplaySettings(PlayerDisplaySettings display, Vector2Int[] resolutions, Vector2Int current, bool stpSupported)
        {
            _display = display ?? new PlayerDisplaySettings();
            _resolutions = resolutions ?? Array.Empty<Vector2Int>();
            _currentResolution = current;
            _stpSupported = stpSupported;
            // Skip the echo of a live UI Scale drag: rebuilding the tab would drop the drag.
            if (!_raisingDisplayIntent && _activeTab == TabSettings && _settingsSubTab == "display" && _tabContent != null)
                ShowTab(TabSettings);
        }

        /// <summary>Shows or hides the "Keep these settings?" prompt (Decision 3); called every refresh while pending.</summary>
        public void SetDisplayConfirmation(bool pending, int secondsLeft)
        {
            if (_displayPrompt == null) return;
            _displayPrompt.style.display = pending ? DisplayStyle.Flex : DisplayStyle.None;
            if (pending) _displayPromptText.text = $"Keep these display settings? Reverting in {secondsLeft} s.";
        }

        /// <summary>
        /// Supplies the audio settings to show. Rebuilds an open Sound tab, except for the echo of the view's own
        /// slider intent (rebuilding mid-drag would drop the drag).
        /// </summary>
        public void SetAudioSettings(PlayerAudioSettings audio)
        {
            _audio = audio ?? new PlayerAudioSettings();
            if (!_raisingAudioIntent && _activeTab == TabSettings && _settingsSubTab == "sound" && _tabContent != null)
                ShowTab(TabSettings);
        }

        public IReadOnlyDictionary<string, JobPriority> JobPriorities => _priorities;

        public IReadOnlyCollection<string> AntagonistOptIns => _antagSelected;
        private bool _runtime;
        // Focused lobby text fields mask keybinds (B = map editor, Escape = menu).
        private readonly InputTextEntryScope _textEntry = new();
        private LobbyMockData.PlayerRow[] _players = Array.Empty<LobbyMockData.PlayerRow>();
        private Label _playerCount;
        private Label _roundStatus;
        private RoundState _roundState;
        private bool _inRound;

        /// <summary>True while a keybind button waits for its next key (Escape cancels it).</summary>
        public bool IsCapturingKey => _capturingRowId != null;

        /// <param name="inRound">The local player has a body; the shell is open as the Escape menu.</param>
        public void SetSession(RoundState state, int seconds, bool ready, bool canAct,
            bool admin, LobbyMockData.PlayerRow[] players, bool inRound)
        {
            _runtime = true;
            _inRound = inRound;
            _roundState = state;
            if (_roundStatus != null) _roundStatus.text = state.ToString();
            _ready = ready;
            bool playersChanged = !_players.Select(p => (p.Id, p.Ready)).SequenceEqual(players.Select(p => (p.Id, p.Ready)));
            _players = players;
            // The self-starting lobby countdown runs while idle, before the station loads.
            bool autoCounting = state is RoundState.Stopped or RoundState.Ended && _autoStartSeconds > 0;
            _countingDown = state == RoundState.WarmingUp || autoCounting;
            _secondsLeft = autoCounting ? _autoStartSeconds : seconds;
            if (_playerCount != null) _playerCount.text = $"{players.Length} online";
            if (!admin && _adminActive) ToggleAdmin();
            _adminButton?.SetEnabled(admin);
            RefreshSidebar();
            RefreshServerSettings();
            _readyButton?.SetEnabled(canAct && !inRound && state is not RoundState.Ending);
            if (playersChanged && _activeTab == TabPlayers) ShowTab(TabPlayers);
        }

        private bool _autoStartEnabled;
        private bool _autoStartHeld;
        private int _autoStartSeconds;

        /// <summary>Server auto-start state; call before <see cref="SetSession"/>.</summary>
        public void SetAutoStart(bool enabled, bool held, int secondsLeft)
        {
            _autoStartEnabled = enabled;
            _autoStartHeld = held;
            _autoStartSeconds = secondsLeft;
        }

        private string _serverName = "Connecting…";
        private string _motd = "";
        private string _serverMode = "—";
        private string _serverBuild = "";
        private DateTimeOffset? _serverStarted;
        private Label _headerServerLabel;
        private Label _headerModeLabel;

        /// <summary>Public server metadata; the Server Info tab rebuilds only when its text changes.</summary>
        public void SetServerInfo(string name, string motd, string mode, string build, DateTimeOffset? started)
        {
            name = string.IsNullOrEmpty(name) ? "Connecting…" : name;
            mode = string.IsNullOrEmpty(mode) ? "—" : mode;
            bool changed = name != _serverName || motd != _motd || mode != _serverMode || build != _serverBuild;
            _serverName = name;
            _motd = motd ?? "";
            _serverMode = mode;
            _serverBuild = build ?? "";
            _serverStarted = started;
            if (_headerServerLabel != null) _headerServerLabel.text = _serverName;
            if (_headerModeLabel != null) _headerModeLabel.text = _serverMode;
            if (changed && _activeTab == TabServerInfo) ShowTab(TabServerInfo);
            else if (_tabContent?.Q<Label>("server-info-meta") is { } meta) meta.text = ServerMetaText();
        }

        private string ServerMetaText()
        {
            string uptime = "—";
            if (_serverStarted is { } started)
            {
                TimeSpan up = DateTimeOffset.UtcNow - started;
                if (up < TimeSpan.Zero) up = TimeSpan.Zero;
                uptime = up.TotalHours >= 1 ? $"{(int)up.TotalHours}h {up.Minutes:00}m" : $"{up.Minutes}m";
            }
            return string.IsNullOrEmpty(_serverBuild) ? $"up {uptime}" : $"build {_serverBuild} · up {uptime}";
        }

        public void SetJobs(IReadOnlyList<JobSlotInfo> jobs)
        {
            if (_jobs.SequenceEqual(jobs)) return;
            _jobs = jobs.ToArray();
            if (_activeTab == TabJobs) ShowTab(TabJobs);
        }


        public LobbyShellView(LobbyAssetCatalog catalog)
        {
            _catalog = catalog;
        }

        public bool IsVisible => _root != null && _root.resolvedStyle.display != DisplayStyle.None;

        public void Attach(VisualElement layerRoot)
        {
            _root = new VisualElement { name = "lobby-shell-surface" };
            _root.AddToClassList("lobby-shell");
            _root.pickingMode = PickingMode.Position;
            _root.focusable = true;

            if (_catalog != null && _catalog.LobbyStyle != null)
            {
                _root.styleSheets.Add(_catalog.LobbyStyle);
            }

            VisualElement window = new() { name = "lobby-shell-window" };
            window.AddToClassList("lobby-shell__window");
            window.pickingMode = PickingMode.Position;
            _root.Add(window);

            BuildChrome(window);
            _root.Add(BuildMapStopPrompt());
            _root.Add(BuildDisplayPrompt());
            _toast = new Label { name = "lobby-toast", pickingMode = PickingMode.Ignore };
            _toast.AddToClassList("lobby-shell__toast");
            _toast.AddToClassList("font-body");
            _toast.style.display = DisplayStyle.None;
            _root.Add(_toast);
            layerRoot.Add(_root);
            RefreshTabs();
            ShowTab(_activeTab);
            RefreshSidebar();
            SetVisible(true);
        }

        public void Detach()
        {
            CharacterCreatorRequested = null;
            ReadyRequested = null;
            RoundChangeRequested = null;
            ServerSettingsChanged = null;
            VolumeChanged = null;
            MuteAllChanged = null;
            MuteWhenUnfocusedChanged = null;
            AudioResetRequested = null;
            ResolutionChanged = null;
            WindowModeChanged = null;
            VSyncChanged = null;
            FrameCapChanged = null;
            QualityChanged = null;
            RenderScaleChanged = null;
            UpscalerChanged = null;
            AntiAliasingChanged = null;
            UiScaleChanged = null;
            ShowFpsChanged = null;
            DisplayResetRequested = null;
            DisplayChangeKept = null;
            DisplayChangeReverted = null;
            KeyCaptureRequested = null;
            KeyCaptureCancelled = null;
            ControlsResetRequested = null;
            MapStopResolved = null;
            _mapStopPrompt = null;
            _mapStopToken = 0;
            _displayPrompt = null;
            _displayPromptText = null;
            _toast = null;
            _settingsScroll = null;
            RoundAdminRequested = null;
            JobPreferencesChanged = null;
            _tabBar = null;
            _tabContent = null;
            _roundDot = null;
            _countdownLabel = null;
            _countdownSub = null;
            _stationLoadLabel = null;
            _headerMapLabel = null;
            _readyButton = null;
            _adminButton = null;
            _previewImage = null;
            _characterNameLabel = null;
            _livePreviewTexture = null;
            _textEntry.Exit();
            _root?.RemoveFromHierarchy();
            _root = null;
        }

        public void SetVisible(bool visible)
        {
            if (_root == null)
            {
                return;
            }

            CancelKeyCapture();
            if (!visible) _textEntry.Exit();
            _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            _root.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
        }

        public void SetCharacterName(string name)
        {
            string display = string.IsNullOrWhiteSpace(name) ? LobbyMockData.CharacterName : name.Trim();
            if (_characterNameLabel != null)
            {
                _characterNameLabel.text = display.ToUpperInvariant();
            }
        }

        public void SetPreviewTexture(Texture texture)
        {
            _livePreviewTexture = texture;
            ApplyPreviewTexture();
        }

        private void ApplyPreviewTexture()
        {
            if (_previewImage == null)
            {
                return;
            }

            if (_livePreviewTexture is RenderTexture renderTexture)
            {
                _previewImage.style.backgroundImage = Background.FromRenderTexture(renderTexture);
                return;
            }

            if (_livePreviewTexture is Texture2D texture2D)
            {
                _previewImage.style.backgroundImage = new StyleBackground(texture2D);
                return;
            }

            _previewImage.style.backgroundImage = StyleKeyword.None;
        }

        private void BuildChrome(VisualElement window)
        {
            window.Clear();

            VisualElement header = new();
            header.AddToClassList("lobby-shell__header");
            header.pickingMode = PickingMode.Ignore;

            VisualElement headerLeft = new();
            headerLeft.AddToClassList("lobby-shell__header-left");
            headerLeft.pickingMode = PickingMode.Ignore;
            _headerServerLabel = MakeLabel(_serverName, "font-body", "text-primary");
            headerLeft.Add(_headerServerLabel);
            headerLeft.Add(MakeSep());
            _headerMapLabel = MakeLabel("No map loaded", "font-body", "text-secondary");
            headerLeft.Add(_headerMapLabel);
            headerLeft.Add(MakeSep());
            _headerModeLabel = MakeLabel(_serverMode, "font-body", "text-secondary");
            headerLeft.Add(_headerModeLabel);
            header.Add(headerLeft);

            VisualElement headerRight = new();
            headerRight.AddToClassList("lobby-shell__header-right");
            headerRight.pickingMode = PickingMode.Ignore;
            // Header is session strip only (design): players + round — not build/uptime.
            _playerCount = MakeMeta("Connecting…");
            headerRight.Add(_playerCount);
            headerRight.Add(MakeSep());
            _roundStatus = MakeMeta("Connecting…");
            headerRight.Add(_roundStatus);
            header.Add(headerRight);
            window.Add(header);

            VisualElement body = new();
            body.AddToClassList("lobby-shell__body");

            VisualElement main = new();
            main.AddToClassList("lobby-shell__main");

            _tabBar = new VisualElement();
            _tabBar.AddToClassList("lobby-shell__tabs");
            main.Add(_tabBar);

            _tabContent = new VisualElement();
            _tabContent.AddToClassList("lobby-shell__tab-content");
            main.Add(_tabContent);

            body.Add(main);
            body.Add(BuildSidebar());
            window.Add(body);
        }

        private VisualElement BuildSidebar()
        {
            VisualElement sidebar = new();
            sidebar.AddToClassList("lobby-shell__sidebar");

            VisualElement roundPanel = new();
            roundPanel.AddToClassList("lobby-shell__panel");

            VisualElement roundHeader = new();
            roundHeader.AddToClassList("lobby-shell__panel-header");
            _roundDot = new VisualElement();
            _roundDot.AddToClassList("lobby-shell__panel-dot");
            roundHeader.Add(_roundDot);
            Label roundTitle = MakeLabel("Round State", "font-titling");
            roundTitle.AddToClassList("lobby-shell__panel-title");
            roundHeader.Add(roundTitle);
            roundPanel.Add(roundHeader);

            VisualElement roundBody = new();
            roundBody.AddToClassList("lobby-shell__panel-body");
            _countdownLabel = new Label();
            _countdownLabel.AddToClassList("lobby-shell__countdown");
            _countdownLabel.AddToClassList("font-terminal");
            _countdownSub = new Label();
            _countdownSub.AddToClassList("lobby-shell__countdown-sub");
            _countdownSub.AddToClassList("font-titling");
            roundBody.Add(_countdownSub);
            roundBody.Add(_countdownLabel);
            _stationLoadLabel = new Label("No map loaded") { name = "lobby-map-load-status" };
            _stationLoadLabel.AddToClassList("lobby-shell__map-load-status");
            roundBody.Add(_stationLoadLabel);
            roundPanel.Add(roundBody);
            sidebar.Add(roundPanel);

            _characterNameLabel = new Label(LobbyMockData.CharacterName.ToUpperInvariant());
            _characterNameLabel.AddToClassList("lobby-shell__character-name");
            _characterNameLabel.AddToClassList("font-arcade");
            _characterNameLabel.pickingMode = PickingMode.Ignore;
            sidebar.Add(_characterNameLabel);

            VisualElement preview = new();
            preview.AddToClassList("lobby-shell__preview");
            preview.pickingMode = PickingMode.Position;
            preview.RegisterCallback<ClickEvent>(_ => CharacterCreatorRequested?.Invoke());

            _previewImage = new VisualElement();
            _previewImage.AddToClassList("lobby-shell__preview-image");
            _previewImage.pickingMode = PickingMode.Ignore;
            ApplyPreviewTexture();
            preview.Add(_previewImage);

            Label previewHint = new("Click to open character creator");
            previewHint.AddToClassList("lobby-shell__preview-hint");
            previewHint.AddToClassList("font-body");
            previewHint.pickingMode = PickingMode.Ignore;
            preview.Add(previewHint);
            sidebar.Add(preview);

            VisualElement actions = new();
            actions.AddToClassList("lobby-shell__actions");

            _readyButton = new Button(ToggleReady) { text = "READY" };
            _readyButton.AddToClassList("lobby-shell__btn");
            _readyButton.AddToClassList("lobby-shell__btn--ready");
            _readyButton.AddToClassList("font-titling");
            actions.Add(_readyButton);

            VisualElement row = new();
            row.AddToClassList("lobby-shell__actions-row");

            Button observe = new(() => Debug.Log("[Lobby] Observe clicked (stub until observer wiring)."))
            {
                text = "OBSERVE",
            };
            observe.SetEnabled(false);
            observe.tooltip = "Observer mode is not implemented yet.";
            observe.AddToClassList("lobby-shell__btn");
            observe.AddToClassList("lobby-shell__btn--observe");
            observe.AddToClassList("font-titling");
            row.Add(observe);

            _adminButton = new Button(ToggleAdmin) { text = "ADMIN" };
            _adminButton.AddToClassList("lobby-shell__btn");
            _adminButton.AddToClassList("lobby-shell__btn--admin");
            _adminButton.AddToClassList("font-titling");
            row.Add(_adminButton);

            actions.Add(row);

            Button report = new(() => BugReportRequested?.Invoke())
            {
                name = "lobby-report-bug",
                text = "REPORT A BUG (F8)",
            };
            report.AddToClassList("lobby-shell__btn");
            report.AddToClassList("lobby-shell__btn--report");
            report.AddToClassList("font-titling");
            actions.Add(report);
            actions.Add(BuildQuitButton());
            sidebar.Add(actions);
            return sidebar;
        }

        /// <summary>
        /// Leaves for the main menu (ending the session when hosting). The first click arms it and the
        /// second leaves, so a stray click in the in-round menu can't drop the player out; moving the
        /// pointer away disarms it.
        /// </summary>
        private Button BuildQuitButton()
        {
            const string idleText = "QUIT TO MENU";
            const string armedText = "CLICK AGAIN TO LEAVE";
            bool armed = false;
            Button quit = null;
            quit = new Button(() =>
            {
                if (!armed)
                {
                    armed = true;
                    quit.text = armedText;
                    quit.AddToClassList("lobby-shell__btn--quit-armed");
                    return;
                }

                QuitRequested?.Invoke();
            })
            {
                name = "lobby-quit",
                text = idleText,
            };
            quit.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                armed = false;
                quit.text = idleText;
                quit.RemoveFromClassList("lobby-shell__btn--quit-armed");
            });
            quit.AddToClassList("lobby-shell__btn");
            quit.AddToClassList("lobby-shell__btn--quit");
            quit.AddToClassList("font-titling");
            return quit;
        }

        private void RefreshSidebar()
        {
            if (_countdownLabel == null)
            {
                return;
            }

            _countdownLabel.EnableInClassList("lobby-shell__countdown--waiting", !_countingDown);
            if (_countingDown)
            {
                int mm = _secondsLeft / 60;
                int ss = _secondsLeft % 60;
                _countdownLabel.text = $"{mm:00}:{ss:00}";
                _countdownSub.text = "Starting In";
                _roundDot?.EnableInClassList("lobby-shell__panel-dot--live", true);
            }
            else
            {
                int readyCount = 0;
                foreach (LobbyMockData.PlayerRow player in _players)
                    if (player.Ready) readyCount++;
                _countdownLabel.text = $"{readyCount}/{_players.Length} ready";
                _countdownSub.text = _runtime ? _roundState.ToString() : "Waiting For Players";
                _roundDot?.EnableInClassList("lobby-shell__panel-dot--live", false);
            }

            if (_readyButton != null)
            {
                _readyButton.text = _ready ? "✓  READY" : "NOT READY";
                if (_runtime && _roundState == RoundState.Ongoing) _readyButton.text = _inRound ? "IN ROUND" : "JOIN ROUND";
                _readyButton.EnableInClassList("lobby-shell__btn--ready-on", _ready);
            }

            if (_adminButton != null)
            {
                _adminButton.text = _adminActive ? "ADMIN ✓" : "ADMIN";
                _adminButton.EnableInClassList("lobby-shell__btn--admin-on", _adminActive);
            }
        }

        private void RefreshTabs()
        {
            if (_tabBar == null)
            {
                return;
            }

            _tabBar.Clear();
            AddTab(TabServerInfo, "Server Info", false);
            AddTab(TabJobs, "Jobs", false);
            AddTab(TabChat, "Chat", false);
            AddTab(TabPlayers, "Players", false);
            AddTab(TabSettings, "Settings", false);
            if (_adminActive)
            {
                AddTab(TabServerSettings, "Server Settings", true);
            }
        }

        private void AddTab(string key, string label, bool admin)
        {
            Button tab = new(() => SelectTab(key)) { name = "tab-" + key, text = label.ToUpperInvariant() };
            tab.AddToClassList("lobby-shell__tab");
            tab.AddToClassList("font-titling");
            tab.style.flexGrow = 1;
            tab.style.flexBasis = 0;
            if (admin)
            {
                tab.AddToClassList("lobby-shell__tab--admin");
            }

            tab.EnableInClassList("lobby-shell__tab--active", key == _activeTab);
            _tabBar.Add(tab);
        }

        private void SelectTab(string key)
        {
            CancelKeyCapture();
            _activeTab = key;
            RefreshTabs();
            ShowTab(key);
        }

        private void ShowTab(string key)
        {
            if (_tabContent == null)
            {
                return;
            }

            // A focused field removed by the rebuild gets no FocusOut.
            _textEntry.Exit();
            _tabContent.Clear();
            _tabContent.EnableInClassList(
                "lobby-shell__tab-content--panel",
                key == TabChat || key == TabPlayers);
            switch (key)
            {
                case TabServerInfo:
                {
                    BuildServerInfoTab(_tabContent);
                    break;
                }

                case TabJobs:
                {
                    BuildJobsTab(_tabContent);
                    break;
                }

                case TabChat:
                {
                    BuildChatTab(_tabContent);
                    break;
                }

                case TabPlayers:
                {
                    BuildPlayersTab(_tabContent);
                    break;
                }

                case TabSettings:
                {
                    BuildSettingsTab(_tabContent);
                    break;
                }

                case TabServerSettings:
                {
                    BuildServerSettingsTab(_tabContent);
                    break;
                }

                default:
                {
                    _tabContent.Add(MakePlaceholder($"Unknown tab: {key}"));
                    break;
                }
            }
        }

        private void BuildServerInfoTab(VisualElement parent)
        {
            VisualElement hero = new();
            hero.AddToClassList("lobby-shell__hero");
            hero.pickingMode = PickingMode.Ignore;
            if (_catalog != null && _catalog.ServerInfoBanner != null)
            {
                hero.style.backgroundImage = new StyleBackground(_catalog.ServerInfoBanner);
            }

            float bannerAspect = _catalog != null && _catalog.ServerInfoBanner != null
                ? _catalog.ServerInfoBanner.rect.width / _catalog.ServerInfoBanner.rect.height
                : 21f / 9f;
            hero.RegisterCallback<GeometryChangedEvent>(evt => hero.style.height = evt.newRect.width / bannerAspect);
            VisualElement card = new();
            card.AddToClassList("lobby-shell__server-card");
            parent.Add(card);
            card.Add(hero);
            VisualElement information = new();
            information.AddToClassList("lobby-shell__server-information");
            card.Add(information);

            VisualElement titleRow = new();
            titleRow.AddToClassList("lobby-shell__server-title-row");
            titleRow.pickingMode = PickingMode.Ignore;

            Label title = new(_serverName.ToUpperInvariant()) { enableRichText = false };
            title.AddToClassList("lobby-shell__server-title");
            title.AddToClassList("font-titling");
            titleRow.Add(title);

            Label meta = new(ServerMetaText()) { name = "server-info-meta" };
            meta.AddToClassList("lobby-shell__server-meta");
            meta.AddToClassList("font-terminal");
            titleRow.Add(meta);
            information.Add(titleRow);

            // Left: MOTD + Map/Mode stacked. Right: Change Log spanning both rows.
            VisualElement bodyGrid = new();
            bodyGrid.AddToClassList("lobby-shell__server-body");

            VisualElement leftCol = new();
            leftCol.AddToClassList("lobby-shell__server-left");

            VisualElement motdBlock = new();
            motdBlock.AddToClassList("lobby-shell__motd");
            motdBlock.Add(SectionTitle("Message of the Day"));
            Label motd = new(_motd) { name = "server-info-motd", enableRichText = false };
            motd.AddToClassList("lobby-shell__inset");
            motd.AddToClassList("font-body");
            motdBlock.Add(motd);
            leftCol.Add(motdBlock);

            VisualElement mapModeBlock = new();
            mapModeBlock.AddToClassList("lobby-shell__map-mode-block");
            mapModeBlock.Add(SectionTitle("Map & Mode"));

            VisualElement mapModeRow = new();
            mapModeRow.AddToClassList("lobby-shell__map-mode-row");
            mapModeRow.Add(BuildSelectCard(
                string.IsNullOrEmpty(_stationName) ? "No map loaded" : _stationName,
                "Map",
                string.IsNullOrEmpty(_stationName) ? "?" : _stationName.Substring(0, 1).ToUpperInvariant(),
                mapSwatch: true));
            mapModeRow.Add(BuildSelectCard(
                _serverMode,
                "Mode",
                _serverMode.Length > 0 ? _serverMode[0].ToString().ToUpperInvariant() : "?",
                mapSwatch: false));
            mapModeBlock.Add(mapModeRow);
            leftCol.Add(mapModeBlock);
            bodyGrid.Add(leftCol);

            bodyGrid.Add(BuildChangelog());
            information.Add(bodyGrid);
        }

        /// <summary>
        /// The client build's change log (see <see cref="BuildInfo"/>): merged PR titles grouped under
        /// the release that shipped them, newest first. Rows open the PR on GitHub; the header link
        /// opens the releases page.
        /// </summary>
        private static VisualElement BuildChangelog()
        {
            BuildInfo.Data info = BuildInfo.Current;

            VisualElement logCol = new();
            logCol.AddToClassList("lobby-shell__changelog-col");

            VisualElement header = new();
            header.AddToClassList("lobby-shell__changelog-header");
            header.Add(SectionTitle("Change Log"));
            if (info.ReleasesUrl is { } releasesUrl)
            {
                header.Add(ExternalLink("All releases ↗", releasesUrl, "changelog-releases-link"));
            }

            logCol.Add(header);

            ScrollView logBox = new(ScrollViewMode.Vertical) { name = "changelog-list" };
            logBox.AddToClassList("lobby-shell__inset");
            logBox.horizontalScrollerVisibility = ScrollerVisibility.Hidden;

            string currentRelease = null;
            foreach (BuildInfo.Entry entry in info.entries)
            {
                string release = entry.release ?? string.Empty;
                if (release != currentRelease)
                {
                    currentRelease = release;
                    Label group = new(release.Length > 0 ? $"v{release}" : "In development");
                    group.AddToClassList("lobby-shell__changelog-group");
                    group.AddToClassList("font-titling");
                    logBox.Add(group);
                }

                VisualElement row = new();
                row.AddToClassList("lobby-shell__changelog-row");
                row.tooltip = entry.date;
                Label ver = new(entry.pr > 0 ? $"#{entry.pr}" : entry.hash);
                ver.AddToClassList("lobby-shell__changelog-ver");
                ver.AddToClassList("font-terminal");
                Label text = new(entry.subject) { enableRichText = false };
                text.AddToClassList("lobby-shell__changelog-text");
                text.AddToClassList("font-body");
                row.Add(ver);
                row.Add(text);

                if (entry.Url(info.repository) is { } url)
                {
                    row.AddToClassList("lobby-shell__changelog-row--link");
                    row.RegisterCallback<ClickEvent>(_ => Application.OpenURL(url));
                }

                logBox.Add(row);
            }

            if (info.entries.Length == 0)
            {
                // Not changelog-text: its flex-basis 0 is for the row; in the column it collapses
                // the label to zero height and only the tops of the glyphs show.
                Label none = new("No change log in this build.");
                none.AddToClassList("lobby-shell__changelog-empty");
                none.AddToClassList("font-body");
                logBox.Add(none);
            }

            logCol.Add(logBox);
            return logCol;
        }

        private static Label ExternalLink(string text, string url, string name)
        {
            Label link = new(text) { name = name, tooltip = url };
            link.AddToClassList("lobby-shell__link");
            link.AddToClassList("font-body");
            link.RegisterCallback<ClickEvent>(_ => Application.OpenURL(url));
            return link;
        }

        private static VisualElement BuildSelectCard(string name, string kind, string initial, bool mapSwatch)
        {
            VisualElement card = new();
            card.AddToClassList("lobby-shell__select-card");

            VisualElement swatch = new();
            swatch.AddToClassList("lobby-shell__select-swatch");
            if (mapSwatch)
            {
                swatch.AddToClassList("lobby-shell__select-swatch--map");
            }

            Label swatchLabel = new(initial);
            swatchLabel.AddToClassList("lobby-shell__select-swatch-label");
            swatchLabel.AddToClassList("font-titling");
            swatch.Add(swatchLabel);
            card.Add(swatch);

            VisualElement textCol = new();
            textCol.AddToClassList("lobby-shell__select-text");
            Label nameLabel = new(name);
            if (mapSwatch) nameLabel.name = "server-info-map";
            nameLabel.AddToClassList("lobby-shell__select-name");
            nameLabel.AddToClassList("font-titling");
            textCol.Add(nameLabel);
            Label kindLabel = new(kind);
            kindLabel.AddToClassList("lobby-shell__select-kind");
            kindLabel.AddToClassList("font-body");
            textCol.Add(kindLabel);
            card.Add(textCol);
            return card;
        }

        private void BuildJobsTab(VisualElement parent)
        {
            VisualElement split = new();
            split.AddToClassList("lobby-shell__jobs-split");
            split.style.flexGrow = 1;

            ScrollView list = new();
            list.AddToClassList("lobby-shell__jobs-list");
            list.style.flexGrow = 1;

            if (_jobs.Length == 0)
            {
                list.Add(MakeLabel(_runtime ? "No jobs available." : "Connecting…", "font-body", "text-secondary"));
            }

            foreach (IGrouping<Department, JobSlotInfo> dept in _jobs
                         .GroupBy(job => (Department)job.Department)
                         .OrderBy(group => group.Key == Department.None ? int.MaxValue : (int)group.Key))
            {
                string deptKey = dept.Key == Department.None ? "other" : dept.Key.ToString().ToLowerInvariant();
                bool expanded = !_collapsedDepts.Contains(deptKey);
                bool unlimited = dept.Any(job => job.Unlimited);
                int openSum = dept.Sum(job => job.Unlimited ? 0 : job.Open);
                int slotSum = dept.Sum(job => job.Unlimited ? 0 : job.Slots);

                VisualElement header = new();
                header.AddToClassList("lobby-shell__dept-header");
                header.pickingMode = PickingMode.Position;
                header.RegisterCallback<ClickEvent>(_ =>
                {
                    if (!_collapsedDepts.Add(deptKey))
                    {
                        _collapsedDepts.Remove(deptKey);
                    }

                    ShowTab(TabJobs);
                });

                VisualElement deptIcon = new();
                deptIcon.AddToClassList("lobby-shell__dept-icon");
                deptIcon.pickingMode = PickingMode.Ignore;
                if (_catalog != null && _catalog.TryGetDepartmentIcon(deptKey, out Texture2D deptTexture))
                {
                    deptIcon.style.backgroundImage = new StyleBackground(deptTexture);
                }

                header.Add(deptIcon);

                Label name = new((dept.Key == Department.None ? "Other" : dept.Key.ToString()).ToUpperInvariant());
                name.AddToClassList("lobby-shell__dept-name");
                name.AddToClassList("font-titling");
                name.pickingMode = PickingMode.Ignore;
                header.Add(name);

                Label slots = new(unlimited ? "OPEN" : $"{openSum}/{slotSum}");
                slots.AddToClassList("lobby-shell__dept-slots");
                slots.AddToClassList("font-terminal");
                slots.pickingMode = PickingMode.Ignore;
                header.Add(slots);

                VisualElement chevron = new();
                chevron.AddToClassList("lobby-shell__dept-chevron");
                chevron.EnableInClassList("lobby-shell__dept-chevron--collapsed", !expanded);
                chevron.pickingMode = PickingMode.Ignore;
                if (_catalog != null && _catalog.ChevronDown != null)
                {
                    chevron.style.backgroundImage = new StyleBackground(_catalog.ChevronDown);
                }

                header.Add(chevron);
                list.Add(header);

                if (!expanded)
                {
                    continue;
                }

                foreach (JobSlotInfo job in dept)
                {
                    list.Add(BuildJobRow(job));
                }
            }

            split.Add(list);
            split.Add(BuildAntagPanel());
            parent.Add(split);
        }

        private VisualElement BuildJobRow(JobSlotInfo job)
        {
            VisualElement row = new();
            row.AddToClassList("lobby-shell__job-row");
            bool full = !job.Unlimited && job.Open == 0;
            row.EnableInClassList("lobby-shell__job-row--locked", full);

            VisualElement iconWell = new();
            iconWell.AddToClassList("lobby-shell__job-icon-well");
            VisualElement icon = new();
            icon.AddToClassList("lobby-shell__job-icon");
            if (_catalog != null && _catalog.TryGetJobIcon(JobIconId(job.Name), out Sprite sprite))
            {
                icon.style.backgroundImage = new StyleBackground(sprite);
            }

            iconWell.Add(icon);
            row.Add(iconWell);

            VisualElement info = new();
            info.AddToClassList("lobby-shell__job-info");
            Label name = new(job.Name);
            name.AddToClassList("lobby-shell__job-name");
            name.AddToClassList("font-body");
            info.Add(name);

            // Live demand next to the slot count (lobby.md §3), counted over online players.
            Label demand = new($"{job.Interested} interested");
            demand.AddToClassList("lobby-shell__job-meta");
            demand.AddToClassList("font-body");
            info.Add(demand);
            row.Add(info);

            Label slots = new(job.Unlimited ? "OPEN" : $"{job.Open}/{job.Slots}");
            slots.AddToClassList("lobby-shell__job-slots");
            slots.AddToClassList("font-terminal");
            slots.tooltip = job.Unlimited ? "No slot limit" : $"{job.Open} of {job.Slots} slots open";
            row.Add(slots);
            row.Add(BuildPriorityChips(job.Name));

            return row;
        }

        // Job icons are named after the art (e.g. SecurityOfficer.png), not the role asset.
        private static string JobIconId(string jobName) => jobName switch
        {
            "Security" => "SecurityOfficer",
            _ => jobName.Replace(" ", string.Empty),
        };

        private VisualElement BuildPriorityChips(string jobId)
        {
            VisualElement chips = new();
            chips.AddToClassList("lobby-shell__priority");
            JobPriority current = _priorities.TryGetValue(jobId, out JobPriority p) ? p : JobPriority.Never;

            foreach (JobPriority level in JobPriorityUtil.CycleOrder)
            {
                JobPriority captured = level;
                Button chip = new(() =>
                {
                    if (captured == JobPriority.Never) _priorities.Remove(jobId);
                    else _priorities[jobId] = captured;
                    JobPreferencesChanged?.Invoke();
                    ShowTab(TabJobs);
                })
                {
                    text = JobPriorityUtil.ShortLabel(level),
                    tooltip = JobPriorityUtil.FullLabel(level),
                };
                chip.AddToClassList("lobby-shell__priority-chip");
                chip.AddToClassList("font-body");
                if (level == current)
                {
                    chip.AddToClassList(PriorityClass(level));
                }
                else
                {
                    chip.AddToClassList("lobby-shell__priority-chip--idle");
                }

                chips.Add(chip);
            }

            return chips;
        }

        private static string PriorityClass(JobPriority priority) => priority switch
        {
            JobPriority.Never => "lobby-shell__priority-chip--never",
            JobPriority.Low => "lobby-shell__priority-chip--low",
            JobPriority.Medium => "lobby-shell__priority-chip--medium",
            JobPriority.High => "lobby-shell__priority-chip--high",
            _ => "lobby-shell__priority-chip--idle",
        };

        private VisualElement BuildAntagPanel()
        {
            VisualElement panel = new();
            panel.AddToClassList("lobby-shell__antag");

            VisualElement titleRow = new()
            {
                style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween, marginBottom = 4 },
            };
            Label title = SectionTitle("Antagonist");
            title.style.marginBottom = 0;
            titleRow.Add(title);

            bool allOn = AntagonistCategories.All.All(role => _antagSelected.Contains(role.Key));

            Label toggleAll = new(allOn ? "Clear All" : "Select All");
            toggleAll.AddToClassList("lobby-shell__link");
            toggleAll.AddToClassList("font-body");
            toggleAll.pickingMode = PickingMode.Position;
            toggleAll.RegisterCallback<ClickEvent>(_ =>
            {
                foreach ((string key, string _) in AntagonistCategories.All)
                {
                    if (allOn) _antagSelected.Remove(key);
                    else _antagSelected.Add(key);
                }

                JobPreferencesChanged?.Invoke();
                ShowTab(TabJobs);
            });
            titleRow.Add(toggleAll);
            panel.Add(titleRow);

            foreach ((string key, string roleLabel) in AntagonistCategories.All)
            {
                bool on = _antagSelected.Contains(key);

                VisualElement row = new();
                row.AddToClassList("lobby-shell__antag-row");
                row.pickingMode = PickingMode.Position;
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    if (on) _antagSelected.Remove(key);
                    else _antagSelected.Add(key);
                    JobPreferencesChanged?.Invoke();
                    ShowTab(TabJobs);
                });

                VisualElement box = new();
                box.AddToClassList("lobby-shell__check");
                box.EnableInClassList("lobby-shell__check--on", on);
                box.pickingMode = PickingMode.Ignore;
                if (on)
                {
                    Label mark = new("✓");
                    mark.AddToClassList("lobby-shell__check-mark");
                    mark.pickingMode = PickingMode.Ignore;
                    box.Add(mark);
                }

                row.Add(box);

                Label roleName = new(roleLabel);
                roleName.AddToClassList("lobby-shell__antag-name");
                roleName.AddToClassList("font-body");
                roleName.pickingMode = PickingMode.Ignore;
                row.Add(roleName);
                panel.Add(row);
            }

            VisualElement divider = new();
            divider.AddToClassList("lobby-shell__antag-divider");
            divider.pickingMode = PickingMode.Ignore;
            panel.Add(divider);

            Label hint = new("Affects eligibility only — checking a role does not guarantee it.");
            hint.AddToClassList("lobby-shell__antag-hint");
            hint.AddToClassList("font-body");
            panel.Add(hint);

            return panel;
        }

        /// <summary>Replaces the chat log snapshot; the open log rebuilds only when the version changes.</summary>
        public void SetChat(IEnumerable<OocChatMessage> log, int version, bool admin)
        {
            if (admin != _chatAdmin)
            {
                _chatAdmin = admin;
                if (!admin && _chatChannel == OocChannel.Admin) _chatChannel = OocChannel.Ooc;
                if (_activeTab == TabChat) ShowTab(TabChat);
            }

            if (version == _chatVersion) return;
            _chatVersion = version;
            _chat = log.ToArray();
            RefreshChatLog();
        }

        private void BuildChatTab(VisualElement parent)
        {
            VisualElement root = new();
            root.AddToClassList("lobby-shell__chat");
            parent.Add(root);

            VisualElement channels = new();
            channels.AddToClassList("lobby-shell__channel-row");
            AddChannel(channels, OocChannel.Ooc, "OOC");
            AddChannel(channels, OocChannel.Looc, "LOOC");
            if (_chatAdmin) AddChannel(channels, OocChannel.Admin, "Admin");
            root.Add(channels);

            _chatLog = new ScrollView();
            _chatLog.AddToClassList("lobby-shell__chat-log");
            root.Add(_chatLog);
            RefreshChatLog();

            VisualElement composer = new();
            composer.AddToClassList("lobby-shell__chat-composer");
            string channelLabel = _chatChannel.ToString().ToUpperInvariant();
            TextField input = new()
            {
                value = string.Empty,
                maxLength = OocChatRules.MaxLength,
            };
            input.AddToClassList("lobby-shell__chat-input");
            input.AddToClassList("font-body");
            input.label = string.Empty;
            if (input.textEdition != null)
            {
                input.textEdition.placeholder = _chatChannel == OocChannel.Looc
                    ? "Message players near your character…"
                    : $"Message {channelLabel}…";
                input.textEdition.hidePlaceholderOnFocus = true;
            }

            // TrickleDown: a real Return targets the inner text element, which consumes it before it
            // bubbles to the field (synthetic events sent to the field itself never showed this).
            input.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter)
                {
                    return;
                }

                string text = input.value?.Trim() ?? string.Empty;
                if (text.Length == 0)
                {
                    return;
                }

                Debug.Log($"[Lobby] Chat send ({_chatChannel.ToString().ToLowerInvariant()}): {text}");
                ChatSubmitted?.Invoke(_chatChannel, text);
                input.value = string.Empty;
                evt.StopPropagation();
            }, TrickleDown.TrickleDown);
            StyleDarkTextField(input);
            HoldTextEntry(input);
            composer.Add(input);
            root.Add(composer);
        }

        private void AddChannel(VisualElement parent, OocChannel channel, string label)
        {
            Button btn = new(() =>
            {
                _chatChannel = channel;
                ShowTab(TabChat);
            })
            {
                name = $"chat-channel-{channel.ToString().ToLowerInvariant()}",
                text = label.ToUpperInvariant(),
            };
            btn.AddToClassList("lobby-shell__channel");
            btn.AddToClassList("font-body");
            btn.EnableInClassList("lobby-shell__channel--active", _chatChannel == channel);
            parent.Add(btn);
        }

        private void RefreshChatLog()
        {
            if (_chatLog == null || _chatLog.panel == null) return;
            // Follow new lines only while the reader is already at the bottom.
            bool follow = _chatLog.scrollOffset.y >= _chatLog.verticalScroller.highValue - 4f;
            _chatLog.Clear();
            bool any = false;
            foreach (OocChatMessage message in _chat)
            {
                if (message.Channel != _chatChannel) continue;
                _chatLog.Add(BuildChatMessage(message));
                any = true;
            }

            if (!any)
            {
                Label empty = new(_runtime ? "No messages yet." : "Not connected.");
                empty.AddToClassList("lobby-shell__chat-empty");
                empty.AddToClassList("font-body");
                _chatLog.Add(empty);
            }

            if (!follow) return;
            // Pin to the newest line once the rebuilt content has been laid out.
            ScrollView log = _chatLog;
            log.schedule.Execute(() => log.scrollOffset = new Vector2(0f, float.MaxValue)).ExecuteLater(1);
        }

        private static VisualElement BuildChatMessage(OocChatMessage message)
        {
            if (message.IsSystem)
            {
                VisualElement announcement = new();
                announcement.AddToClassList("lobby-shell__chat-announcement");
                Label announcementText = new($"— {message.Text} —") { enableRichText = false };
                announcementText.AddToClassList("lobby-shell__chat-announcement-text");
                announcementText.AddToClassList("font-terminal");
                announcement.Add(announcementText);
                return announcement;
            }

            VisualElement row = new();
            row.AddToClassList("lobby-shell__chat-line");

            string time = DateTimeOffset.FromUnixTimeSeconds(message.UnixSeconds).ToLocalTime().ToString("HH:mm");
            Label timeLabel = new(time);
            timeLabel.AddToClassList("lobby-shell__chat-time");
            timeLabel.AddToClassList("font-terminal");
            row.Add(timeLabel);

            // Player-supplied text: never interpret rich-text tags.
            Label name = new($"{message.Sender}:") { enableRichText = false };
            name.AddToClassList("lobby-shell__chat-name");
            name.AddToClassList("font-body");
            name.EnableInClassList("lobby-shell__chat-name--admin", message.SenderRole >= ServerRoleTypes.Administrator);
            row.Add(name);

            Label body = new(message.Text) { enableRichText = false };
            body.AddToClassList("lobby-shell__chat-text");
            body.AddToClassList("font-terminal");
            row.Add(body);
            return row;
        }

        private void BuildPlayersTab(VisualElement parent)
        {
            VisualElement toolbar = new();
            toolbar.AddToClassList("lobby-shell__players-toolbar");

            TextField search = new() { value = _playerSearch };
            search.AddToClassList("lobby-shell__search");
            search.AddToClassList("font-body");
            search.label = string.Empty;
            if (search.textEdition != null)
            {
                search.textEdition.placeholder = "Search players…";
                search.textEdition.hidePlaceholderOnFocus = true;
            }

            search.RegisterValueChangedCallback(evt =>
            {
                _playerSearch = evt.newValue ?? string.Empty;
                ShowTab(TabPlayers);
            });
            StyleDarkTextField(search);
            HoldTextEntry(search);
            toolbar.Add(search);

            string filter = _playerSearch.Trim().ToLowerInvariant();
            int shown = 0;
            foreach (LobbyMockData.PlayerRow player in _players)
            {
                if (string.IsNullOrEmpty(filter) || player.Name.ToLowerInvariant().Contains(filter))
                {
                    shown++;
                }
            }

            Label count = new($"{shown}/{_players.Length}");
            count.AddToClassList("lobby-shell__players-count");
            count.AddToClassList("font-display");
            toolbar.Add(count);
            parent.Add(toolbar);

            ScrollView list = new();
            list.AddToClassList("lobby-shell__players-list");

            if (shown == 0)
            {
                list.Add(MakePlaceholder($"No players match \"{_playerSearch}\"."));
                parent.Add(list);
                return;
            }

            foreach (LobbyMockData.PlayerRow player in _players)
            {
                if (!string.IsNullOrEmpty(filter) && !player.Name.ToLowerInvariant().Contains(filter))
                {
                    continue;
                }

                bool open = _openPlayerId == player.Id;
                VisualElement card = new();
                card.AddToClassList("lobby-shell__player-card");

                VisualElement header = new();
                header.AddToClassList("lobby-shell__player-row");
                header.pickingMode = PickingMode.Position;
                string id = player.Id;
                header.RegisterCallback<ClickEvent>(_ =>
                {
                    _openPlayerId = _openPlayerId == id ? null : id;
                    ShowTab(TabPlayers);
                });

                VisualElement dot = new();
                dot.AddToClassList("lobby-shell__player-dot");
                dot.EnableInClassList("lobby-shell__player-dot--ready", player.Ready);
                dot.pickingMode = PickingMode.Ignore;
                header.Add(dot);

                Label name = new(player.Name);
                name.AddToClassList("lobby-shell__player-name");
                name.AddToClassList("font-display");
                name.pickingMode = PickingMode.Ignore;
                header.Add(name);

                if (!string.IsNullOrEmpty(player.Rank))
                {
                    string badgeLabel = RankBadgeLabel(player.Rank);
                    Label badge = new(badgeLabel);
                    badge.AddToClassList("lobby-shell__player-badge");
                    badge.AddToClassList("font-titling");
                    badge.EnableInClassList(
                        "lobby-shell__player-badge--admin",
                        player.Rank.Equals("admin", StringComparison.Ordinal));
                    badge.EnableInClassList(
                        "lobby-shell__player-badge--mentor",
                        player.Rank.Equals("mentor", StringComparison.Ordinal));
                    badge.pickingMode = PickingMode.Ignore;
                    header.Add(badge);
                }

                Label status = new(player.Ready ? "Ready" : "Not Ready");
                status.AddToClassList("lobby-shell__player-status");
                status.AddToClassList("font-body");
                status.pickingMode = PickingMode.Ignore;
                header.Add(status);

                VisualElement chevron = new();
                chevron.AddToClassList("lobby-shell__player-chevron");
                chevron.EnableInClassList("lobby-shell__player-chevron--collapsed", !open);
                chevron.pickingMode = PickingMode.Ignore;
                if (_catalog != null && _catalog.ChevronDown != null)
                {
                    chevron.style.backgroundImage = new StyleBackground(_catalog.ChevronDown);
                }

                header.Add(chevron);
                card.Add(header);

                if (open)
                {
                    VisualElement detail = new();
                    detail.AddToClassList("lobby-shell__player-detail");

                    VisualElement meta = new();
                    meta.AddToClassList("lobby-shell__player-meta");
                    meta.Add(MakePlayerMeta($"Ping: {player.Ping}"));
                    meta.Add(MakePlayerMeta($"Joined: {player.JoinTime}"));
                    meta.Add(MakePlayerMeta($"Account age: {player.AccountAge}"));
                    detail.Add(meta);

                    VisualElement actions = new();
                    actions.AddToClassList("lobby-shell__player-actions");
                    if (_adminActive)
                    {
                        actions.Add(MakePlayerAction("Kick", id, "kick"));
                        actions.Add(MakePlayerAction("Ban", id, "ban"));
                    }

                    actions.Add(MakePlayerAction("Mute", id, "mute"));
                    detail.Add(actions);
                    card.Add(detail);
                }

                list.Add(card);
            }

            parent.Add(list);
        }

        private static string RankBadgeLabel(string rank)
        {
            if (rank.Equals("admin", StringComparison.Ordinal))
            {
                return "ADMIN";
            }

            if (rank.Equals("mentor", StringComparison.Ordinal))
            {
                return "MENTOR";
            }

            return rank.ToUpperInvariant();
        }

        private static Label MakePlayerMeta(string text)
        {
            Label label = new(text);
            label.AddToClassList("lobby-shell__player-meta-item");
            label.AddToClassList("font-terminal");
            return label;
        }

        private Button MakePlayerAction(string label, string playerId, string action)
        {
            Button btn = new(() => Debug.Log($"[Lobby] Player action stub: {action} → {playerId}"))
            {
                text = label.ToUpperInvariant(),
            };
            btn.AddToClassList("lobby-shell__player-action");
            btn.AddToClassList("font-titling");
            btn.RegisterCallback<ClickEvent>(evt => evt.StopPropagation());
            return btn;
        }

        private void BuildSettingsTab(VisualElement parent)
        {
            VisualElement subTabs = new();
            subTabs.AddToClassList("lobby-shell__settings-tabs");
            AddSettingsSubTab(subTabs, "display", "Display");
            AddSettingsSubTab(subTabs, "sound", "Audio");
            AddSettingsSubTab(subTabs, "keyboard", "Controls");
            parent.Add(subTabs);

            ScrollView scroll = new(ScrollViewMode.Vertical);
            scroll.AddToClassList("lobby-shell__settings-scroll");
            if (_settingsScroll != null && _settingsScrollSubTab == _settingsSubTab)
            {
                Vector2 offset = _settingsScroll.scrollOffset;
                // The new content has no layout yet; restore once it does, or the offset clamps to zero.
                scroll.RegisterCallbackOnce<GeometryChangedEvent>(_ => scroll.scrollOffset = offset);
            }
            _settingsScroll = scroll;
            _settingsScrollSubTab = _settingsSubTab;
            parent.Add(scroll);
            VisualElement form = new();
            form.AddToClassList("lobby-shell__settings-form");
            scroll.Add(form);
            switch (_settingsSubTab)
            {
                case "sound":
                {
                    form.Add(SettingsToggle("muted", "Mute All Audio", _audio.MuteAll, value => MuteAllChanged?.Invoke(value)));
                    form.Add(SettingsToggle("muteUnfocused", "Mute When Unfocused", _audio.MuteWhenUnfocused,
                        value => MuteWhenUnfocusedChanged?.Invoke(value)));
                    foreach ((PlayerVolume volume, string key, string title) in VolumeSliders)
                    {
                        PlayerVolume target = volume;
                        form.Add(SettingsSlider(key, title, true, !_audio.MuteAll, _audio.Get(volume), value =>
                        {
                            _raisingAudioIntent = true;
                            try { VolumeChanged?.Invoke(target, value); }
                            finally { _raisingAudioIntent = false; }
                        }));
                    }
                    form.Add(AudioResetButton());
                    break;
                }
                case "keyboard":
                {
                    BuildControlsForm(form);
                    break;
                }
                default:
                {
                    BuildDisplayForm(form);
                    break;
                }
            }
        }

        private static Label SettingsLabel(string text)
        {
            Label label = MakeLabel(text.ToUpperInvariant(), "font-body");
            label.AddToClassList("lobby-shell__settings-label");
            return label;
        }

        private static Toggle SettingsToggle(string key, string label, bool value, Action<bool> changed)
        {
            Toggle toggle = new() { text = label, name = "settings-" + key, value = value };
            toggle.AddToClassList("lobby-shell__settings-toggle");
            toggle.AddToClassList("font-body");
            toggle.RegisterValueChangedCallback(evt => changed(evt.newValue));
            return toggle;
        }

        private static readonly string[] WindowModeLabels = { "Fullscreen", "Borderless", "Windowed" };
        private static readonly string[] QualityLabels = { "Low", "Medium", "High", "Ultra" };
        private static readonly string[] UpscalerLabels = { "STP", "FSR 1", "Bilinear" };
        private static readonly string[] AntiAliasingLabels = { "Off", "FXAA", "SMAA", "TAA" };

        private void BuildDisplayForm(VisualElement form)
        {
            Vector2Int[] resolutions = _resolutions.Length > 0 ? _resolutions : new Vector2Int[] { _currentResolution };
            form.Add(SettingsDropdown("resolution", "Resolution", resolutions.Select(r => $"{r.x} × {r.y}").ToList(),
                Array.IndexOf(resolutions, _currentResolution), i => ResolutionChanged?.Invoke(resolutions[i])));
            form.Add(SettingsDropdown("windowMode", "Window Mode", WindowModeLabels.ToList(), (int)_display.WindowMode,
                i => WindowModeChanged?.Invoke((PlayerWindowMode)i)));
            form.Add(SettingsToggle("vsync", "VSync", _display.VSync, value => VSyncChanged?.Invoke(value)));
            int[] caps = PlayerDisplaySettings.FrameCaps;
            form.Add(SettingsDropdown("frameCap", "Frame Cap", caps.Select(c => c == 0 ? "Off" : c.ToString()).ToList(),
                Array.IndexOf(caps, _display.FrameCap), i => FrameCapChanged?.Invoke(caps[i]), enabled: !_display.VSync,
                tooltip: _display.VSync ? "VSync paces frames while it is on." : null));
            form.Add(SettingsDropdown("quality", "Quality", QualityLabels.ToList(), (int)_display.Quality,
                i => QualityChanged?.Invoke((PlayerQualityTier)i)));
            float[] scales = PlayerDisplaySettings.RenderScales;
            form.Add(SettingsDropdown("renderScale", "Render Scale", scales.Select(f => $"{Mathf.RoundToInt(f * 100f)} %").ToList(),
                Array.IndexOf(scales, _display.RenderScale), i => RenderScaleChanged?.Invoke(scales[i])));
            RenderPlan plan = PlayerDisplayPlan.Resolve(_display, _stpSupported);
            bool upscaling = _display.RenderScale < 1f;
            string upscalerHint = null;
            if (!upscaling) upscalerHint = "Upscaling applies below 100 % render scale.";
            else if (_display.Upscaler == PlayerUpscaler.Stp && !_stpSupported) upscalerHint = "STP is not supported on this GPU; FSR 1 is used.";
            form.Add(SettingsDropdown("upscaler", "Upscaler", UpscalerLabels.ToList(), (int)_display.Upscaler,
                i => UpscalerChanged?.Invoke((PlayerUpscaler)i), enabled: upscaling, tooltip: upscalerHint));
            form.Add(SettingsDropdown("antiAliasing", "Anti-Aliasing", AntiAliasingLabels.ToList(),
                (int)(plan.AntiAliasingLocked ? plan.AntiAliasing : _display.AntiAliasing),
                i => AntiAliasingChanged?.Invoke((PlayerAntiAliasing)i), enabled: !plan.AntiAliasingLocked,
                tooltip: plan.AntiAliasingLocked ? "STP upscaling requires TAA." : null));
            form.Add(UiScaleSlider());
            form.Add(SettingsToggle("showFps", "Show FPS", _display.ShowFps, value => ShowFpsChanged?.Invoke(value)));
            form.Add(DisplayResetButton());
        }

        private VisualElement SettingsDropdown(string key, string title, List<string> choices, int index, Action<int> changed,
            bool enabled = true, string tooltip = null)
        {
            VisualElement field = new();
            field.Add(SettingsLabel(title));
            DropdownField dropdown = new(choices, Mathf.Clamp(index, 0, choices.Count - 1)) { name = "settings-" + key };
            dropdown.AddToClassList("lobby-shell__settings-dropdown");
            dropdown.AddToClassList("font-body");
            dropdown.RegisterValueChangedCallback(_ =>
            {
                if (dropdown.index >= 0) changed(dropdown.index);
            });
            StyleDarkTextField(dropdown);
            dropdown.SetEnabled(enabled);
            if (tooltip != null) field.tooltip = tooltip;
            field.Add(dropdown);
            return field;
        }

        /// <summary>UI Scale applies live, one 1 % step at a time (owner's call, overriding Decision 8's apply-on-release).</summary>
        private VisualElement UiScaleSlider() => SettingsSlider("uiScale", "UI Scale", true, true, _display.UiScale, value =>
        {
            if (Mathf.Approximately(value, _display.UiScale)) return;
            _raisingDisplayIntent = true;
            try { UiScaleChanged?.Invoke(value); }
            finally { _raisingDisplayIntent = false; }
        }, PlayerDisplaySettings.MinUiScale, PlayerDisplaySettings.MaxUiScale, PlayerDisplaySettings.UiScaleStep);

        /// <summary>
        /// Controls tab (Decisions 9–13): one heading per group in row order, a button per binding (read-only rows
        /// disabled) and Reset to defaults; rebind notices show as a toast.
        /// </summary>
        private void BuildControlsForm(VisualElement form)
        {
            string group = null;
            foreach (KeybindRow keybind in _keybindRows)
            {
                if (keybind.Group == null) continue;
                if (keybind.Group != group)
                {
                    group = keybind.Group;
                    form.Add(SettingsLabel(group));
                }

                VisualElement row = new();
                row.AddToClassList("lobby-shell__binding-row");
                Label label = MakeLabel(keybind.Label, "font-body");
                label.AddToClassList("lobby-shell__binding-label");
                row.Add(label);
                string rowId = keybind.Id;
                bool listening = _capturingRowId == rowId;
                Button binding = new(() =>
                {
                    if (_capturingRowId == rowId) KeyCaptureCancelled?.Invoke();
                    else KeyCaptureRequested?.Invoke(rowId);
                })
                {
                    name = "bind-" + rowId,
                    text = listening ? "Press a key… (Esc cancels)" : KeybindRules.DisplayName(keybind.Path, keybind.ModifierPath),
                };
                binding.AddToClassList("lobby-shell__binding-button");
                binding.AddToClassList("font-terminal");
                binding.EnableInClassList("lobby-shell__binding-button--listening", listening);
                binding.SetEnabled(!keybind.ReadOnly);
                if (keybind.ReadOnly) binding.tooltip = "This binding is fixed.";
                row.Add(binding);
                form.Add(row);
            }

            form.Add(ResetButton("settings-reset-controls", _confirmingControlsReset, () => _confirmingControlsReset = true, () =>
            {
                _confirmingControlsReset = false;
                ControlsResetRequested?.Invoke();
            }));
        }

        private Button DisplayResetButton() => ResetButton("settings-reset-display", _confirmingDisplayReset,
            () => _confirmingDisplayReset = true, () =>
            {
                _confirmingDisplayReset = false;
                DisplayResetRequested?.Invoke();
            });

        private VisualElement BuildDisplayPrompt()
        {
            _displayPrompt = new VisualElement { name = "settings-display-prompt" };
            _displayPrompt.AddToClassList("lobby-shell__display-prompt");
            _displayPrompt.style.display = DisplayStyle.None;
            VisualElement card = new();
            card.AddToClassList("lobby-shell__display-prompt-card");
            _displayPromptText = MakeLabel("", "font-body");
            _displayPromptText.AddToClassList("lobby-shell__display-prompt-text");
            card.Add(_displayPromptText);
            VisualElement row = new();
            row.AddToClassList("lobby-shell__actions-row");
            Button keep = new(() => DisplayChangeKept?.Invoke()) { name = "settings-display-keep", text = "KEEP" };
            keep.AddToClassList("lobby-shell__btn");
            keep.AddToClassList("lobby-shell__btn--prompt-keep");
            keep.AddToClassList("font-titling");
            Button revert = new(() => DisplayChangeReverted?.Invoke()) { name = "settings-display-revert", text = "REVERT" };
            revert.AddToClassList("lobby-shell__btn");
            revert.AddToClassList("lobby-shell__btn--observe");
            revert.AddToClassList("font-titling");
            row.Add(keep);
            row.Add(revert);
            card.Add(row);
            _displayPrompt.Add(card);
            return _displayPrompt;
        }

        /// <summary>Reset to defaults asks once more before acting (Decision 13).</summary>
        private Button AudioResetButton() => ResetButton("settings-reset-audio", _confirmingAudioReset,
            () => _confirmingAudioReset = true, () =>
            {
                _confirmingAudioReset = false;
                AudioResetRequested?.Invoke();
            });

        private Button ResetButton(string name, bool confirming, Action arm, Action reset)
        {
            Button button = null;
            button = new Button(() =>
            {
                if (!confirming)
                {
                    arm();
                    confirming = true;
                    button.text = "CONFIRM RESET";
                    return;
                }
                reset();
                ShowTab(TabSettings);
            })
            {
                name = name,
                text = confirming ? "CONFIRM RESET" : "RESET TO DEFAULTS",
            };
            button.AddToClassList("lobby-shell__btn");
            button.AddToClassList("lobby-shell__btn--settings-reset");
            button.AddToClassList("font-titling");
            return button;
        }

        private VisualElement SettingsSlider(string key, string title, bool percentage, bool enabled, float value,
            Action<float> changed, float min = 0f, float max = 1f, float step = 0f)
        {
            VisualElement field = new();
            field.AddToClassList("lobby-shell__settings-slider-field");
            field.Add(SettingsLabel(title));
            VisualElement row = new();
            row.AddToClassList("lobby-shell__settings-slider-row");
            Slider slider = new(min, max) { name = "settings-" + key, value = value };
            slider.AddToClassList("lobby-shell__settings-slider");
            VisualElement fill = new() { pickingMode = PickingMode.Ignore };
            fill.AddToClassList("lobby-shell__settings-slider-fill");
            fill.style.width = Length.Percent((slider.value - min) / (max - min) * 100f);
            slider.Q(className: "unity-base-slider__tracker").Add(fill);
            string Display(float value) => percentage ? Mathf.RoundToInt(value * 100f).ToString()
                : value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            Label number = MakeLabel(Display(slider.value), "font-terminal");
            number.name = "settings-value-" + key;
            number.AddToClassList("lobby-shell__settings-number");
            slider.RegisterValueChangedCallback(evt =>
            {
                float value = evt.newValue;
                if (step > 0f)
                {
                    value = Mathf.Clamp((float)Math.Round(Mathf.Round(value / step) * step, 2), min, max);
                    slider.SetValueWithoutNotify(value);
                }
                changed(value);
                number.text = Display(value);
                fill.style.width = Length.Percent((value - min) / (max - min) * 100f);
            });
            slider.SetEnabled(enabled);
            row.Add(slider);
            row.Add(number);
            field.Add(row);
            return field;
        }

        private void AddSettingsSubTab(VisualElement parent, string key, string label)
        {
            Button btn = new(() =>
            {
                CancelKeyCapture();
                _confirmingAudioReset = false;
                _confirmingDisplayReset = false;
                _confirmingControlsReset = false;
                _settingsSubTab = key;
                ShowTab(TabSettings);
            })
            {
                name = "settings-tab-" + key,
                text = label.ToUpperInvariant(),
            };
            btn.AddToClassList("lobby-shell__settings-tab");
            btn.AddToClassList("font-titling");
            btn.EnableInClassList("lobby-shell__settings-tab--active", _settingsSubTab == key);
            parent.Add(btn);
        }

        private static VisualElement SettingRow(string label, string value)
        {
            VisualElement row = new();
            row.AddToClassList("lobby-shell__setting-row");
            Label left = new(label);
            left.AddToClassList("lobby-shell__setting-label");
            left.AddToClassList("font-body");
            Label right = new(value);
            right.AddToClassList("lobby-shell__setting-value");
            right.AddToClassList("font-terminal");
            row.Add(left);
            row.Add(right);
            return row;
        }

        private static Button AdminButton(string label, Action onClick, string extraClass = null)
        {
            Button btn = new(onClick) { text = label.ToUpperInvariant() };
            btn.AddToClassList("lobby-shell__admin-btn");
            btn.AddToClassList("font-titling");
            if (!string.IsNullOrEmpty(extraClass))
            {
                btn.AddToClassList(extraClass);
            }

            return btn;
        }

        private void ToggleReady()
        {
            ReadyRequested?.Invoke();
        }

        private void ToggleAdmin()
        {
            _adminActive = !_adminActive;
            if (!_adminActive && _activeTab == TabServerSettings)
            {
                _activeTab = TabServerInfo;
            }
            else if (_adminActive)
            {
                _activeTab = TabServerSettings;
            }

            RefreshTabs();
            ShowTab(_activeTab);
            RefreshSidebar();
        }

        private static Label SectionTitle(string text)
        {
            Label label = new(text);
            label.AddToClassList("lobby-shell__section-title");
            label.AddToClassList("font-titling");
            return label;
        }

        private static Label MakePlaceholder(string text)
        {
            Label label = new(text);
            label.AddToClassList("lobby-shell__placeholder");
            label.AddToClassList("font-body");
            return label;
        }

        private void HoldTextEntry(TextField field)
        {
            field.RegisterCallback<FocusInEvent>(_ => _textEntry.Enter());
            field.RegisterCallback<FocusOutEvent>(_ => _textEntry.Exit());
        }

        /// <summary>
        /// UITK default theme paints TextField inputs white; force the dark inset fill on the
        /// inner input element (class names vary slightly across Unity 6 builds).
        /// </summary>
        private static void StyleDarkTextField(BaseField<string> field)
        {
            if (field == null)
            {
                return;
            }

            Color fill = new(0.1647059f, 0.1647059f, 0.1803922f, 1f); // #2A2A2E
            field.style.backgroundColor = fill;

            void Apply()
            {
                VisualElement input = field.Q(className: "unity-base-text-field__input")
                    ?? field.Q(className: "unity-text-field__input")
                    ?? field.Q(className: "unity-base-field__input");
                if (input != null)
                {
                    input.style.backgroundColor = fill;
                    input.style.color = Color.white;
                    input.style.borderTopWidth = 0;
                    input.style.borderRightWidth = 0;
                    input.style.borderBottomWidth = 0;
                    input.style.borderLeftWidth = 0;
                }
            }

            Apply();
            field.RegisterCallback<AttachToPanelEvent>(_ => Apply());
        }

        private static Label MakeLabel(string text, string fontClass, string colorClass = null)
        {
            Label label = new(text);
            label.AddToClassList(fontClass);
            if (!string.IsNullOrEmpty(colorClass))
            {
                label.AddToClassList(colorClass);
            }

            return label;
        }

        private static Label MakeSep()
        {
            Label sep = new("•");
            sep.AddToClassList("lobby-shell__header-sep");
            sep.AddToClassList("font-body");
            return sep;
        }

        private static Label MakeMeta(string text)
        {
            Label label = new(text);
            label.AddToClassList("lobby-shell__header-meta");
            label.AddToClassList("font-body");
            return label;
        }
    }
}
