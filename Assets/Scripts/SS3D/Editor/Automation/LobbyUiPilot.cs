using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;

namespace SS3D.Editor.Automation
{
    /// <summary>Repeatable rendered smoke check, callable through MCP execute_menu_item.</summary>
    [InitializeOnLoad]
    public static class LobbyUiPilot
    {
        private const string ActiveKey = "SS3D.LobbyPilot.Active";
        private const string OutputKey = "SS3D.LobbyPilot.Output";
        private const string SceneKey = "SS3D.LobbyPilot.PreviousStartScene";
        private const string SizeKey = "SS3D.LobbyPilot.PreviousGameViewSize";
        private static readonly List<string> Errors = new();
        private static readonly List<string> Checks = new();
        private static int _step;
        private static int _styleCheckStep;
        private static int _shellCheckStep;
        private static int[] _initialOutfitMeshes;
        private static double _next;
        private static double _layoutDeadline;
        private static double _deadline;
        private static string _pendingImage;
        private static bool _running;
        private static Report _pending;

        static LobbyUiPilot()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        // Guards: lobby shell, settings and Character Creator render and respond (rounds-lobby UI contracts) · Not cheaper because: needs rendered UI Toolkit panels and screenshots a human reviews; weekly graphical run
        [MenuItem("SS3D/Testing/Run Lobby UI Pilot")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before starting a fresh lobby pilot.");
            if (EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("Fix compilation errors before starting the lobby pilot.");
            // A compiler-blocked Play request may never emit EnteredEditMode.
            if (SessionState.GetBool(ActiveKey, false)) RestoreEditorSettings();
            string output = Path.GetFullPath("artifacts/ui/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff"));
            Directory.CreateDirectory(output);
            SessionState.SetString(OutputKey, output);
            try { AutomationPreflight.Require("lobby UI pilot"); }
            catch (InvalidOperationException exception)
            {
                Write(new Report { status = "failed", failure = exception.Message, checks = new string[0], errors = new string[0] }, output);
                throw;
            }
            SessionState.SetString(SceneKey, AssetDatabase.GetAssetPath(EditorSceneManager.playModeStartScene));
            SetGameViewSize();
            SessionState.SetBool(ActiveKey, true);
            EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Content/Scenes/Boot.unity");
            File.WriteAllText(Path.Combine(output, "status.json"), "{\"status\":\"starting\"}");
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                Errors.Clear();
                Checks.Clear();
                _step = 0;
                _styleCheckStep = 0;
                _shellCheckStep = 0;
                _pendingImage = null;
                _running = true;
                _deadline = EditorApplication.timeSinceStartup + 120;
                _layoutDeadline = 0;
                _next = EditorApplication.timeSinceStartup + 3;
                Application.logMessageReceived += OnLog;
                EditorApplication.update += Tick;
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                if (_running) Finish("Play Mode exited before the scenario completed.", false);
                Application.logMessageReceived -= OnLog;
                EditorApplication.update -= Tick;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                RestoreEditorSettings();
                if (_pending == null) return;
                Report report = _pending;
                string output = SessionState.GetString(OutputKey, "");
                _pending = null;
                AutomationPreflight.AfterPlay(problems => Complete(report, output, problems));
            }
        }

        private static void RestoreEditorSettings()
        {
            string previous = SessionState.GetString(SceneKey, "");
            EditorSceneManager.playModeStartScene = string.IsNullOrEmpty(previous)
                ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(previous);
            GameViewSizeProperty().SetValue(GameView(), SessionState.GetInt(SizeKey, 0));
            SessionState.SetBool(ActiveKey, false);
        }

        // Unity has no public Game View size API. Keep the version-sensitive reflection
        // here, fail explicitly if it changes, and restore the user's selected size.
        private static EditorWindow GameView() => EditorWindow.GetWindow(
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView", true));

        // Mirrors of PlayerSettings' file shape; this editor assembly does not reference the runtime ones.
        [Serializable]
        private sealed class SavedSettings
        {
            public SavedAudio Audio;
        }

        [Serializable]
        private sealed class SavedAudio
        {
            public float Master;
            public float Music;
            public bool MuteAll;
        }

        /// <summary>Flushes PlayerSettingsSubSystem's debounced save so the pilot can read the file back.</summary>
        private static bool SavePlayerSettingsNow()
        {
            foreach (MonoBehaviour behaviour in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour.GetType().Name == "PlayerSettingsSubSystem")
                    return (bool)behaviour.GetType().GetMethod("SaveNow").Invoke(behaviour, null);
            return false;
        }

        private static PropertyInfo GameViewSizeProperty() => GameView().GetType().GetProperty(
            "selectedSizeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        private static void SetGameViewSize()
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes", true);
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes = singleton.GetProperty("instance").GetValue(null);
            object group = sizesType.GetMethod("GetGroup").Invoke(sizes, new object[] { 0 });
            Type groupType = group.GetType();
            int builtIn = (int)groupType.GetMethod("GetBuiltinCount").Invoke(group, null);
            int custom = (int)groupType.GetMethod("GetCustomCount").Invoke(group, null);
            const string label = "SS3D UI Pilot 1920x1080";
            int selected = -1;
            for (int i = 0; i < builtIn + custom; i++)
            {
                object size = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((string)size.GetType().GetProperty("baseText").GetValue(size) == label) selected = i;
            }
            if (selected < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize", true);
                var kindType = assembly.GetType("UnityEditor.GameViewSizeType", true);
                object size = Activator.CreateInstance(sizeType, Enum.ToObject(kindType, 1), 1920, 1080, label);
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                selected = builtIn + custom;
            }
            EditorWindow view = GameView();
            SessionState.SetInt(SizeKey, (int)GameViewSizeProperty().GetValue(view));
            GameViewSizeProperty().SetValue(view, selected);
            view.Focus();
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Errors.Add(message + "\n" + stack);
        }

        private static VisualElement Root()
        {
            return UnityEngine.Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
                .Select(d => d.rootVisualElement)
                .FirstOrDefault(r => r != null && r.Q("lobby-shell-surface") != null);
        }

        // The uGUI lobby (LobbyCanvas, renamed "Lobby" in Game.unity) and Character Customizer canvases were purged in Phase F.
        private static bool LegacyLobbyCanvasPresent() => UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Any(canvas => canvas.isRootCanvas && (canvas.name == "Lobby" || canvas.name == "Character Customizer"));

        private static void Require(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
            Checks.Add(message);
        }

        private static void Click(VisualElement element)
        {
            Require(element != null && element.worldBound.width > 0, "Clickable element has layout");
            using (ClickEvent evt = ClickEvent.GetPooled())
            {
                evt.target = element;
                element.SendEvent(evt);
            }
        }

        private static void Submit(VisualElement surface, string text)
        {
            Button button = surface.Query<Button>().ToList().FirstOrDefault(b => b.text == text);
            Require(button != null && button.enabledInHierarchy && button.worldBound.width > 0,
                "Enabled, laid-out button: " + text);
            button.Focus();
            using (NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        private static void Capture(string name)
        {
            _pendingImage = Path.Combine(SessionState.GetString(OutputKey, ""), name + ".png");
            ScreenCapture.CaptureScreenshot(_pendingImage);
        }

        private static void CheckPreviewFraming()
        {
            Camera camera = GameObject.Find("CharacterPreviewCamera").GetComponent<Camera>();
            RenderTexture small = RenderTexture.GetTemporary(192, 256, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture2D texture = new(192, 256, TextureFormat.RGBA32, false);
            try
            {
                Graphics.Blit(camera.targetTexture, small);
                RenderTexture.active = small;
                texture.ReadPixels(new Rect(0, 0, 192, 256), 0, 0);
                texture.Apply();
                Color32[] pixels = texture.GetPixels32();
                Color32 background = pixels[0];
                int minY = 256, maxY = -1;
                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 192; x++)
                    {
                        Color32 pixel = pixels[y * 192 + x];
                        if (Mathf.Abs(pixel.r - background.r) <= 12 && Mathf.Abs(pixel.g - background.g) <= 12
                            && Mathf.Abs(pixel.b - background.b) <= 12) continue;
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
                float occupancy = (maxY - minY + 1) / 256f;
                Require(occupancy >= 0.72f, $"Character fills preview height (measured {occupancy:P0})");
                Require(minY > 1 && maxY < 254, "Preview keeps head and feet inside the frame");
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(small);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static int[] PreviewOutfitMeshes()
        {
            GameObject dummy = GameObject.Find("CharacterPreviewDummy");
            SkinnedMeshRenderer[] renderers = dummy.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            return new string[] { "Jumpsuit", "ShoeLeft", "ShoeRight" }.Select(name =>
            {
                SkinnedMeshRenderer renderer = renderers.Single(r => r.name == name);
                Require(renderer.enabled && renderer.gameObject.activeInHierarchy && renderer.sharedMesh != null,
                    "Visible preview clothing: " + name);
                return renderer.sharedMesh.GetInstanceID();
            }).ToArray();
        }

        private const double LayoutSettleSeconds = 15;

        private static bool ShellLayoutResolved(VisualElement shell)
        {
            Rect panel = shell.panel.visualTree.layout;
            Rect window = shell.Q("lobby-shell-window").worldBound;
            Rect sidebar = shell.Q(className: "lobby-shell__sidebar").worldBound;
            return Mathf.Approximately(panel.width, Screen.width) && Mathf.Approximately(panel.height, Screen.height)
                && Mathf.Abs(window.width - 1180f) < 1f && Mathf.Abs(sidebar.width - 280f) < 1f;
        }

        private static bool CheckShell(VisualElement shell)
        {
            switch (_shellCheckStep++)
            {
                case 0:
                    float windowWidth = shell.Q("lobby-shell-window").worldBound.width;
                    float sidebarWidth = shell.Q(className: "lobby-shell__sidebar").worldBound.width;
                    Require(Mathf.Abs(windowWidth - 1180f) < 1f, $"Shell matches reference maximum width (got {windowWidth}, screen {Screen.width}x{Screen.height})");
                    Require(Mathf.Abs(sidebarWidth - 280f) < 1f, $"Shell has 280px sidebar (got {sidebarWidth})");
                    Require(shell.Q(className: "lobby-shell__preview-image").resolvedStyle.backgroundImage.renderTexture != null,
                        "Lobby starts with a live character preview");
                    Light[] previewLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(l => l.name.StartsWith("CharacterPreview")).ToArray();
                    Require(previewLights.Length == 2 && previewLights.All(l => l.enabled && l.gameObject.activeInHierarchy),
                        "Preview lighting survives station initialization");
                    CheckPreviewFraming();
                    _initialOutfitMeshes = PreviewOutfitMeshes();
                    Require(shell.Q(className: "lobby-shell__server-scroll") == null, "Server Info has no outer scroll view");
                    VisualElement info = shell.Q(className: "lobby-shell__server-information");
                    Require(info.worldBound.yMax <= shell.Q(className: "lobby-shell__tab-content").worldBound.yMax,
                        "Server Info content fits the panel");
                    VisualElement preview = shell.Q(className: "lobby-shell__preview");
                    VisualElement actions = shell.Q(className: "lobby-shell__actions");
                    Require(actions.worldBound.yMin - preview.worldBound.yMax >= 16f,
                        "Ready action has spacing below character preview");
                    Capture("00-server-info");
                    break;
                case 1: Submit(shell, "JOBS"); break;
                case 2:
                    Require(shell.Q(className: "lobby-shell__priority") != null, "Jobs has segmented priorities");
                    Capture("00-jobs");
                    break;
                case 3: Submit(shell, "CHAT"); break;
                case 4:
                    Require(shell.Q<TextField>(className: "lobby-shell__chat-input") != null, "Chat composer exists");
                    Capture("00-chat");
                    break;
                case 5: Submit(shell, "PLAYERS"); break;
                case 6:
                    Require(shell.Q(className: "lobby-shell__player-card") != null, "Players list exists");
                    Capture("00-players");
                    break;
                case 7: Submit(shell, "SETTINGS"); break;
                case 8:
                    Require(shell.Query(className: "lobby-shell__settings-tab").ToList().Count == 3, "Settings has three underlined subtabs");
                    foreach (string dropdown in new[] { "resolution", "windowMode", "frameCap", "quality", "renderScale", "upscaler", "antiAliasing" })
                        Require(shell.Q<DropdownField>("settings-" + dropdown) != null, $"Display has a {dropdown} dropdown");
                    Require(shell.Q<Slider>("settings-brightness") == null, "Brightness is gone (darkness keeps its gameplay meaning)");
                    shell.Q<DropdownField>("settings-quality").index = 0;
                    Capture("00-settings-display");
                    break;
                case 9:
                    Require(QualitySettings.GetQualityLevel() == 0, "Quality dropdown switches the quality level");
                    Submit(shell, "AUDIO");
                    break;
                case 10:
                    Require(shell.Query<Slider>(className: "lobby-shell__settings-slider").ToList().Count == 5,
                        "Sound has Master, Music, SFX, Ambience and Personal sliders");
                    Require(shell.Q<Toggle>("settings-muteUnfocused") != null, "Sound has Mute When Unfocused");
                    shell.Q<Slider>("settings-masterVolume").value = 0.35f;
                    shell.Q<Slider>("settings-musicVolume").value = 0.5f;
                    shell.Q<Toggle>("settings-muted").value = true;
                    break;
                case 11:
                {
                    Require(shell.Query<Slider>(className: "lobby-shell__settings-slider").ToList().All(s => !s.enabledInHierarchy), "Mute disables volume sliders");
                    Require(shell.Q<Label>("settings-value-masterVolume").text == "35", "Sound slider readout updates");
                    AudioMixer mixer = Resources.Load<AudioMixer>("MainMixer");
                    Require(mixer.GetFloat("MusicVolume", out float musicDb) && Mathf.Abs(musicDb - -6.02f) < 0.05f,
                        "Music slider drives the mixer's MusicVolume");
                    Require(mixer.GetFloat("MasterVolume", out float masterDb) && masterDb <= -80f, "Mute All silences MasterVolume");
                    Require(SavePlayerSettingsNow(), "Player settings save");
                    string settingsPath = Path.Combine(SessionState.GetString(OutputKey, ""), "settings.json");
                    SavedAudio saved = JsonUtility.FromJson<SavedSettings>(File.ReadAllText(settingsPath)).Audio;
                    Require(Mathf.Approximately(saved.Master, 0.35f) && Mathf.Approximately(saved.Music, 0.5f) && saved.MuteAll,
                        "Audio settings persist to the pilot's settings.json");
                    Capture("00-settings-muted");
                    break;
                }
                case 12: shell.Q<Toggle>("settings-muted").value = false; break;
                case 13:
                    Require(shell.Query<Slider>(className: "lobby-shell__settings-slider").ToList().All(s => s.enabledInHierarchy), "Unmute restores volume controls");
                    Capture("00-settings-sound");
                    break;
                case 14: Submit(shell, "CONTROLS"); break;
                case 15:
                {
                    List<Button> bindings = shell.Query<Button>(className: "lobby-shell__binding-button").ToList();
                    Require(bindings.Count >= 25, "Controls lists the player-visible actions from Controls.inputactions");
                    Require(bindings.Any(b => b.text == "Ctrl + Q" && b.enabledSelf), "Modifier chords show their fixed prefix and stay rebindable");
                    Require(bindings.Any(b => b.text == "Escape" && !b.enabledSelf), "Menu (Escape) is read-only");
                    Capture("00-settings-controls");
                    break;
                }
                case 16: Submit(shell, "E"); break;
                case 17:
                {
                    Button listening = shell.Q<Button>(className: "lobby-shell__binding-button--listening");
                    Require(listening != null && listening.text.StartsWith("Press a key"), "Clicking a binding starts key capture");
                    Submit(shell, listening.text);
                    break;
                }
                case 18:
                    Require(shell.Q<Button>(className: "lobby-shell__binding-button--listening") == null
                        && shell.Query<Button>(className: "lobby-shell__binding-button").ToList().Any(b => b.text == "E"),
                        "Clicking the listening binding cancels capture and keeps the key");
                    Submit(shell, "DISPLAY");
                    break;
                case 19:
                    Require(shell.Q<DropdownField>("settings-quality").value == "Low", "Display choices persist across subtabs");
                    Submit(shell, "SERVER INFO");
                    break;
                default: return true;
            }
            return false;
        }

        private static void Tick()
        {
            if (!_running) return;
            try
            {
                double now = EditorApplication.timeSinceStartup;
                if (now > _deadline) throw new TimeoutException("Lobby pilot exceeded 120 seconds (UI readiness or screenshot capture stalled).");
                if (now < _next) return;
                if (_pendingImage != null)
                {
                    if (!File.Exists(_pendingImage) || new FileInfo(_pendingImage).Length == 0) return;
                    _pendingImage = null;
                }
                VisualElement root = Root();
                if (root == null) return;
                VisualElement shell = root.Q("lobby-shell-surface");
                VisualElement creator = root.Q("character-creator-surface");
                switch (_step)
                {
                    case 0:
                        // The shell appears once the host session is online; wait for its layout.
                        if (shell == null || shell.Q("lobby-shell-window")?.worldBound.width < 1f)
                        {
                            _step--;
                            break;
                        }
                        // A slow first Play Mode can report a laid-out but not yet final shell; measure once
                        // the reference geometry resolves, or after LayoutSettleSeconds so a real regression fails.
                        if (_layoutDeadline == 0) _layoutDeadline = now + LayoutSettleSeconds;
                        if (!ShellLayoutResolved(shell) && now < _layoutDeadline)
                        {
                            _step--;
                            break;
                        }
                        Require(!LegacyLobbyCanvasPresent(), "No legacy uGUI lobby canvas in the scene");
                        if (!CheckShell(shell))
                        {
                            _step--;
                            break;
                        }
                        Require(shell.resolvedStyle.display != DisplayStyle.None, "Lobby shell visible");
                        Capture("01-lobby");
                        break;
                    case 1:
                        VisualElement preview = shell.Q(className: "lobby-shell__preview");
                        Require(preview != null && preview.worldBound.width > 0, "Lobby preview has layout");
                        using (ClickEvent evt = ClickEvent.GetPooled())
                        {
                            evt.target = preview;
                            preview.SendEvent(evt);
                        }
                        break;
                    case 2:
                        Require(creator != null && creator.resolvedStyle.display != DisplayStyle.None, "Character creator visible after preview click");
                        Require(shell.resolvedStyle.display == DisplayStyle.None, "Lobby hidden while creator open");
                        TextField field = creator.Q<TextField>();
                        Require(field != null, "Identity name field exists");
                        field.value = "Automation Pilot";
                        Require(creator.Q(className: "char-creator__preview-image").resolvedStyle.backgroundImage.renderTexture != null,
                            "Live character RenderTexture attached");
                        Require(creator.Q<DropdownField>("character-species") != null, "Species uses a dropdown");
                        Require(!creator.Query<Label>().ToList().Any(l => l.text == "UNIFORM COLOR"), "Uniform color removed from Identity");
                        Require(creator.Q<Button>(className: "char-creator__nav-btn").enabledInHierarchy == false,
                            "Back disabled on Identity");
                        Capture("02-identity");
                        break;
                    case 3: Submit(creator, "NEXT"); break;
                    case 4:
                        var sliders = creator.Query<Slider>().ToList();
                        Require(sliders.Count >= 6, "Body morph controls exist");
                        Require(!creator.Query<Label>().ToList().Any(l => l.text == "HAIR COLOR"), "Hair color removed from Body");
                        sliders[0].value = 0.6f;
                        sliders[1].value = 0.4f;
                        Submit(creator, "›");
                        break;
                    case 5:
                        Require(creator.Q<Label>(className: "char-creator__angle-label").text == "Side", "Preview changed to Side");
                        Require(PreviewOutfitMeshes().SequenceEqual(_initialOutfitMeshes), "Body edits preserve the chosen outfit");
                        Capture("03-body-side");
                        break;
                    case 6: Submit(creator, "NEXT"); break;
                    case 7:
                        switch (_styleCheckStep)
                        {
                            case 0:
                                Require(creator.Q("style-color-picker").Query<Label>().ToList().Any(l => l.text == "HAIR COLOR"), "Hair palette appears on Style");
                                Click(creator.Q("style-color-picker").Query(className: "char-creator__swatch").ToList()[2]);
                                break;
                            case 1: Submit(creator, "FACIAL HAIR"); break;
                            case 2:
                                Require(creator.Q("style-color-picker").Query<Label>().ToList().Any(l => l.text == "FACIAL HAIR COLOR"), "Facial Hair palette has its own label");
                                Click(creator.Q("style-color-picker").Query(className: "char-creator__swatch").ToList()[3]);
                                break;
                            case 3: Submit(creator, "EYEBROWS"); break;
                            case 4:
                                Require(creator.Q("style-color-picker").Query<Label>().ToList().Any(l => l.text == "EYEBROW COLOR"), "Eyebrow palette has its own label");
                                Click(creator.Q("style-color-picker").Query(className: "char-creator__swatch").ToList()[4]);
                                break;
                            case 5: Submit(creator, "HAIR"); break;
                            default:
                                Require(creator.Q("style-color-picker").Query(className: "char-creator__swatch").ToList()[2]
                                    .ClassListContains("char-creator__swatch--selected"), "Hair color survives changes to other style colors");
                                Require(creator.Q("style-color-picker").worldBound.yMin >= creator.Q(className: "char-creator__style-scroll").worldBound.yMax,
                                    "Color palette sits below style tiles");
                                Capture("04-style");
                                break;
                        }
                        if (_styleCheckStep++ < 6) _step--;
                        break;
                    case 8: Submit(creator, "NEXT"); break;
                    case 9:
                        Require(creator.Query<Label>().ToList().Any(l => l.text.Contains("Automation Pilot")), "Review contains edited name");
                        Require(creator.Query<Button>().ToList().Count(b => b.text == "SAVE CHARACTER") == 1,
                            "Review has one Save action in the navigation footer");
                        Button save = creator.Query<Button>().ToList().Single(b => b.text == "SAVE CHARACTER");
                        Require(save.ClassListContains("char-creator__nav-btn--save"), "Save uses the green footer action");
                        Require(save.worldBound.yMax <= creator.worldBound.yMax, "Save footer stays inside creator");
                        Capture("05-review");
                        break;
                    case 10: Submit(creator, "SAVE CHARACTER"); break;
                    case 11:
                        Require(creator.Query<Button>().ToList().Any(b => b.text == "SAVED" && !b.enabledInHierarchy),
                            "Save confirms Saved and prevents duplicate submission");
                        Submit(creator, "RETURN TO LOBBY");
                        break;
                    case 12:
                        Require(shell.resolvedStyle.display != DisplayStyle.None, "Lobby restored after Return");
                        Require(creator.resolvedStyle.display == DisplayStyle.None, "Creator hidden after Return");
                        Require(shell.Query<Label>().ToList().Any(l => l.text == "AUTOMATION PILOT"), "Saved name displayed in lobby");
                        Capture("06-saved-lobby");
                        break;
                    case 13: Click(shell.Q(className: "lobby-shell__preview")); break;
                    case 14: Click(creator.Q(className: "char-creator__loadout-btn")); break;
                    case 15:
                        List<VisualElement> loadoutRows = creator.Query(className: "char-creator__loadout-row").ToList();
                        Require(loadoutRows.Count == 1, "Only the one starter character ships before New Character");
                        Click(loadoutRows[0]);
                        break;
                    case 16:
                        Require(creator.Q<TextField>("character-name").value == "Automation Pilot", "Reselecting the character keeps its draft");
                        Click(creator.Q(className: "char-creator__loadout-btn"));
                        break;
                    case 17: Click(creator.Q(className: "char-creator__loadout-new")); break;
                    case 18:
                        Require(creator.Q<TextField>("character-name").value == "New Character", "New Character resets identity");
                        Submit(creator, "NEXT");
                        break;
                    case 19:
                        Require(Mathf.Approximately(creator.Q<Slider>("body-height").value, 0.5f)
                            && Mathf.Approximately(creator.Q<Slider>("body-female").value, 0f), "New Character resets body morphs");
                        Click(creator.Q(className: "char-creator__loadout-btn"));
                        break;
                    case 20:
                        Click(creator.Query(className: "char-creator__loadout-row").ToList()
                            .Single(e => e.Query<Label>().ToList().Any(l => l.text == "Automation Pilot")));
                        break;
                    case 21:
                        Require(creator.Q<TextField>("character-name").value == "Automation Pilot", "Saved character restores edited name");
                        Submit(creator, "NEXT");
                        break;
                    case 22:
                        Require(Mathf.Approximately(creator.Q<Slider>("body-height").value, 0.6f)
                            && Mathf.Approximately(creator.Q<Slider>("body-female").value, 0.4f), "Saved character retains edited body morphs");
                        Require(PreviewOutfitMeshes().SequenceEqual(_initialOutfitMeshes), "Saved character restores its chosen outfit");
                        Submit(creator, "RETURN TO LOBBY");
                        break;
                    case 23: Click(shell.Q(className: "lobby-shell__preview")); break;
                    case 24:
                        Click(creator.Query(className: "char-creator__rail-item").ToList()
                            .Single(e => e.Query<Label>().ToList().Any(l => l.text == "STYLE")));
                        break;
                    case 25:
                        Require(creator.Q("style-color-picker").Query(className: "char-creator__swatch").ToList()[2]
                            .ClassListContains("char-creator__swatch--selected"), "Saved draft restores hair color");
                        Submit(creator, "FACIAL HAIR");
                        break;
                    case 26:
                        Require(creator.Q("style-color-picker").Query(className: "char-creator__swatch").ToList()[3]
                            .ClassListContains("char-creator__swatch--selected"), "Saved draft restores independent facial hair color");
                        Submit(creator, "EYEBROWS");
                        break;
                    case 27:
                        Require(creator.Q("style-color-picker").Query(className: "char-creator__swatch").ToList()[4]
                            .ClassListContains("char-creator__swatch--selected"), "Saved draft restores independent eyebrow color");
                        Submit(creator, "RETURN TO LOBBY");
                        break;
                    default: Finish(null, true); return;
                }
                _step++;
                _next = now + 1.0;
            }
            catch (Exception exception) { Finish(exception.ToString(), true); }
        }

        [Serializable]
        private sealed class Report
        {
            public string status;
            public string failure;
            public string unityVersion;
            public int width, height;
            public string[] checks, errors, postconditions;
            public string scope = "Rendered lobby smoke; synthetic UI events, not OS pointer/keyboard coverage. Asserts the legacy uGUI lobby canvas is absent. Screenshots require visual review; no pixel baseline comparison.";
        }

        private static void Finish(string failure, bool stopPlay)
        {
            _running = false;
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            var report = new Report
            {
                status = failure == null && Errors.Count == 0 ? "passed" : "failed",
                failure = failure,
                unityVersion = Application.unityVersion,
                width = Screen.width, height = Screen.height,
                checks = Checks.ToArray(), errors = Errors.ToArray()
            };
            // Drivers wait for passed/failed; the final status follows the post-Play scene check.
            string result = report.status;
            report.status = "verifying";
            Write(report, SessionState.GetString(OutputKey, ""));
            report.status = result;
            _pending = report;
            if (stopPlay) EditorApplication.isPlaying = false;
        }

        private static void Complete(Report report, string output, string[] problems)
        {
            report.postconditions = problems;
            if (problems.Length > 0)
            {
                report.status = "failed";
                report.failure ??= "Postcondition: " + string.Join("; ", problems);
            }
            Write(report, output);
            Debug.Log($"[SS3D Automation] Lobby pilot {report.status}: {output}");
        }

        private static void Write(Report report, string output) =>
            File.WriteAllText(Path.Combine(output, "status.json"), JsonUtility.ToJson(report, true));
    }
}
