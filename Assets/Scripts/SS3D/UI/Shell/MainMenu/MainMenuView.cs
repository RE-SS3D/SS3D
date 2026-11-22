using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace SS3D.UI.Shell.MainMenu
{
    /// <summary>
    /// Presentation of the main-menu design. Attach explicitly to a shell layer. The view has no
    /// gameplay effects of its own: buttons raise <see cref="CommandIssued"/> and edits raise
    /// <see cref="FieldChanged"/>; the owner (<c>MainMenuSubSystem</c>, or the editor preview, which
    /// ignores them) applies them and calls <see cref="Present"/> again.
    /// </summary>
    /// <remarks>
    /// The look is the UI bible's (MainMenu.uss): every colour is a USS class, so the view only picks classes. The menu
    /// is laid out at the bible's 1200 × 675 reference and scaled to fill the screen (<see cref="UiReferenceScale"/>).
    /// Motion is <see cref="UiMotion"/>'s: a part that appears eases in from its hidden look, which it waits in while
    /// out of sight (<see cref="Prime"/>), so the entrance starts there however the part was hidden.
    /// </remarks>
    public sealed class MainMenuView : IUiSurface
    {
        private const string HiddenClass = "main-menu__hidden";
        private const string SpinClass = "main-menu__spin";
        private const string EnterLeft = "ui-enter--left";
        private const string EnterRight = "ui-enter--right";
        private const string EnterScale = "ui-enter--scale";
        private const string WallpaperDriftClass = "main-menu__wallpaper--drift";
        private const string WallpaperStillClass = "main-menu__wallpaper--still";
        private const string DirectSectionClass = "main-menu__section--direct";

        /// <summary>Where PlayerSettingsPanel keeps its groups; found by class, the view does not know that type.</summary>
        private const string SettingsBodyClass = "settings-panel__body";

        private const long SidebarStepMs = 40;
        private const long SidebarIntroFallbackMs = 150;
        private const long RowStepMs = 35;

        /// <summary>A panel's rows start once the panel is under way.</summary>
        private const long PanelLeadMs = 110;

        private const int MaxStaggered = 10;

        /// <summary>The longest entrance (a panel's 0.35 s) and a little: then an element gets its own transitions back.</summary>
        private const long SettleMs = 420;

        /// <summary>A row this far into its entrance when the list is rebuilt shows at once rather than starting over.</summary>
        private const float CatchUpMs = 90f;

        /// <summary>One leg of the wallpaper's drift; MainMenu.uss eases each leg over as long.</summary>
        private const long DriftMs = 18000;

        public VisualElement Root { get; }

        /// <summary>Where the owner mounts the shared Settings screen; shown while <see cref="MainMenuScreen.Settings"/> is.</summary>
        public VisualElement SettingsHost => _settingsHost;
        private readonly MainMenuAssetCatalog _catalog;
        private Texture2D _blurredWallpaper;
        private MainMenuPresentation _model = new();

        /// <summary>The dialog's kinds, as <c>main-menu__overlay--*</c> classes: each sets the dialog's colour (--op).</summary>
        private static readonly string[] OperationKinds = { "host", "join", "update", "lost", "failed" };

        /// <summary>The steps last built; while it holds, a Present updates their classes in place, so transitions run.</summary>
        private string _stepsBuilt;

        private readonly VisualElement _overlay;
        private readonly VisualElement _progress;
        private readonly VisualElement _progressFill;
        private readonly VisualElement _progressGlint;
        private readonly VisualElement _wallpaper;
        private readonly VisualElement _sidePanel;
        private readonly VisualElement _settingsHost;

        /// <summary>The sidebar's rows top to bottom, each with its entrance's variant, for the sidebar's stagger.</summary>
        private readonly List<(VisualElement Row, string Variant)> _sidebarRows = new();

        /// <summary>The screen the last Present showed (none before the first): entrances play only for what is new.</summary>
        private MainMenuScreen? _shownScreen;

        /// <summary>The menu's first show waits for its first layout before the sidebar eases in.</summary>
        private bool _sidebarIntroPending;

        /// <summary>When the server list last opened; its rows' entrance is timed from here (<see cref="StaggerList"/>).</summary>
        private float _listOpenedAt = float.NegativeInfinity;

        /// <summary>Servers whose details the last Present showed, and those this one shows: newly opened details ease in.</summary>
        private readonly HashSet<string> _expandedShown = new();
        private readonly HashSet<string> _expandedNow = new();

        /// <summary>A button or link; server commands pass the server id, others an empty string.</summary>
        public event Action<MainMenuCommand, string> CommandIssued;

        /// <summary>An edit: field, server id (server fields only) and the new value.</summary>
        public event Action<MainMenuField, string, string> FieldChanged;

        public MainMenuView(MainMenuAssetCatalog catalog)
        {
            _catalog = catalog;
            Root = catalog.Template.CloneTree();
            Root.style.flexGrow = 1;
            Root.styleSheets.Add(catalog.Style);
            Root.Q("logo").style.backgroundImage = catalog.Logo;
            Root.Q("music-icon").style.backgroundImage = catalog.MusicIcon;
            UiReferenceScale.Fit(Root, Root.Q("main-menu"), () => catalog.PanelSettings != null ? catalog.PanelSettings.scale : 1f);
            // The spacer pushes music and the footer to the bottom: the column is at least as tall as its plate.
            ScrollView sidebar = Root.Q<ScrollView>("sidebar-scroll");
            sidebar.RegisterCallback<GeometryChangedEvent>(e => sidebar.contentContainer.style.minHeight = e.newRect.height);
            Command("join", MainMenuCommand.Join);
            Command("local", MainMenuCommand.PlayLocally);
            Command("advanced", MainMenuCommand.ToggleAdvanced);
            Command("settings", MainMenuCommand.Settings);
            Command("quit", MainMenuCommand.Quit);
            Command("back", MainMenuCommand.Back);
            Command("refresh", MainMenuCommand.Refresh);
            Command("discord", MainMenuCommand.Discord);
            Command("credits", MainMenuCommand.Credits);
            Command("update", MainMenuCommand.Update);
            Command("cancel", MainMenuCommand.Cancel);
            Command("retry", MainMenuCommand.Retry);
            Command("back-to-servers", MainMenuCommand.Back);
            Command("music-toggle", MainMenuCommand.ToggleMusic);
            Command("music-next", MainMenuCommand.NextTrack);
            Slider musicVolume = Root.Q<Slider>("music-volume");
            VisualElement volumeFill = new() { name = "music-volume-fill", pickingMode = PickingMode.Ignore };
            volumeFill.AddToClassList("main-menu__music-volume-fill");
            musicVolume.Q("unity-tracker").Add(volumeFill);
            musicVolume.RegisterValueChangedCallback(e =>
            {
                UpdateVolumeFill(e.newValue);
                FieldChanged?.Invoke(MainMenuField.MusicVolume, "",
                    e.newValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            });
            Field("player-name", MainMenuField.PlayerName);
            Field("port", MainMenuField.Port);
            Field("overlay-token", MainMenuField.OperationToken);
            Root.Q<TextField>("overlay-token").RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter)) return;
                Issue(MainMenuCommand.Retry);
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
            Root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Escape) return;
                Issue(MainMenuCommand.Back);
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
            _overlay = Root.Q("overlay");
            _progress = Root.Q("overlay-progress");
            _progressFill = Root.Q("overlay-progress-fill");
            _progressGlint = Root.Q("overlay-progress-glint");
            _wallpaper = Root.Q("wallpaper");
            _sidePanel = Root.Q("side-panel");
            _settingsHost = Root.Q("settings-host");

            // Everything that eases in waits primed, so the first Present plays the menu's entrance.
            _sidebarRows.Add((Root.Q("logo"), EnterScale));
            _sidebarRows.Add((Root.Q("update-banner"), EnterLeft));
            _sidebarRows.Add((Root.Q(className: "main-menu__identity"), EnterLeft));
            foreach (VisualElement row in Root.Q(className: "main-menu__navigation").Children()) _sidebarRows.Add((row, EnterLeft));
            _sidebarRows.Add((Root.Q("music"), EnterLeft));
            _sidebarRows.Add((Root.Q(className: "main-menu__footer"), EnterLeft));
            foreach ((VisualElement row, string variant) in _sidebarRows) Prime(row, variant);
            Prime(_sidePanel, EnterRight);
            Prime(_settingsHost, EnterRight);

            Root.schedule.Execute(Animate).Every(16);
            Root.schedule.Execute(Drift).Every(DriftMs);
        }

        /// <summary>Spins what is working (the dialog's step, a server being checked) and runs the progress bar's glint.</summary>
        private void Animate()
        {
            if (Root.panel == null) return;
            float time = Time.realtimeSinceStartup;
            Rotate spin = new(new Angle(time * 300f % 360f, AngleUnit.Degree));
            Root.Query(className: SpinClass).ForEach(element => element.style.rotate = spin);
            if (!_progress.ClassListContains("main-menu__progress--working")) return;
            float width = _progressFill.layout.width;
            if (width <= 0f || float.IsNaN(width)) return;
            const float Glint = 60f;
            _progressGlint.style.left = (time * 220f % (width + Glint)) - Glint;
        }

        /// <summary>
        /// The one ambient loop: on the home screen the wallpaper drifts, a class toggled every <see cref="DriftMs"/>
        /// that MainMenu.uss eases over as long, so it never rests. On other screens the current leg ends and it waits;
        /// behind a dialog it holds still (<see cref="PresentWallpaper"/>).
        /// </summary>
        private void Drift()
        {
            if (Root.panel == null || _model.Screen != MainMenuScreen.Menu) return;
            _wallpaper.ToggleInClassList(WallpaperDriftClass);
        }

        private void UpdateVolumeFill(float value)
        {
            Root.Q("music-volume-fill").style.width = Length.Percent(Mathf.Clamp01(value) * 100f);
        }

        /// <summary>The wallpaper, blurred and still behind the operation dialogs (the blur is made once per attach).</summary>
        private void PresentWallpaper(bool operation)
        {
            if (operation && _blurredWallpaper == null) _blurredWallpaper = MainMenuWallpaper.Blur(_catalog.Wallpaper);
            _wallpaper.style.backgroundImage = operation ? _blurredWallpaper : _catalog.Wallpaper;
            _wallpaper.EnableInClassList(WallpaperStillClass, operation);
        }

        public void Attach(VisualElement layerRoot) { Detach(); layerRoot.Add(Root); }
        public void Detach()
        {
            Root.RemoveFromHierarchy();
            DestroyBlur();
        }

        private void DestroyBlur()
        {
            if (_blurredWallpaper == null) return;
            if (UnityEngine.Application.isPlaying) Object.Destroy(_blurredWallpaper);
            else Object.DestroyImmediate(_blurredWallpaper);
            _blurredWallpaper = null;
        }

        public void Present(MainMenuPresentation model)
        {
            _model = model;
            MainMenuScreen? previous = _shownScreen;
            _shownScreen = model.Screen;
            SetText("player-name", model.PlayerName);
            SetText("port", model.Port);
            Root.Q<Label>("version").text = $"v{model.Version} pre-alpha";
            Reveal(Root.Q("update-banner"), model.UpdateAvailable, EnterLeft);
            bool hasMusic = !string.IsNullOrEmpty(model.MusicTitle);
            Root.Q("music").EnableInClassList("main-menu__music--off", !model.MusicOn || !hasMusic);
            Root.Q("music-toggle").EnableInClassList("main-menu__music-toggle--on", model.MusicOn && hasMusic);
            Root.Q("music-toggle").tooltip = model.MusicOn ? "Turn music off" : "Turn music on";
            string musicTitle = model.MusicOn ? model.MusicTitle : "Off";
            Root.Q<Label>("music-title").text = hasMusic ? musicTitle : "No music";
            Root.Q<Button>("music-next").SetEnabled(hasMusic);
            Slider volume = Root.Q<Slider>("music-volume");
            if (!Mathf.Approximately(volume.value, model.MusicVolume)) volume.SetValueWithoutNotify(model.MusicVolume);
            UpdateVolumeFill(volume.value);
            Root.Q<Button>("update").text = model.CanSelfUpdate ? "Update and restart" : "Download";
            Show("update-hint", model.CanSelfUpdate);
            Root.Q<Label>("update-copy").text = $"v{model.LatestVersion} is out; this is v{model.Version}. {model.UpdateMessage}";
            bool hasName = !string.IsNullOrWhiteSpace(model.PlayerName);
            Root.Q<Button>("join").SetEnabled(hasName);
            Root.Q<Button>("local").SetEnabled(hasName);
            string joinSubtitle = "Recent, public or direct connect";
            if (!hasName) joinSubtitle = "Needs a name";
            else if (model.FirstLaunch) joinSubtitle = "Paste an address from Discord";
            else if (model.Recent.Count > 0) joinSubtitle = $"Last played: {model.Recent[0].Name}";
            Root.Q<Label>("join-subtitle").text = joinSubtitle;
            Label localSubtitle = Root.Q<Label>("local-subtitle");
            localSubtitle.text = hasName ? "Host on this computer" : "Needs a name";
            bool localError = hasName && !string.IsNullOrEmpty(model.LocalError);
            if (localError) localSubtitle.text = model.LocalError;
            localSubtitle.EnableInClassList("main-menu__nav-subtitle--error", localError);
            Show("name-hint", !hasName || model.FirstLaunch);
            Label nameHint = Root.Q<Label>("name-hint");
            nameHint.text = !hasName ? "Enter a name to join or play." : "Taken from this computer's user name. Change it any time.";
            nameHint.EnableInClassList("main-menu__hint--warning", !hasName);
            Reveal(Root.Q("port-panel"), model.Advanced, EnterLeft);
            // The port lives on its button at the end of the row, under "Port"; the button stays lit while its field is open.
            Root.Q<Label>("advanced-port").text = string.IsNullOrWhiteSpace(model.Port) ? "—" : model.Port.Trim();
            Root.Q("advanced").EnableInClassList("main-menu__advanced--open", model.Advanced);
            bool servers = model.Screen == MainMenuScreen.Servers;
            bool settings = model.Screen == MainMenuScreen.Settings;
            // Where you are: the open screen's row is the white plate with the notch; on the home screen Join holds the
            // plate as the default action.
            Root.Q("join").EnableInClassList("main-menu__nav--selected", servers);
            Root.Q("join").EnableInClassList("main-menu__button--accent", !servers && !settings);
            Root.Q("settings").EnableInClassList("main-menu__nav--selected", settings);
            bool operation = IsOperation(model.Screen);
            PresentWallpaper(operation);
            Show("sidebar", !operation);
            PresentSidebarMotion(operation, previous);
            Reveal(_sidePanel, servers, EnterRight);
            Reveal(_settingsHost, settings, EnterRight);
            PresentSettingsMotion(settings, previous != MainMenuScreen.Settings);
            Root.Q<Label>("checked").text = model.CheckedText;
            PresentRefreshSpin(servers, model);
            VisualElement content = Root.Q("server-content");
            content.Clear();
            _expandedNow.Clear();
            if (servers)
            {
                if (previous != MainMenuScreen.Servers) _listOpenedAt = UiMotion.Time;
                DirectConnect(content, model);
                Section(content, "Recent", "recent", model.Recent, true, model);
                Section(content, "Public", "public", model.Public, false, model);
                StaggerList(content);
            }

            _expandedShown.Clear();
            _expandedShown.UnionWith(_expandedNow);
            bool shown = !_overlay.ClassListContains(HiddenClass);
            Show("overlay", operation);
            if (operation)
            {
                Operation(model, !shown);
                // Shown without --in, then given it a frame later: the plate eases in.
                if (!shown) _overlay.schedule.Execute(() => _overlay.AddToClassList("main-menu__overlay--in")).ExecuteLater(30);
            }
            else if (shown)
            {
                _overlay.RemoveFromClassList("main-menu__overlay--in");
                _stepsBuilt = null;
                _progressFill.style.width = 0;
            }
        }

        private static bool IsOperation(MainMenuScreen screen) =>
            screen is MainMenuScreen.Connecting or MainMenuScreen.Hosting or MainMenuScreen.Updating;

        /// <summary>
        /// The sidebar's rows ease in one after another whenever the sidebar appears (the menu opening, a dialog
        /// closing); hidden behind a dialog, they wait primed.
        /// </summary>
        private void PresentSidebarMotion(bool operation, MainMenuScreen? previous)
        {
            if (operation)
            {
                foreach ((VisualElement row, string variant) in _sidebarRows) Prime(row, variant);
                return;
            }

            if (previous.HasValue && !IsOperation(previous.Value)) return;
            if (previous.HasValue || Root.panel == null)
            {
                _sidebarIntroPending = false;
                StaggerSidebar();
                return;
            }

            // The menu's first show: the rows ease in after its first layout, once they are styled in their hidden look
            // (UiMotion adds --in 16 ms on, which could otherwise land before the first style pass and skip the ease).
            // Already laid out, nothing would come: the timer goes anyway.
            VisualElement sidebar = Root.Q("sidebar");
            _sidebarIntroPending = true;
            EventCallback<GeometryChangedEvent> firstLayout = null;
            firstLayout = _ =>
            {
                sidebar.UnregisterCallback(firstLayout);
                StartSidebarIntro();
            };
            sidebar.RegisterCallback(firstLayout);
            Root.schedule.Execute(StartSidebarIntro).ExecuteLater(SidebarIntroFallbackMs);
        }

        private void StartSidebarIntro()
        {
            if (!_sidebarIntroPending) return;
            _sidebarIntroPending = false;
            // Behind a dialog by now: it eases in when the dialog closes.
            if (!IsOperation(_model.Screen)) StaggerSidebar();
        }

        private void StaggerSidebar()
        {
            int i = 0;
            foreach ((VisualElement row, string variant) in _sidebarRows)
            {
                if (row.ClassListContains(HiddenClass)) continue;
                Arrive(row, Math.Min(i++, MaxStaggered) * SidebarStepMs, variant);
            }
        }

        /// <summary>
        /// The Settings screen's groups rise in once its panel is under way; while it is closed they wait primed.
        /// PlayerSettingsPanel eases them in itself on a tab switch.
        /// </summary>
        private void PresentSettingsMotion(bool settings, bool opened)
        {
            VisualElement body = _settingsHost.Q(className: SettingsBodyClass);
            if (body == null || (settings && !opened)) return;
            if (settings) Cascade(body, PanelLeadMs, RowStepMs);
            else
            {
                foreach (VisualElement group in body.Children()) Prime(group, null);
            }
        }

        /// <summary>While a recent server is being asked, the refresh glyph spins (<see cref="Animate"/>) in the info colour.</summary>
        private void PresentRefreshSpin(bool servers, MainMenuPresentation model)
        {
            bool checking = false;
            foreach (MainMenuServerPresentation server in model.Recent) checking |= server.State == MainMenuServerState.Checking;
            VisualElement icon = Root.Q("refresh-icon");
            bool spin = servers && checking;
            icon.EnableInClassList(SpinClass, spin);
            if (!spin) icon.style.rotate = StyleKeyword.Null;
        }

        /// <summary>
        /// The list's rows rise in one after another when it opens. Present rebuilds the list (a status reply, a click),
        /// so a rebuild during that entrance carries it on: rows still due wait their turn, rows well under way show at
        /// once. After it, a rebuilt list shows as it is.
        /// </summary>
        private void StaggerList(VisualElement content)
        {
            float elapsedMs = (UiMotion.Time - _listOpenedAt) * 1000f;
            if (elapsedMs > PanelLeadMs + (MaxStaggered * RowStepMs) + SettleMs) return;
            int i = 0;
            foreach (VisualElement section in content.Children())
            {
                // Direct connect rises whole; a server section heading, columns and card by card.
                if (section.ClassListContains(DirectSectionClass))
                {
                    ListRow(section, i++, elapsedMs);
                    continue;
                }

                foreach (VisualElement row in section.Children()) ListRow(row, i++, elapsedMs);
            }
        }

        private static void ListRow(VisualElement row, int index, float elapsedMs)
        {
            float dueMs = PanelLeadMs + (Math.Min(index, MaxStaggered) * RowStepMs) - elapsedMs;
            if (dueMs < -CatchUpMs) return;
            Arrive(row, (long)Mathf.Max(0f, dueMs), null);
        }

        /// <summary>A section's heading: an icon (<c>main-menu__heading-icon--*</c>), the title and a rule out to the end.</summary>
        private static VisualElement Heading(VisualElement parent, string icon, string title, string titleClasses)
        {
            VisualElement heading = Element(parent, "main-menu__row main-menu__section-heading");
            Element(heading, "main-menu__heading-icon main-menu__heading-icon--" + icon).pickingMode = PickingMode.Ignore;
            Text(heading, title, titleClasses);
            Element(heading, "main-menu__heading-rule").pickingMode = PickingMode.Ignore;
            return heading;
        }

        private void DirectConnect(VisualElement parent, MainMenuPresentation model)
        {
            VisualElement box = Element(parent, "main-menu__section " + DirectSectionClass);
            if (model.FirstLaunch)
            {
                box.AddToClassList("main-menu__first");
                Heading(box, "direct", "Paste a server address", "main-menu__first-title");
                Text(box, "Server addresses are shared in the SS3D Discord. Copy one there and paste it here; the menu remembers it after the first join.", "main-menu__first-copy");
            }
            else Heading(box, "direct", "Direct connect", "main-menu__eyebrow");
            VisualElement row = Element(box, "main-menu__direct-row");
            TextField address = Input(row, model.Address, "main-menu__address");
            address.name = "direct-address";
            address.textEdition.placeholder = model.FirstLaunch ? "address or address:port" : "Paste address or address:port";
            Button connect = Action(row, "Connect", "main-menu__button--accent");
            connect.name = "connect";
            connect.SetEnabled(!string.IsNullOrWhiteSpace(model.Address) && !string.IsNullOrWhiteSpace(model.PlayerName));
            connect.clicked += () => Issue(MainMenuCommand.Connect);
            // Typing must not re-present: that would rebuild this field and drop focus.
            address.RegisterValueChangedCallback(e =>
            {
                _model.Address = e.newValue;
                connect.SetEnabled(!string.IsNullOrWhiteSpace(e.newValue) && !string.IsNullOrWhiteSpace(_model.PlayerName));
                FieldChanged?.Invoke(MainMenuField.Address, "", e.newValue);
            });
            address.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter) || !connect.enabledSelf) return;
                Issue(MainMenuCommand.Connect);
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
            if (!string.IsNullOrEmpty(model.AddressError))
                Text(box, model.AddressError, "main-menu__hint main-menu__hint--error");
            if (model.FirstLaunch) Quiet(box, "Open the SS3D Discord", "main-menu__link").clicked += () => Issue(MainMenuCommand.Discord);
        }

        private void Section(VisualElement parent, string title, string icon, IReadOnlyList<MainMenuServerPresentation> servers,
            bool recent, MainMenuPresentation model)
        {
            VisualElement section = Element(parent, "main-menu__section");
            VisualElement heading = Heading(section, icon, title, "main-menu__eyebrow");
            bool failed = !recent && model.PublicList == MainMenuPublicListState.FetchFailed;
            string count = servers.Count.ToString();
            if (recent) count += " / 5";
            else if (failed) count = "unavailable";
            Text(heading, count, "main-menu__fine");
            if (servers.Count == 0 || (!recent && model.PublicList != MainMenuPublicListState.Populated))
            {
                // A list that could not be fetched is a warning area; one with nothing in it is only empty.
                VisualElement empty = Element(section, failed ? "main-menu__empty main-menu__empty--failed" : "main-menu__empty");
                string emptyTitle = "No public servers yet";
                string emptyCopy = "The project lists servers here once they go public. Until then, addresses are shared on Discord.";
                if (recent) { emptyTitle = "No recent servers"; emptyCopy = "Servers you join show up here, newest first. The last five are kept."; }
                else if (failed) { emptyTitle = "Couldn't fetch the public list"; emptyCopy = "Recent servers and direct connect still work. Refresh to try again."; }
                Text(empty, emptyTitle, "main-menu__empty-title");
                Text(empty, emptyCopy, "main-menu__hint");
                return;
            }
            // Column heads reuse the columns' classes, so they line up with the rows.
            VisualElement columns = Element(section, "main-menu__server-heading");
            Text(columns, "Server", "main-menu__server-identity");
            Text(columns, "Status", "main-menu__status");
            Text(columns, "Players", "main-menu__players");
            Text(columns, "Ping", "main-menu__ping");
            Element(columns, "main-menu__actions");
            bool primary = false;
            foreach (MainMenuServerPresentation server in servers)
            {
                bool accent = recent && !primary && server.State == MainMenuServerState.Compatible;
                Server(section, server, recent, accent, !string.IsNullOrWhiteSpace(model.PlayerName));
                if (accent) primary = true;
            }
        }

        /// <summary>
        /// A server's state as its icon and card (<c>main-menu__state-icon--*</c>, <c>main-menu__server--*</c>, coloured
        /// in USS) and the status line's colour modifier.
        /// </summary>
        private static (string Icon, string Status) Tone(MainMenuServerState state) => state switch
        {
            MainMenuServerState.Compatible => ("online", ""),
            MainMenuServerState.NeedsUpdate => ("update", ""),
            MainMenuServerState.Full => ("full", "warn"),
            MainMenuServerState.Offline => ("offline", "danger"),
            MainMenuServerState.OlderVersion => ("older", ""),
            _ => ("checking", ""),
        };

        private void Server(VisualElement parent, MainMenuServerPresentation server, bool recent, bool primary, bool hasName)
        {
            (string icon, string tone) = Tone(server.State);
            // The card's left edge takes the state's colour on hover and keeps it while open; a refusal makes it danger.
            VisualElement card = Element(parent, "main-menu__server main-menu__server--" + icon); card.name = $"server-{server.Id}";
            VisualElement row = Element(card, "main-menu__server-row");
            string id = server.Id;
            VisualElement stateIcon = Element(row, "main-menu__state-icon main-menu__state-icon--" + icon);
            stateIcon.pickingMode = PickingMode.Ignore;
            if (server.State == MainMenuServerState.Checking) stateIcon.AddToClassList(SpinClass);
            VisualElement identity = Element(row, "main-menu__server-identity");
            Text(identity, server.Name, "main-menu__server-name");
            Text(identity, server.Address, "main-menu__server-address");
            string status = server.State switch { MainMenuServerState.Compatible => "Online", MainMenuServerState.NeedsUpdate => $"Needs v{server.RequiredVersion} — update", MainMenuServerState.Full => "Full", MainMenuServerState.OlderVersion => $"Runs v{server.RequiredVersion}, older than yours", MainMenuServerState.Checking => "Checking…", _ => "Offline" };
            Text(row, status, "main-menu__status" + (tone.Length > 0 ? " main-menu__status--" + tone : ""));
            Text(row, server.Players, "main-menu__players" + (server.State == MainMenuServerState.Full ? " main-menu__players--warn" : ""));
            Text(row, server.Ping, "main-menu__ping");
            VisualElement actions = Element(row, "main-menu__actions");
            if (recent) Quiet(actions, server.Expanded ? "Hide" : "Details", "main-menu__details-button").clicked += () => Issue(MainMenuCommand.ServerDetails, id);
            bool update = server.State == MainMenuServerState.NeedsUpdate;
            string actionClass = "";
            if (update) actionClass = "main-menu__button--blue";
            else if (primary) actionClass = "main-menu__button--accent";
            Button join = Action(actions, update ? "Update" : "Join", actionClass);
            join.SetEnabled(hasName && (update || server.State is MainMenuServerState.Compatible or MainMenuServerState.Checking));
            join.clicked += () => Issue(update ? MainMenuCommand.ServerUpdate : MainMenuCommand.ServerJoin, id);
            if (!string.IsNullOrEmpty(server.Refusal))
            {
                card.AddToClassList("main-menu__server--refused");
                VisualElement refusal = Element(card, "main-menu__refusal");
                Text(refusal, server.Refusal, "");
                Text(refusal, server.RefusalTime, "main-menu__fine");
            }

            if (!recent || !server.Expanded) return;
            card.AddToClassList("main-menu__server--open");
            VisualElement details = Details(card, server);
            // Opened by this Present: the details rise in under the row.
            _expandedNow.Add(id);
            if (!_expandedShown.Contains(id)) Arrive(details, 0, null);
        }

        private VisualElement Details(VisualElement parent, MainMenuServerPresentation server)
        {
            string id = server.Id;
            VisualElement details = Element(parent, "main-menu__details");
            VisualElement name = DetailRow(details, "Name");
            TextField nameField = Input(name, server.Name, "main-menu__detail-value");
            nameField.maxLength = 40;
            nameField.RegisterValueChangedCallback(e => FieldChanged?.Invoke(MainMenuField.ServerName, id, e.newValue));
            Text(DetailRow(details, "Address"), server.Address, "main-menu__detail-text");
            VisualElement tokenRow = DetailRow(details, "Admin token");
            TextField token = Input(tokenRow, server.AdminToken, "main-menu__detail-value");
            token.isPasswordField = true; token.textEdition.placeholder = "Optional";
            token.RegisterValueChangedCallback(e => FieldChanged?.Invoke(MainMenuField.AdminToken, id, e.newValue));
            Button show = Quiet(tokenRow, "Show", "main-menu__details-button");
            show.style.marginLeft = 6;
            show.clicked += () =>
            {
                token.isPasswordField = !token.isPasswordField;
                show.text = token.isPasswordField ? "Show" : "Hide";
            };
            VisualElement footer = Element(details, "main-menu__details-footer");
            Text(footer, "Saved automatically. The token is sent when joining; admins join like everyone else.", "main-menu__hint");
            Action(footer, "Remove", "main-menu__button--danger").clicked += () => Issue(MainMenuCommand.ServerRemove, id);
            return details;
        }

        /// <param name="entering">The dialog has just appeared: its steps follow it in, one after another.</param>
        private void Operation(MainMenuPresentation model, bool entering)
        {
            bool updating = model.Screen == MainMenuScreen.Updating;
            bool hosting = model.Screen == MainMenuScreen.Hosting;
            string title = "CONNECTING";
            if (updating) title = "UPDATE AND RESTART";
            else if (hosting) title = "STARTING LOCAL SESSION";
            else if (model.OperationLost) title = "CONNECTION LOST";
            Root.Q<Label>("overlay-title").text = title;
            Root.Q<Label>("overlay-name").text = model.OperationName;
            Root.Q<Label>("overlay-address").text = model.OperationAddress;
            Show("cancel", !updating && !model.OperationFailed);
            Show("retry", model.OperationFailed);
            Show("back-to-servers", model.OperationFailed);
            Show("overlay-token-row", model.OperationNeedsToken);
            if (model.OperationNeedsToken) SetText("overlay-token", model.OperationToken);

            // Colour coding: hosting ok, joining info, updating warn, lost or failed danger (--op, MainMenu.uss).
            string kind = "join";
            if (updating) kind = "update";
            else if (hosting) kind = "host";
            if (model.OperationLost) kind = "lost";
            else if (model.OperationFailed) kind = "failed";
            foreach (string each in OperationKinds) _overlay.EnableInClassList("main-menu__overlay--" + each, each == kind);

            string[] labels = { "Contacting server", "Checking version", "Loading world", "Logging in" };
            if (updating) labels = new string[] { "Downloading update", "Keeping maps and settings", "Restarting" };
            else if (hosting) labels = new string[] { "Starting host", "Loading world", "Opening lobby" };
            VisualElement steps = Root.Q("progress-steps");
            string built = string.Join("|", labels) + "|" + model.OperationLost + "|" + model.OperationFailed + "|" + model.OperationReason;
            if (built != _stepsBuilt)
            {
                _stepsBuilt = built;
                steps.Clear();
                if (model.OperationLost) Reason(steps, model.OperationReason);
                else
                {
                    for (int i = 0; i < labels.Length; i++)
                    {
                        VisualElement row = Element(steps, "main-menu__step");
                        Element(row, "main-menu__step-icon").pickingMode = PickingMode.Ignore;
                        Text(row, labels[i], "main-menu__step-label");
                        Text(row, "", "main-menu__step-mark");
                        if (model.OperationFailed && i == model.OperationStep) Reason(steps, model.OperationReason);
                    }
                }

                if (entering) Cascade(steps, PanelLeadMs, RowStepMs + 10);
            }

            _progress.EnableInClassList("main-menu__progress--hidden", model.OperationLost);
            _progress.EnableInClassList("main-menu__progress--working", !model.OperationLost && !model.OperationFailed
                && model.OperationStep < labels.Length);
            if (model.OperationLost) return;
            int index = 0;
            foreach (VisualElement row in steps.Children())
            {
                if (!row.ClassListContains("main-menu__step")) continue;
                PresentStep(row, index++, model);
            }

            // Done steps, and half the working one; a failure fills up to the step that failed.
            float share = (model.OperationStep + (model.OperationFailed ? 1f : 0.5f)) / labels.Length;
            _progressFill.style.width = Length.Percent(Mathf.Clamp01(share) * 100f);
        }

        /// <summary>One step's state: pending (a dot), working (the spinner in --op), done (a check, OK), failed (a cross).</summary>
        private static void PresentStep(VisualElement row, int i, MainMenuPresentation model)
        {
            bool failed = model.OperationFailed && i == model.OperationStep;
            bool done = i < model.OperationStep;
            bool active = !model.OperationFailed && i == model.OperationStep;
            bool pending = i > model.OperationStep;
            row.EnableInClassList("main-menu__step--pending", pending);
            row.EnableInClassList("main-menu__step--active", active);
            row.EnableInClassList("main-menu__step--failed", failed);
            VisualElement icon = row.Q(className: "main-menu__step-icon");
            icon.EnableInClassList("main-menu__step-icon--pending", pending);
            icon.EnableInClassList("main-menu__step-icon--active", active);
            icon.EnableInClassList("main-menu__step-icon--done", done);
            icon.EnableInClassList("main-menu__step-icon--failed", failed);
            icon.EnableInClassList(SpinClass, active);
            if (!active) icon.style.rotate = StyleKeyword.Null;
            Label mark = row.Q<Label>(className: "main-menu__step-mark");
            mark.text = "";
            if (done) mark.text = "OK";
            else if (failed) mark.text = "FAILED";
            mark.EnableInClassList("main-menu__step-mark--ok", done);
            mark.EnableInClassList("main-menu__step-mark--failed", failed);
        }

        private static void Reason(VisualElement parent, string reason) =>
            Text(Element(parent, "main-menu__step-reason"), reason, "");

        // ---- Motion (UiMotion; the looks are in Tokens/ui-controls.uss, the transitions' override in MainMenu.uss) ----

        /// <summary>
        /// Shows or hides a part; one that appears eases in. Hidden, it waits primed, so the entrance starts from its
        /// hidden look rather than easing out of the look it had when it was hidden.
        /// </summary>
        private static void Reveal(VisualElement element, bool visible, string variant)
        {
            bool wasVisible = !element.ClassListContains(HiddenClass);
            element.EnableInClassList(HiddenClass, !visible);
            if (!visible) Prime(element, variant);
            else if (!wasVisible) Arrive(element, 0, variant);
        }

        /// <summary>Puts an element out of sight in its entrance's hidden look (<c>ui-enter</c>, without <c>--in</c>).</summary>
        private static void Prime(VisualElement element, string variant)
        {
            if (variant != null) element.AddToClassList(variant);
            element.AddToClassList(UiMotion.EnterClass);
            element.RemoveFromClassList(UiMotion.InClass);
        }

        /// <summary>
        /// Eases an element in (<see cref="UiMotion.Enter"/>), then settles it: the entrance's classes and delay come
        /// off, since they replace the element's own transitions (a hover's) and would hold back every later one.
        /// </summary>
        private static void Arrive(VisualElement element, long delayMs, string variant)
        {
            if (variant != null) element.AddToClassList(variant);
            UiMotion.Enter(element, delayMs);
            if (element.panel == null)
            {
                Settle(element, variant);
                return;
            }

            element.schedule.Execute(() => Settle(element, variant)).ExecuteLater(delayMs + SettleMs);
        }

        private static void Settle(VisualElement element, string variant)
        {
            // Primed again since (hidden before it arrived): it stays in its hidden look.
            if (!element.ClassListContains(UiMotion.InClass)) return;
            element.RemoveFromClassList(UiMotion.EnterClass);
            element.RemoveFromClassList(UiMotion.InClass);
            if (variant != null) element.RemoveFromClassList(variant);
            element.style.transitionDelay = new StyleList<TimeValue>(StyleKeyword.Null);
        }

        /// <summary>Eases each child of <paramref name="parent"/> in, <paramref name="stepMs"/> apart, and settles it.</summary>
        private static void Cascade(VisualElement parent, long firstMs, long stepMs)
        {
            int i = 0;
            foreach (VisualElement child in parent.Children()) Arrive(child, firstMs + (Math.Min(i++, MaxStaggered) * stepMs), null);
        }

        private void Show(string name, bool visible) => Root.Q(name).EnableInClassList(HiddenClass, !visible);
        private void SetText(string name, string value)
        {
            // Only on change: rewriting the text being typed would move the caret.
            TextField field = Root.Q<TextField>(name);
            if (field.value != value) field.SetValueWithoutNotify(value);
        }
        private void Issue(MainMenuCommand command, string id = "") => CommandIssued?.Invoke(command, id);
        private void Command(string name, MainMenuCommand command) => Root.Q<Button>(name).clicked += () => Issue(command);
        private void Field(string name, MainMenuField field) =>
            Root.Q<TextField>(name).RegisterValueChangedCallback(e => FieldChanged?.Invoke(field, "", e.newValue));
        private static void Classes(VisualElement element, string classes)
        { foreach (string value in classes.Split(' ')) if (value.Length > 0) element.AddToClassList(value); }
        private static VisualElement Element(VisualElement parent, string classes)
        { VisualElement element = new(); Classes(element, classes); parent.Add(element); return element; }
        private static Label Text(VisualElement parent, string text, string classes)
        { Label label = new(text); label.AddToClassList("main-menu__text"); Classes(label, classes); parent.Add(label); return label; }
        /// <summary>A tinted button with a Disket label (<c>main-menu__button</c>).</summary>
        private static Button Action(VisualElement parent, string text, string classes)
        { Button button = new() { text = text }; button.AddToClassList("main-menu__button"); Classes(button, classes); parent.Add(button); return button; }
        /// <summary>A text button with no plate: a link or a details toggle.</summary>
        private static Button Quiet(VisualElement parent, string text, string classes)
        { Button button = new() { text = text }; Classes(button, classes); parent.Add(button); return button; }
        private static TextField Input(VisualElement parent, string value, string classes)
        { TextField field = new() { value = value }; field.AddToClassList("main-menu__input"); Classes(field, classes); parent.Add(field); return field; }
        private static VisualElement DetailRow(VisualElement parent, string key)
        { VisualElement row = Element(parent, "main-menu__detail-row"); Text(row, key, "main-menu__detail-key"); return row; }
    }
}
