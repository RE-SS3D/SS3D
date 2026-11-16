using System;
using System.Collections.Generic;
using SS3D.Systems.Settings;
using UnityEngine;
using UnityEngine.UIElements;
using static SS3D.UI.SettingsPanel.SettingsUi;

namespace SS3D.UI.SettingsPanel
{
    /// <summary>
    /// The one player Settings screen (Display, Audio, Controls), shared by the main menu and the lobby
    /// (main-menu.md §2: "the same Settings screen as the in-game Escape menu"). Reads and writes
    /// <see cref="PlayerSettingsSubSystem"/> directly: every control raises a "set X" intent there and the rows
    /// update in place on <see cref="PlayerSettingsSubSystem.Changed"/>, so a slider drag is never rebuilt away.
    /// The host gives it an element to fill and a full-screen element for the "Keep these settings?" prompt.
    /// </summary>
    public sealed partial class PlayerSettingsPanel : IDisposable
    {
        public const string StyleResource = "PlayerSettingsPanel";

        private const string TabDisplay = "display";
        private const string TabAudio = "audio";
        private const string TabControls = "controls";
        private const int NoticeMilliseconds = 4500;
        private const int ResetArmMilliseconds = 4000;
        private const int TickMilliseconds = 300;

        private static readonly (string Key, string Label)[] Tabs =
        {
            (TabDisplay, "Display"),
            (TabAudio, "Audio"),
            (TabControls, "Controls"),
        };

        private static readonly Dictionary<string, string> Intros = new()
        {
            [TabDisplay] = "Changes apply right away. Resolution and window mode ask before they stick, and revert after 15 s if not kept.",
            [TabAudio] = "Changes apply right away.",
            [TabControls] = "Click an action, then press a key or mouse button 3–5. Esc or a second click cancels. Taking a key another action uses swaps the two.",
        };

        private readonly PlayerSettingsSubSystem _settings;
        private readonly VisualElement _root;
        private readonly VisualElement _prompt;
        private readonly List<Button> _tabButtons = new();
        private readonly List<Action> _refreshers = new();
        private readonly Label _contextLabel;
        private readonly Label _intro;
        private readonly Button _reset;
        private readonly VisualElement _mutedBanner;
        private readonly VisualElement _body;
        private readonly ScrollView _scroll;
        private readonly VisualElement _notice;
        private readonly Label _noticeText;
        private readonly IVisualElementScheduledItem _ticker;
        private Label _promptWhat;
        private Label _promptPrevious;
        private Label _promptSeconds;
        private VisualElement _promptBar;

        private string _tab = TabDisplay;
        private bool _resetArmed;
        private IVisualElementScheduledItem _disarm;
        private IVisualElementScheduledItem _noticeHide;
        private bool _promptShown;
        private bool _promptFromReset;
        private bool _keptHere;
        private string _revertText;
        private int _noticeVersion;
        private int _captureEndedFrame = -10;
        private string _lastCapturing;
        private bool _disposed;
        private SettingsControlsTab _controls;

        /// <summary>Back (the header button, or Esc with nothing else to cancel).</summary>
        public event Action BackRequested;

        /// <param name="host">Filled by the panel.</param>
        /// <param name="modalHost">Covers the screen; holds the "Keep these settings?" prompt.</param>
        /// <param name="contextLabel">Where Settings was opened from ("Main menu"), shown in the header.</param>
        /// <param name="showHeader">False when the host already titles the screen (the lobby's Settings tab).</param>
        public PlayerSettingsPanel(VisualElement host, VisualElement modalHost, PlayerSettingsSubSystem settings,
            string contextLabel, bool showHeader)
        {
            _settings = settings;
            StyleSheet style = Resources.Load<StyleSheet>(StyleResource);

            _root = new VisualElement { name = "settings-panel" };
            _root.AddToClassList("settings-theme");
            _root.AddToClassList("settings-panel");
            if (style != null) _root.styleSheets.Add(style);
            else Debug.LogWarning($"[Settings] Resources/{StyleResource}.uss is missing; the Settings screen is unstyled.");

            if (showHeader)
            {
                VisualElement header = Element(_root, "settings-panel__header");
                Text(header, "SETTINGS", "settings-panel__title");
                _contextLabel = Text(header, contextLabel ?? "", "settings-panel__context");
                Element(header, "settings-panel__spacer");
                Text(header, "Esc", "settings-panel__mono settings-panel__esc");
                ActionButton(header, "settings-back", "BACK", () => BackRequested?.Invoke());
            }

            VisualElement tabBar = Element(_root, "settings-panel__tabs");
            foreach ((string key, string label) in Tabs)
            {
                string tab = key;
                Button button = new(() => SelectTab(tab)) { name = "settings-tab-" + key, text = label.ToUpperInvariant() };
                button.AddToClassList("settings-panel__tab");
                tabBar.Add(button);
                _tabButtons.Add(button);
            }

            _scroll = new ScrollView(ScrollViewMode.Vertical) { name = "settings-scroll" };
            _scroll.AddToClassList("settings-panel__scroll");
            _scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _root.Add(_scroll);
            VisualElement content = Element(_scroll, "settings-panel__content");

            VisualElement introRow = Element(content, "settings-panel__intro-row");
            _intro = Text(introRow, "", "settings-panel__intro");
            _reset = ActionButton(introRow, "settings-reset", "", HandleReset);

            _mutedBanner = Element(content, "settings-panel__banner");
            Text(_mutedBanner, "All sound is muted. The levels below are kept and can still be changed.", "settings-panel__banner-text");

            _body = Element(content, "settings-panel__body");

            _notice = Element(_root, "settings-panel__notice");
            _notice.name = "settings-notice";
            Element(_notice, "settings-panel__notice-dot");
            _noticeText = Text(_notice, "", "settings-panel__notice-text");
            _notice.style.display = DisplayStyle.None;

            _root.RegisterCallback<PointerDownEvent>(evt => _controls?.HandlePointerDown(evt), TrickleDown.TrickleDown);
            _root.RegisterCallback<DetachFromPanelEvent>(_ => CancelTransient());
            host.Add(_root);

            _prompt = BuildPrompt(style);
            modalHost.Add(_prompt);
            // On the prompt, which stays attached while the lobby shows another tab: the countdown still has to run.
            _ticker = _prompt.schedule.Execute(Tick).Every(TickMilliseconds);

            _noticeVersion = _settings.ControlsNoticeVersion;
            _settings.Changed += Refresh;
            ShowTab();
        }

        /// <summary>The header's "opened from" label; the lobby switches it when the shell becomes the Escape menu.</summary>
        public string ContextLabel
        {
            set { if (_contextLabel != null) _contextLabel.text = value ?? ""; }
        }

        public bool IsCapturingKey => _settings.CapturingRowId != null;

        /// <summary>
        /// Escape, as the host received it. Cancels a key capture (the capture itself already saw it), reverts a
        /// pending display change, or else asks to go back. Returns false only when the host should treat Escape as
        /// its own (the lobby's embedded panel has no Back).
        /// </summary>
        public bool ConsumeEscape()
        {
            if (IsCapturingKey || Time.frameCount - _captureEndedFrame <= 1) return true;
            if (_settings.DisplayConfirmationPending)
            {
                _settings.RevertDisplayChange();
                return true;
            }
            if (BackRequested == null) return false;
            BackRequested.Invoke();
            return true;
        }

        /// <summary>Stops listening for a key and disarms Reset; called when the panel is hidden.</summary>
        public void CancelTransient()
        {
            if (_disposed) return;
            if (IsCapturingKey) _settings.CancelKeyCapture();
            Disarm();
        }

        public void Dispose()
        {
            if (_disposed) return;
            CancelTransient();
            _disposed = true;
            _settings.Changed -= Refresh;
            _ticker?.Pause();
            _root.RemoveFromHierarchy();
            _prompt.RemoveFromHierarchy();
            BackRequested = null;
        }

        private void SelectTab(string tab)
        {
            if (_tab == tab) return;
            CancelTransient();
            _tab = tab;
            ShowTab();
            _scroll.scrollOffset = Vector2.zero;
        }

        private void ShowTab()
        {
            for (int i = 0; i < Tabs.Length; i++)
                _tabButtons[i].EnableInClassList("settings-panel__tab--active", Tabs[i].Key == _tab);
            _intro.text = Intros[_tab];
            _body.Clear();
            _refreshers.Clear();
            _controls = null;
            if (_tab == TabAudio) BuildAudio();
            else if (_tab == TabControls) _controls = new SettingsControlsTab(_settings, _body);
            else BuildDisplay();
            Refresh();
        }

        private void Refresh()
        {
            if (_disposed) return;
            string capturing = _settings.CapturingRowId;
            if (_lastCapturing != null && capturing == null) _captureEndedFrame = Time.frameCount;
            _lastCapturing = capturing;

            if (_settings.ControlsNoticeVersion != _noticeVersion)
            {
                _noticeVersion = _settings.ControlsNoticeVersion;
                if (!string.IsNullOrEmpty(_settings.ControlsNotice))
                    Say(_settings.ControlsNoticeRefused ? NoticeKind.Refused : NoticeKind.Swap, _settings.ControlsNotice + ".");
            }

            _mutedBanner.style.display = _tab == TabAudio && _settings.Settings.Audio.MuteAll ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshReset();
            foreach (Action refresh in _refreshers) refresh();
            _controls?.Refresh();
            RefreshPrompt();
        }

        private void Tick()
        {
            RefreshPrompt();
            _controls?.PulseListening();
        }

        // ---- Reset to defaults (Decision 13: the second click resets) ----

        private void HandleReset()
        {
            if (!_resetArmed)
            {
                _resetArmed = true;
                RefreshReset();
                _disarm?.Pause();
                _disarm = _root.schedule.Execute(Disarm).StartingIn(ResetArmMilliseconds);
                return;
            }

            Disarm();
            string name = Array.Find(Tabs, t => t.Key == _tab).Label;
            if (_tab == TabAudio)
            {
                _settings.ResetAudio();
            }
            else if (_tab == TabControls)
            {
                if (_controls != null) _controls.SuppressFlash = true;
                try { _settings.ResetControls(); }
                finally { if (_controls != null) _controls.SuppressFlash = false; }
            }
            else
            {
                _promptFromReset = true;
                _settings.ResetDisplay();
            }
            Say(NoticeKind.Info, name + " reset to defaults.");
        }

        private void Disarm()
        {
            _disarm?.Pause();
            _disarm = null;
            if (!_resetArmed) return;
            _resetArmed = false;
            RefreshReset();
        }

        private void RefreshReset()
        {
            string name = Array.Find(Tabs, t => t.Key == _tab).Label;
            _reset.text = _resetArmed ? "CLICK AGAIN TO RESET" : $"RESET {name.ToUpperInvariant()} TO DEFAULTS";
            _reset.EnableInClassList("settings-panel__button--amber", _resetArmed);
        }

        // ---- Notices ----

        private enum NoticeKind { Info = 0, Swap = 1, Refused = 2 }

        private void Say(NoticeKind kind, string text)
        {
            _noticeText.text = text;
            _notice.EnableInClassList("settings-panel__notice--swap", kind == NoticeKind.Swap);
            _notice.EnableInClassList("settings-panel__notice--refused", kind == NoticeKind.Refused);
            _notice.style.display = DisplayStyle.Flex;
            _noticeHide?.Pause();
            _noticeHide = _root.schedule.Execute(() => _notice.style.display = DisplayStyle.None).StartingIn(NoticeMilliseconds);
        }

        // ---- "Keep these settings?" (Decision 3) ----

        private VisualElement BuildPrompt(StyleSheet style)
        {
            VisualElement scrim = new() { name = "settings-keep-prompt", pickingMode = PickingMode.Position };
            scrim.AddToClassList("settings-theme");
            scrim.AddToClassList("settings-prompt");
            if (style != null) scrim.styleSheets.Add(style);
            scrim.style.display = DisplayStyle.None;

            VisualElement card = Element(scrim, "settings-prompt__card");
            VisualElement header = Element(card, "settings-panel__header settings-prompt__header");
            Text(header, "KEEP THESE SETTINGS?", "settings-panel__title");
            VisualElement body = Element(card, "settings-prompt__body");
            _promptWhat = Text(body, "", "settings-prompt__what");
            VisualElement line = Element(body, "settings-prompt__line");
            Text(line, "Reverting to ", "settings-prompt__secondary");
            _promptPrevious = Text(line, "", "settings-panel__mono settings-prompt__value");
            Text(line, " in ", "settings-prompt__secondary");
            _promptSeconds = Text(line, "", "settings-panel__mono settings-prompt__value");
            Text(line, " unless kept.", "settings-prompt__secondary");
            VisualElement track = Element(body, "settings-prompt__track");
            _promptBar = Element(track, "settings-prompt__bar");
            VisualElement actions = Element(body, "settings-prompt__actions");
            ActionButton(actions, "settings-keep-revert", "REVERT", () => _settings.RevertDisplayChange());
            Button keep = ActionButton(actions, "settings-keep-keep", "KEEP", () =>
            {
                _keptHere = true;
                _settings.ConfirmDisplayChange();
            });
            keep.AddToClassList("settings-panel__button--accent");
            return scrim;
        }

        private void RefreshPrompt()
        {
            if (_disposed) return;
            bool pending = _settings.TryGetDisplayRevertTarget(out Vector2Int previousSize, out PlayerWindowMode previousMode);
            if (!pending)
            {
                if (_promptShown)
                {
                    _promptShown = false;
                    _prompt.style.display = DisplayStyle.None;
                    if (!_keptHere && _revertText != null) Say(NoticeKind.Info, "Display reverted to " + _revertText + ".");
                }
                _keptHere = false;
                _promptFromReset = false;
                return;
            }

            PlayerDisplaySettings display = _settings.Settings.Display;
            Vector2Int size = _settings.CurrentResolution;
            bool sizeChanged = size != previousSize;
            bool modeChanged = display.WindowMode != previousMode;
            string now = ResolutionText(size) + ", " + ModeText(display.WindowMode);
            string before = ResolutionText(previousSize) + ", " + ModeText(previousMode);
            if (_promptFromReset) _promptWhat.text = "Display reset: " + now + ".";
            else if (sizeChanged && !modeChanged) _promptWhat.text = "Resolution is now " + ResolutionText(size) + ".";
            else if (modeChanged && !sizeChanged) _promptWhat.text = "Window mode is now " + ModeText(display.WindowMode) + ".";
            else _promptWhat.text = "Display is now " + now + ".";
            string previous = sizeChanged ? ResolutionText(previousSize) : ModeText(previousMode);
            _promptPrevious.text = _promptFromReset || (sizeChanged && modeChanged) ? before : previous;
            _revertText = before;

            float left = _settings.DisplayConfirmationSecondsLeft;
            _promptSeconds.text = Mathf.CeilToInt(left) + " s";
            _promptBar.style.width = Length.Percent(Mathf.Clamp01((left - 1f) / 15f) * 100f);
            if (!_promptShown)
            {
                _promptShown = true;
                _keptHere = false;
                _prompt.style.display = DisplayStyle.Flex;
                _prompt.BringToFront();
                CancelTransient();
            }
        }
    }
}
