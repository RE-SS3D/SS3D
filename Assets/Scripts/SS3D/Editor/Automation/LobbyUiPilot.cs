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
        private static int _shellCheckStep;
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

        // Guards: lobby shell, settings, character window and switcher render and respond (rounds-lobby UI contracts) · Not cheaper because: needs rendered UI Toolkit panels and screenshots a human reviews; weekly graphical run
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
            // A Button acts on submit; a plain element (a switcher row) listens for ClickEvent.
            if (element is Button button)
            {
                using NavigationSubmitEvent submit = NavigationSubmitEvent.GetPooled();
                submit.target = button;
                button.SendEvent(submit);
                return;
            }

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

        /// <summary>Like <see cref="Submit"/>, for a button found by name (segments and key rows have no text of their own).</summary>
        private static void Press(VisualElement surface, string name)
        {
            Button button = surface.Q<Button>(name);
            Require(button != null && button.enabledInHierarchy && button.worldBound.width > 0, "Enabled, laid-out button: " + name);
            button.Focus();
            using (NavigationSubmitEvent evt = NavigationSubmitEvent.GetPooled())
            {
                evt.target = button;
                button.SendEvent(evt);
            }
        }

        private static string _pilotBind;
        private static string _pilotBindKey;

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

        /// <summary>LobbyView.CompactWidth: below it the shell takes the 1366 × 768 layout.</summary>
        private const float CompactWidth = 1600f;

        private static bool ShellLayoutResolved(VisualElement shell)
        {
            Rect panel = shell.panel.visualTree.layout;
            Rect window = shell.Q("lobby-shell-window").worldBound;
            Rect sidebar = shell.Q("lobby-sidebar").worldBound;
            float expected = panel.width < CompactWidth ? 320f : 400f;
            return Mathf.Approximately(panel.width, Screen.width) && Mathf.Approximately(panel.height, Screen.height)
                && Mathf.Abs(sidebar.width - expected) < 1f && window.xMin > sidebar.xMax;
        }

        private static bool CheckShell(VisualElement shell)
        {
            switch (_shellCheckStep++)
            {
                case 0:
                    Rect panelRect = shell.panel.visualTree.layout;
                    float sidebarWidth = shell.Q("lobby-sidebar").worldBound.width;
                    float expectedSidebar = panelRect.width < CompactWidth ? 320f : 400f;
                    Require(Mathf.Abs(sidebarWidth - expectedSidebar) < 1f,
                        $"Sidebar is {expectedSidebar}px at {panelRect.width}px wide (got {sidebarWidth})");
                    Require(shell.ClassListContains("lobby--compact") == panelRect.width < CompactWidth,
                        "The 1366 layout class follows the panel width");
                    Require(shell.Q("lobby-preview-image").resolvedStyle.backgroundImage.renderTexture != null,
                        "Lobby starts with a live character preview");
                    Light[] previewLights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                        .Where(l => l.name.StartsWith("CharacterPreview")).ToArray();
                    Require(previewLights.Length == 2 && previewLights.All(l => l.enabled && l.gameObject.activeInHierarchy),
                        "Preview lighting survives station initialization");
                    CheckPreviewFraming();
                    PreviewOutfitMeshes();
                    VisualElement info = shell.Q("server-info");
                    Require(info.worldBound.yMax <= shell.Q("lobby-tab-content").worldBound.yMax + 1f,
                        "Server Info content fits the panel");
                    Require(shell.Q("lobby-ready").worldBound.yMin >= shell.Q("lobby-character").worldBound.yMax + 8f,
                        "Ready sits below the character card");
                    Require(shell.Q("lobby-quit").worldBound.yMax <= panelRect.height + 1f,
                        "The sidebar footer fits on screen");
                    Require(shell.Q("tab-jobs") == null && shell.Q("tab-players") == null,
                        "No job list or player list in the lobby (lobby.md §8)");
                    Capture("00-server-info");
                    break;
                case 1: Press(shell, "tab-chat"); break;
                case 2:
                    Require(shell.Q<TextField>("chat-input") != null, "Chat composer exists");
                    Require(shell.Q<Button>("chat-channel-ooc") != null && shell.Q<Button>("chat-channel-looc") != null,
                        "Chat has OOC and LOOC channel buttons");
                    Capture("00-chat");
                    break;
                case 3: Submit(shell, "SETTINGS"); break;
                case 4:
                    Require(shell.Query(className: "settings-panel__tab").ToList().Count == 3, "Settings has Display, Audio and Controls tabs");
                    Require(shell.Q<DropdownField>("settings-resolution") != null, "Display has a resolution dropdown");
                    foreach (string row in new[] { "windowMode", "vsync", "frameCap", "quality", "renderScale", "upscaler", "antiAliasing", "showFps" })
                        Require(shell.Q<Button>($"settings-{row}-0") != null, $"Display has a {row} row");
                    Require(shell.Q("settings-row-brightness") == null, "Brightness is gone (darkness keeps its gameplay meaning)");
                    Require(!shell.Q<Button>("settings-frameCap-0").enabledSelf, "Frame cap is locked while VSync is on");
                    Press(shell, "settings-quality-0");
                    Capture("00-settings-display");
                    break;
                case 5:
                    Require(QualitySettings.GetQualityLevel() == 0, "Quality preset switches the quality level");
                    Require(shell.Q<Button>("settings-quality-0").ClassListContains("settings-panel__segment--on"), "The picked preset shows as selected");
                    Submit(shell, "AUDIO");
                    break;
                case 6:
                    Require(shell.Query<SliderInt>(className: "settings-panel__slider").ToList().Count == 5,
                        "Audio has Master, Music, SFX, Ambience and Personal sliders");
                    Require(shell.Q<Button>("settings-muteUnfocused-1") != null, "Audio has Mute when window unfocused");
                    Require(shell.Q<Button>("settings-menuMusic-1") != null, "Audio has Menu music");
                    shell.Q<SliderInt>("settings-masterVolume").value = 35;
                    shell.Q<SliderInt>("settings-musicVolume").value = 50;
                    Press(shell, "settings-muteAll-1");
                    break;
                case 7:
                {
                    Require(shell.Q(className: "settings-panel__banner").resolvedStyle.display == DisplayStyle.Flex, "Mute All shows the muted banner");
                    Require(shell.Query<SliderInt>(className: "settings-panel__slider").ToList().All(s => s.enabledInHierarchy),
                        "Levels stay changeable while muted");
                    Require(shell.Q<Label>("settings-value-masterVolume").text == "35 %", "Audio slider readout updates");
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
                case 8: Press(shell, "settings-muteAll-0"); break;
                case 9:
                    Require(shell.Q(className: "settings-panel__banner").resolvedStyle.display == DisplayStyle.None, "Unmute hides the muted banner");
                    Capture("00-settings-sound");
                    break;
                case 10: Submit(shell, "CONTROLS"); break;
                case 11:
                {
                    List<Button> bindings = shell.Query<Button>(className: "settings-panel__bind").ToList();
                    Require(bindings.Count >= 20, "Controls lists the rebindable actions from Controls.inputactions");
                    Require(bindings.Any(b => b.Q<Label>(className: "settings-panel__prefix")?.text == "Ctrl +"
                        && b.Q<Label>(className: "settings-panel__keycap").text == "Q"), "Modifier chords show their fixed prefix and stay rebindable");
                    Require(shell.Query<Label>(className: "settings-panel__fixed-key").ToList().Any(l => l.text == "Escape"), "Menu (Escape) is fixed");
                    Capture("00-settings-controls");
                    break;
                }
                case 12:
                    _pilotBind = shell.Query<Button>(className: "settings-panel__bind").First().name;
                    _pilotBindKey = shell.Q<Button>(_pilotBind).Q<Label>(className: "settings-panel__keycap").text;
                    Press(shell, _pilotBind);
                    break;
                case 13:
                {
                    Button listening = shell.Q<Button>(_pilotBind);
                    Require(listening.ClassListContains("settings-panel__bind--listening")
                        && listening.Q<Label>(className: "settings-panel__keycap").text == "Press a key", "Clicking a binding starts key capture");
                    Press(shell, _pilotBind);
                    break;
                }
                case 14:
                    Require(shell.Q<Button>(className: "settings-panel__bind--listening") == null
                        && shell.Q<Button>(_pilotBind).Q<Label>(className: "settings-panel__keycap").text == _pilotBindKey,
                        "Clicking the listening binding cancels capture and keeps the key");
                    Submit(shell, "DISPLAY");
                    break;
                case 15:
                    Require(shell.Q<Button>("settings-quality-0").ClassListContains("settings-panel__segment--on"), "Display choices persist across tabs");
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
                VisualElement window = root.Q("character-window-surface");
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
                        Press(shell, "lobby-character");
                        break;
                    case 2:
                        Require(window != null && window.resolvedStyle.display != DisplayStyle.None, "Character window visible after the card click");
                        Require(shell.resolvedStyle.display == DisplayStyle.None, "Lobby hidden while the window is open");
                        Require(window.Q("charwin-preview-image").resolvedStyle.backgroundImage.renderTexture != null,
                            "Live character RenderTexture attached");
                        Require(window.Q<Label>("charwin-status-label").text == "Saved", "Opens Saved");
                        Require(!window.Q<Button>("charwin-save").enabledInHierarchy, "Save waits for a change");
                        Require(window.Q("charwin-randomize-look") != null && window.Q("charwin-randomize-name") != null, "Randomize buttons exist");
                        Require(window.Query<Slider>().ToList().Count(slider => slider.name.StartsWith("body-")) == 6, "Six body sliders");
                        Require(window.Q("charwin-hair-styles").childCount >= 2 && window.Q("charwin-beard-styles").childCount >= 2
                            && window.Q("charwin-eyebrow-styles").childCount >= 2, "Hair, facial hair and eyebrow grids");
                        window.Q<TextField>("charwin-name").value = "Pilot";
                        break;
                    case 3:
                        Require(!window.Q("charwin-name-error").ClassListContains("charwin-look__error--hidden"), "A one-word name shows its error");
                        Require(!window.Q<Button>("charwin-save").enabledInHierarchy, "Save stays disabled while the name is invalid");
                        Capture("02-appearance-name-error");
                        break;
                    case 4:
                        window.Q<TextField>("charwin-name").value = "automation pilot";
                        window.Q<Slider>("body-height").value = 0.6f;
                        window.Q<Slider>("body-female").value = 0.4f;
                        Click(window.Q("charwin-hair-colour").Children().ElementAt(2));
                        Click(window.Q("charwin-beard-colour").Children().ElementAt(3));
                        Click(window.Q("charwin-eyebrow-colour").Children().ElementAt(4));
                        break;
                    case 5:
                        Require(window.Q("charwin-name-error").ClassListContains("charwin-look__error--hidden"), "A first and last name clears the error");
                        Require(window.Q<Label>("charwin-status-label").text == "Unsaved changes", "Edits show Unsaved changes");
                        Require(window.Q<Button>("charwin-save").enabledInHierarchy, "Save enabled with a valid change");
                        Require(window.Q("charwin-hair-colour").Children().ElementAt(2).ClassListContains("charwin-look__swatch--selected")
                            && window.Q("charwin-beard-colour").Children().ElementAt(3).ClassListContains("charwin-look__swatch--selected")
                            && window.Q("charwin-eyebrow-colour").Children().ElementAt(4).ClassListContains("charwin-look__swatch--selected"),
                            "Hair, facial hair and eyebrow colours are independent");
                        Capture("03-appearance");
                        break;
                    case 6: Press(window, "charwin-rotate-right"); break;
                    case 7:
                        Require(window.Q("charwin-angles").Children().ElementAt(1).ClassListContains("charwin__angle--active"), "Preview rotated to Side");
                        Capture("04-appearance-side");
                        break;
                    case 8: Press(window, "charwin-rotate-left"); break;
                    case 9: Press(window, "charwin-tab-jobs"); break;
                    case 10:
                        Require(window.Q("charwin-jobs") != null && window.Q("charwin-job-Security") != null, "Jobs tab lists the server's jobs");
                        Require(window.Q("charwin-fallback") != null, "The fallback choice is pinned under the list");
                        Press(window.Q("charwin-job-Engineer"), "charwin-prio-high");
                        break;
                    case 11:
                        Press(window.Q("charwin-job-Security"), "charwin-prio-high");
                        break;
                    case 12:
                        Require(window.Q("charwin-job-Engineer").Q<Button>("charwin-prio-medium").ClassListContains("charwin-jobs__prio-option--medium-on"),
                            "A new High drops the old High to Medium");
                        Require(!window.Q("charwin-jobs-notice").ClassListContains("charwin__notice--hidden"), "The notice says what moved");
                        Require(window.Q<Label>("charwin-outfit-caption").text.Contains("Security"), "The preview wears the High job's clothes");
                        Capture("05-jobs");
                        break;
                    case 13: Press(window, "charwin-show-clothing"); break;
                    case 14:
                        Require(!GameObject.Find("CharacterPreviewDummy").GetComponentsInChildren<SkinnedMeshRenderer>(true)
                            .Single(r => r.name == "Jumpsuit").gameObject.activeInHierarchy, "Show clothing off strips the preview");
                        Press(window, "charwin-show-clothing");
                        break;
                    case 15:
                        PreviewOutfitMeshes();
                        Press(window, "charwin-tab-antags");
                        break;
                    case 16:
                        Require(window.Q("charwin-antags") != null && window.Q("charwin-antags-note") != null, "Antags tab with the mode-secrecy note");
                        Capture("06-antags");
                        break;
                    case 17: Press(window, "charwin-save"); break;
                    case 18:
                        Require(window.Q<Label>("charwin-status-label").text == "Saved", "Save shows Saved");
                        Require(window.Q<Label>("charwin-switch-name").text == "Automation Pilot", "Save normalises the name");
                        Press(window, "charwin-return");
                        break;
                    case 19:
                        Require(shell.resolvedStyle.display != DisplayStyle.None, "Lobby restored after Return");
                        Require(window.resolvedStyle.display == DisplayStyle.None, "Window hidden after Return");
                        Require(shell.Q<Label>("lobby-character-name").text == "Automation Pilot", "Saved name on the lobby card");
                        Require(shell.Q<Label>("lobby-high-job").text == "Security", "Saved High job on the lobby card");
                        Capture("07-saved-lobby");
                        break;
                    case 20: Press(shell, "lobby-switch-character"); break;
                    case 21:
                        Require(shell.Q("character-switcher").ClassListContains("switcher--open"), "Switch character opens the switcher");
                        Require(shell.Query("character-switcher-row").ToList().Count == 1, "A first run has one character");
                        Capture("08-switcher");
                        break;
                    case 22: Press(shell, "character-switcher-new"); break;
                    case 23:
                        Require(shell.Query("character-switcher-row").ToList().Count == 2, "New adds a character");
                        Require(shell.Q<Label>("lobby-character-name").text != "Automation Pilot", "New selects the new random character");
                        Click(shell.Query("character-switcher-row").ToList()
                            .Single(row => row.Query<Label>().ToList().Any(l => l.text == "Automation Pilot")));
                        break;
                    case 24:
                        Require(shell.Q<Label>("lobby-character-name").text == "Automation Pilot", "Selecting a row switches the card");
                        Require(!shell.Q("character-switcher").ClassListContains("switcher--open"), "Selecting closes the switcher");
                        Press(shell, "lobby-character");
                        break;
                    case 25:
                        Require(window.Q<TextField>("charwin-name").value == "Automation Pilot", "The window opens on the selected character");
                        Require(Mathf.Approximately(window.Q<Slider>("body-height").value, 0.6f)
                            && Mathf.Approximately(window.Q<Slider>("body-female").value, 0.4f), "Saved body sliders restored");
                        Require(window.Q("charwin-hair-colour").Children().ElementAt(2).ClassListContains("charwin-look__swatch--selected"),
                            "Saved hair colour restored");
                        window.Q<Slider>("body-fat").value = 0.9f;
                        Press(window, "charwin-return");
                        break;
                    case 26:
                        Require(window.Q("charwin-prompt").ClassListContains("charwin__prompt--open"), "Return with unsaved changes asks first");
                        Capture("09-unsaved-prompt");
                        break;
                    case 27: Press(window, "charwin-prompt-discard"); break;
                    case 28:
                        Require(window.resolvedStyle.display == DisplayStyle.None && shell.resolvedStyle.display != DisplayStyle.None,
                            "Discard returns to the lobby");
                        Press(shell, "lobby-switch-character");
                        break;
                    case 29:
                        VisualElement other = shell.Query("character-switcher-row").ToList()
                            .Single(row => !row.Query<Label>().ToList().Any(l => l.text == "Automation Pilot"));
                        Press(other, "character-switcher-trash");
                        break;
                    case 30:
                        Require(shell.Q("character-switcher-confirm") != null, "Delete asks for confirmation");
                        Press(shell, "character-switcher-confirm-delete");
                        break;
                    case 31:
                        Require(shell.Query("character-switcher-row").ToList().Count == 1, "Delete removes the character");
                        shell.Q("character-switcher").RemoveFromClassList("switcher--open");
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
