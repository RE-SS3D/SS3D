using System;
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
    public sealed class MainMenuView : IUiSurface
    {
        public VisualElement Root { get; }
        private readonly MainMenuAssetCatalog _catalog;
        private Texture2D _blurredWallpaper;
        private Texture2D _blurredSource;
        private MainMenuPresentation _model = new();

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
            Root.Query(className: "main-menu__empty").ForEach(DashedBorder);
            Root.Q("main-menu").RegisterCallback<GeometryChangedEvent>(e =>
            {
                Root.Q("main-menu").EnableInClassList("main-menu--compact", e.newRect.width < 1450);
                Root.Q<ScrollView>("sidebar").contentContainer.style.minHeight = e.newRect.height;
            });
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
            Root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode != KeyCode.Escape) return;
                Issue(MainMenuCommand.Back);
                e.StopPropagation();
            }, TrickleDown.TrickleDown);
        }

        private void UpdateVolumeFill(float value)
        {
            Root.Q("music-volume-fill").style.width = Length.Percent(Mathf.Clamp01(value) * 100f);
        }

        public void Attach(VisualElement layerRoot) { Detach(); layerRoot.Add(Root); }
        public void Detach()
        {
            Root.RemoveFromHierarchy();
            if (_blurredWallpaper != null)
            {
                if (UnityEngine.Application.isPlaying) Object.Destroy(_blurredWallpaper);
                else Object.DestroyImmediate(_blurredWallpaper);
                _blurredWallpaper = null;
                _blurredSource = null;
            }
        }

        public void Present(MainMenuPresentation model)
        {
            _model = model;
            Root.Q("wallpaper").style.backgroundImage = _catalog.Wallpaper(model.Wallpaper);
            SetText("player-name", model.PlayerName);
            SetText("port", model.Port);
            Root.Q<Label>("version").text = $"v{model.Version} pre-alpha";
            Show("update-banner", model.UpdateAvailable);
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
            Root.Q<Button>("update").text = model.CanSelfUpdate ? "UPDATE AND RESTART" : "DOWNLOAD";
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
            localSubtitle.text = hasName ? $"Host on this computer, port {model.Port}" : "Needs a name";
            bool localError = hasName && !string.IsNullOrEmpty(model.LocalError);
            if (localError) localSubtitle.text = model.LocalError;
            localSubtitle.style.color = localError ? new StyleColor(ColorCode("#d17a7a")) : new StyleColor(StyleKeyword.Null);
            Show("name-hint", !hasName || model.FirstLaunch);
            Root.Q<Label>("name-hint").text = !hasName ? "Enter a name to join or play." : "Taken from this computer's user name. Change it any time.";
            Root.Q<Label>("name-hint").style.color = hasName ? new Color(0.667f, 0.667f, 0.667f) : new Color(0.941f, 0.769f, 0.49f);
            Show("port-panel", model.Advanced);
            Root.Q("advanced-caret").EnableInClassList("main-menu__caret--up", model.Advanced);
            bool servers = model.Screen == MainMenuScreen.Servers;
            bool settings = model.Screen == MainMenuScreen.Settings;
            Root.Q("settings").EnableInClassList("main-menu__nav--selected", settings);
            bool operation = model.Screen is MainMenuScreen.Connecting or MainMenuScreen.Hosting or MainMenuScreen.Updating;
            if (operation)
            {
                Texture2D source = _catalog.Wallpaper(model.Wallpaper);
                if (_blurredWallpaper == null || _blurredSource != source)
                {
                    if (_blurredWallpaper != null)
                    {
                        if (UnityEngine.Application.isPlaying) Object.Destroy(_blurredWallpaper);
                        else Object.DestroyImmediate(_blurredWallpaper);
                    }
                    _blurredSource = source;
                    _blurredWallpaper = MainMenuWallpaper.Blur(source);
                }
                Root.Q("wallpaper").style.backgroundImage = _blurredWallpaper;
            }
            Show("sidebar", !operation);
            Show("side-panel", servers || settings);
            Show("server-scroll", servers);
            Show("settings-placeholder", settings);
            Show("refresh", servers);
            Root.Q<Label>("panel-title").text = settings ? "SETTINGS" : "JOIN A SERVER";
            Root.Q<Label>("checked").text = settings ? "Esc" : model.CheckedText;
            VisualElement content = Root.Q("server-content");
            content.Clear();
            if (servers)
            {
                DirectConnect(content, model);
                Section(content, "RECENT", model.Recent, true, model);
                Section(content, "PUBLIC", model.Public, false, model);
            }
            Show("overlay", operation);
            if (operation) Operation(model);
        }

        private void DirectConnect(VisualElement parent, MainMenuPresentation model)
        {
            VisualElement box = Element(parent, "main-menu__section");
            if (model.FirstLaunch)
            {
                box.AddToClassList("main-menu__first");
                Text(box, "Paste a server address", "main-menu__bold main-menu__large");
                Text(box, "Server addresses are shared in the SS3D Discord. Copy one there and paste it here; the menu remembers it after the first join.", "main-menu__secondary main-menu__wrap");
            }
            else Text(box, "DIRECT CONNECT", "main-menu__eyebrow");
            VisualElement row = Element(box, "main-menu__direct-row");
            TextField address = Input(row, model.Address, "main-menu__address main-menu__mono");
            address.name = "direct-address";
            address.textEdition.placeholder = model.FirstLaunch ? "address or address:port" : "Paste address or address:port";
            Button connect = Action(row, "CONNECT", "main-menu__button--accent");
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
                Text(box, model.AddressError, "main-menu__wrap main-menu__hint").style.color = ColorCode("#d17a7a");
            if (model.FirstLaunch) Action(box, "Open the SS3D Discord", "main-menu__link").clicked += () => Issue(MainMenuCommand.Discord);
        }

        private void Section(VisualElement parent, string title, System.Collections.Generic.IReadOnlyList<MainMenuServerPresentation> servers,
            bool recent, MainMenuPresentation model)
        {
            VisualElement section = Element(parent, "main-menu__section");
            VisualElement heading = Element(section, "main-menu__row main-menu__spread");
            Text(heading, title, "main-menu__eyebrow");
            string count = servers.Count.ToString();
            if (recent) count += " / 5";
            else if (model.PublicList == MainMenuPublicListState.FetchFailed) count = "unavailable";
            Text(heading, count, "main-menu__mono main-menu__tiny main-menu__secondary");
            if (servers.Count == 0 || (!recent && model.PublicList != MainMenuPublicListState.Populated))
            {
                VisualElement empty = Element(section, "main-menu__empty");
                bool failed = !recent && model.PublicList == MainMenuPublicListState.FetchFailed;
                string emptyTitle = "No public servers yet";
                string emptyCopy = "The project lists servers here once they go public. Until then, addresses are shared on Discord.";
                if (recent) { emptyTitle = "No recent servers"; emptyCopy = "Servers you join show up here, newest first. The last five are kept."; }
                else if (failed) { emptyTitle = "Couldn't fetch the public list"; emptyCopy = "Recent servers and direct connect still work. Refresh to try again."; }
                Text(empty, emptyTitle, "");
                Text(empty, emptyCopy, "main-menu__secondary main-menu__wrap");
                return;
            }
            VisualElement columns = Element(section, "main-menu__server-heading");
            VisualElement gutter = Element(columns, ""); gutter.style.width = 18;
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

        private void Server(VisualElement parent, MainMenuServerPresentation server, bool recent, bool primary, bool hasName)
        {
            VisualElement card = Element(parent, "main-menu__server"); card.name = $"server-{server.Id}";
            VisualElement row = Element(card, "main-menu__server-row");
            string color = server.State switch { MainMenuServerState.Compatible => "#88b36a", MainMenuServerState.NeedsUpdate => "#e6ad53", MainMenuServerState.Full => "#e6ad53", MainMenuServerState.Offline => "#b13d3d", _ => "#717c7f" };
            string id = server.Id;
            Element(row, "main-menu__dot").style.backgroundColor = ColorCode(color);
            VisualElement identity = Element(row, "main-menu__server-identity");
            Text(identity, server.Name, "main-menu__server-name");
            Text(identity, server.Address, "main-menu__server-address main-menu__mono");
            string status = server.State switch { MainMenuServerState.Compatible => "Online", MainMenuServerState.NeedsUpdate => $"Needs v{server.RequiredVersion} — update", MainMenuServerState.Full => "Full", MainMenuServerState.OlderVersion => $"Runs v{server.RequiredVersion}, older than yours", MainMenuServerState.Checking => "Checking…", _ => "Offline" };
            string statusColor = "#aaaaaa";
            if (server.State == MainMenuServerState.Compatible) statusColor = "#a8cf8c";
            else if (server.State is MainMenuServerState.NeedsUpdate or MainMenuServerState.Full) statusColor = "#f0c47d";
            Text(row, status, "main-menu__status main-menu__wrap").style.color = ColorCode(statusColor);
            Label players = Text(row, server.Players, "main-menu__players main-menu__mono");
            if (server.State == MainMenuServerState.Full) players.style.color = ColorCode("#f0c47d");
            Text(row, server.Ping, "main-menu__ping main-menu__mono");
            VisualElement actions = Element(row, "main-menu__actions");
            if (recent) Action(actions, server.Expanded ? "Hide" : "Details", "main-menu__details-button").clicked += () => Issue(MainMenuCommand.ServerDetails, id);
            bool update = server.State == MainMenuServerState.NeedsUpdate;
            string actionClass = "";
            if (update) actionClass = "main-menu__button--blue";
            else if (primary) actionClass = "main-menu__button--accent";
            Button join = Action(actions, update ? "UPDATE" : "JOIN", actionClass);
            join.SetEnabled(hasName && (update || server.State is MainMenuServerState.Compatible or MainMenuServerState.Checking));
            join.clicked += () => Issue(update ? MainMenuCommand.ServerUpdate : MainMenuCommand.ServerJoin, id);
            if (!string.IsNullOrEmpty(server.Refusal))
            {
                VisualElement refusal = Element(card, "main-menu__refusal");
                Text(refusal, server.Refusal, "main-menu__wrap");
                Text(refusal, server.RefusalTime, "main-menu__mono main-menu__tiny main-menu__secondary");
            }
            if (recent && server.Expanded) Details(card, server);
        }

        private void Details(VisualElement parent, MainMenuServerPresentation server)
        {
            string id = server.Id;
            VisualElement details = Element(parent, "main-menu__details");
            VisualElement name = DetailRow(details, "Name");
            TextField nameField = Input(name, server.Name, "main-menu__detail-value");
            nameField.maxLength = 40;
            nameField.RegisterValueChangedCallback(e => FieldChanged?.Invoke(MainMenuField.ServerName, id, e.newValue));
            Text(DetailRow(details, "Address"), server.Address, "main-menu__mono");
            VisualElement tokenRow = DetailRow(details, "Admin token");
            TextField token = Input(tokenRow, server.AdminToken, "main-menu__detail-value main-menu__mono");
            token.isPasswordField = true; token.textEdition.placeholder = "Optional";
            token.RegisterValueChangedCallback(e => FieldChanged?.Invoke(MainMenuField.AdminToken, id, e.newValue));
            Button show = Action(tokenRow, "Show", "main-menu__details-button");
            show.clicked += () =>
            {
                token.isPasswordField = !token.isPasswordField;
                show.text = token.isPasswordField ? "Show" : "Hide";
            };
            VisualElement footer = DetailRow(details, "");
            Label hint = Text(footer, "Saved automatically. The token is sent when joining; admins join like everyone else.", "main-menu__secondary main-menu__tiny main-menu__wrap");
            hint.style.maxWidth = 420; hint.style.flexShrink = 1; hint.style.marginRight = 12;
            Action(footer, "REMOVE", "main-menu__button--danger").clicked += () => Issue(MainMenuCommand.ServerRemove, id);
        }

        private void Operation(MainMenuPresentation model)
        {
            bool updating = model.Screen == MainMenuScreen.Updating;
            bool hosting = model.Screen == MainMenuScreen.Hosting;
            string title = "CONNECTING";
            if (updating) title = "UPDATE AND RESTART";
            else if (hosting) title = "STARTING LOCAL SESSION";
            Root.Q<Label>("overlay-title").text = title;
            Root.Q<Label>("overlay-name").text = model.OperationName;
            Root.Q<Label>("overlay-address").text = model.OperationAddress;
            Show("cancel", !updating);
            VisualElement steps = Root.Q("progress-steps"); steps.Clear();
            string[] labels = { "Contacting server", "Checking version", "Loading world" };
            if (updating) labels = new string[] { "Downloading update", "Keeping maps and settings", "Restarting" };
            else if (hosting) labels = new string[] { "Starting host", "Loading world", "Opening lobby" };
            for (int i = 0; i < labels.Length; i++)
            {
                VisualElement row = Element(steps, "main-menu__step");
                Text(row, labels[i], i > model.OperationStep ? "main-menu__secondary" : "");
                string progress = "";
                if (i < model.OperationStep) progress = "OK";
                else if (i == model.OperationStep) progress = "…";
                Label mark = Text(row, progress, "main-menu__mono");
                if (i < model.OperationStep) mark.style.color = ColorCode("#a8cf8c");
            }
        }

        private void Show(string name, bool visible) => Root.Q(name).EnableInClassList("main-menu__hidden", !visible);
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
        private static Color ColorCode(string value) { ColorUtility.TryParseHtmlString(value, out Color color); return color; }
        private static void Classes(VisualElement element, string classes)
        { foreach (string value in classes.Split(' ')) if (value.Length > 0) element.AddToClassList(value); }
        private static VisualElement Element(VisualElement parent, string classes)
        {
            VisualElement element = new(); Classes(element, classes); parent.Add(element);
            if (element.ClassListContains("main-menu__empty")) DashedBorder(element);
            return element;
        }

        private static void DashedBorder(VisualElement element)
        {
            element.generateVisualContent += context =>
            {
                Painter2D painter = context.painter2D;
                painter.lineWidth = 1;
                painter.strokeColor = new Color(0.847f, 0.847f, 0.847f, 0.18f);
                float width = element.layout.width - 1;
                float height = element.layout.height - 1;
                Vector2[] corners = { new(0.5f, 0.5f), new(width, 0.5f), new(width, height), new(0.5f, height) };
                painter.BeginPath();
                for (int edge = 0; edge < 4; edge++)
                {
                    Vector2 start = corners[edge];
                    Vector2 delta = corners[(edge + 1) % 4] - start;
                    float length = delta.magnitude;
                    for (float distance = 0; distance < length; distance += 10)
                    {
                        painter.MoveTo(start + delta.normalized * distance);
                        painter.LineTo(start + delta.normalized * Mathf.Min(distance + 6, length));
                    }
                }
                painter.Stroke();
            };
        }
        private static Label Text(VisualElement parent, string text, string classes)
        { Label label = new(text); label.AddToClassList("main-menu__text"); Classes(label, classes); parent.Add(label); return label; }
        private static Button Action(VisualElement parent, string text, string classes)
        { Button button = new() { text = text }; button.AddToClassList("main-menu__button"); Classes(button, classes); parent.Add(button); return button; }
        private static TextField Input(VisualElement parent, string value, string classes)
        { TextField field = new() { value = value }; field.AddToClassList("main-menu__input"); Classes(field, classes); parent.Add(field); return field; }
        private static VisualElement DetailRow(VisualElement parent, string key)
        { VisualElement row = Element(parent, "main-menu__detail-row"); Text(row, key, "main-menu__detail-key"); return row; }
    }
}
