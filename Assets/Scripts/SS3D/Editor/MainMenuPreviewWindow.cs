#if UNITY_EDITOR
using SS3D.UI.Shell.MainMenu;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    /// <summary>Explicit visual fixture. No game action is connected, including the preview's menu buttons.</summary>
    public sealed class MainMenuPreviewWindow : EditorWindow
    {
        public enum PreviewState { ReturningOutOfDate, ReturningCurrent, FirstLaunch, EmptyName, AdvancedPort,
            Servers, ServersOutOfDate, ServerDetails, FirstJoin, PublicEmpty, PublicUnavailable, AddressError,
            RefusedFull, RefusedTimeout, RefusedUnreachable, Settings, Connecting, Hosting, Updating }
        public enum PreviewResolution { HD1080, HD720, Ultrawide }
        private PreviewState _state;
        private PreviewResolution _resolution;
        private int _wallpaper;
        private VisualElement _stage;
        private VisualElement _frame;
        private MainMenuView _view;
        public MainMenuView View => _view;

        [MenuItem("SS3D/UI/Preview Main Menu")]
        public static void OpenWindow()
        {
            MainMenuPreviewWindow window = GetWindow<MainMenuPreviewWindow>();
            window.titleContent = new GUIContent("Main Menu · Visual Preview");
            window.minSize = new Vector2(800, 500);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            Toolbar toolbar = new();
            EnumField state = new(_state) { name = "preview-state" };
            state.RegisterValueChangedCallback(e => ShowState((PreviewState)e.newValue));
            toolbar.Add(state);
            EnumField resolution = new(_resolution) { name = "preview-resolution" };
            resolution.RegisterValueChangedCallback(e => SetResolution((PreviewResolution)e.newValue));
            toolbar.Add(resolution);
            ToolbarButton wallpaper = new(() => { _wallpaper++; ShowState(_state); }) { text = "Wallpaper" };
            toolbar.Add(wallpaper);
            toolbar.Add(new Label("Visual fixture · sample data · actions unwired"));
            rootVisualElement.Add(toolbar);
            _stage = new VisualElement { name = "preview-stage" };
            _stage.style.flexGrow = 1;
            _stage.style.overflow = Overflow.Hidden;
            _stage.style.backgroundColor = Color.black;
            rootVisualElement.Add(_stage);
            _frame = new VisualElement { name = "reference-frame" };
            _frame.style.position = Position.Absolute;
            _frame.style.transformOrigin = new TransformOrigin(0, 0);
            _stage.Add(_frame);
            _stage.RegisterCallback<GeometryChangedEvent>(_ => Fit());
            ShowState(_state);
        }

        public void SetResolution(PreviewResolution resolution)
        {
            _resolution = resolution;
            rootVisualElement.Q<EnumField>("preview-resolution")?.SetValueWithoutNotify(resolution);
            Fit();
        }

        private void Fit()
        {
            if (_frame == null) return;
            Vector2 size = _resolution == PreviewResolution.HD720 ? new Vector2(1280, 720)
                : _resolution == PreviewResolution.Ultrawide ? new Vector2(2560, 1080) : new Vector2(1920, 1080);
            _frame.style.width = size.x; _frame.style.height = size.y;
            float scale = Mathf.Min(_stage.contentRect.width / size.x, _stage.contentRect.height / size.y);
            if (float.IsNaN(scale) || scale <= 0) return;
            _frame.style.scale = new Scale(new Vector3(scale, scale, 1));
            _frame.style.left = (_stage.contentRect.width - size.x * scale) / 2;
            _frame.style.top = (_stage.contentRect.height - size.y * scale) / 2;
        }

        public void ShowState(PreviewState state)
        {
            _state = state;
            rootVisualElement.Q<EnumField>("preview-state")?.SetValueWithoutNotify(state);
            if (_frame == null) return;
            _view?.Detach();
            MainMenuAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<MainMenuAssetCatalog>(MainMenuAssetPaths.Catalog);
            if (catalog == null || !catalog.HasRequiredAssets(out _))
            {
                if (!MainMenuAssetCatalogBuilder.TryRebuildCatalog(out string error))
                { _frame.Add(new Label(error)); return; }
                catalog = AssetDatabase.LoadAssetAtPath<MainMenuAssetCatalog>(MainMenuAssetPaths.Catalog);
            }
            _view = new MainMenuView(catalog);
            _view.Attach(_frame);
            MainMenuPresentation model = Sample(state);
            model.Wallpaper = _wallpaper;
            _view.Present(model);
            Fit();
        }

        private static MainMenuServerPresentation Server(string id, string name, string address,
            MainMenuServerState state, string players, string ping, string version = "0.5.0") => new()
        { Id = id, Name = name, Address = address, State = state, Players = players, Ping = ping, RequiredVersion = version };

        public static MainMenuPresentation Sample(PreviewState state)
        {
            bool old = state is PreviewState.ReturningOutOfDate or PreviewState.ServersOutOfDate;
            bool first = state is PreviewState.FirstLaunch or PreviewState.FirstJoin;
            MainMenuServerPresentation sandbox = Server("sandbox", "RE:SS3D Sandbox", "sandbox.ss3d.space:1151", old ? MainMenuServerState.NeedsUpdate : MainMenuServerState.Compatible, "9/16", "41 ms");
            MainMenuServerPresentation kai = Server("kai", "Kai's build night", "82.14.203.9:1151", old ? MainMenuServerState.Compatible : MainMenuServerState.OlderVersion, "15/16", "22 ms", "0.4.2");
            MainMenuServerPresentation full = Server("medbay", "Medbay RP (EU)", "medbay-rp.eu:7780", MainMenuServerState.Full, "16/16", "58 ms");
            MainMenuServerPresentation offline = Server("oldbox", "Old test box", "192.168.1.44:1151", MainMenuServerState.Offline, "—", "—");
            MainMenuServerPresentation night = Server("night", "Night shift (NA)", "ns.ss3d-community.net:1151", MainMenuServerState.Compatible, "4/16", "134 ms");
            if (!first) { night.Refusal = "Timed out: no answer after 15 s."; night.RefusalTime = "21:47"; }
            MainMenuPresentation model = new()
            {
                MusicTitle = "Breach",
                PlayerName = state == PreviewState.EmptyName ? "" : "jmoore",
                Version = old ? "0.4.2" : "0.5.0", LatestVersion = "0.5.0", UpdateAvailable = old,
                UpdateMessage = "1 server in your list needs it.", FirstLaunch = first,
                Advanced = state == PreviewState.AdvancedPort,
                Recent = first ? System.Array.Empty<MainMenuServerPresentation>() : new[] { sandbox, kai, full, offline },
                Public = new[] { sandbox, Server("eng", "Engineering co-op", "eng-coop.ss3d.space:1151", MainMenuServerState.Compatible, "3/12", "33 ms"), night,
                    Server("honk", "Honk Station", "honk.example.org:1151", MainMenuServerState.Compatible, "11/20", "76 ms"),
                    Server("legacy", "Legacy 0.4 archive", "archive.ss3d.space:1151", MainMenuServerState.OlderVersion, "1/16", "47 ms", "0.4.1") },
                OperationName = "RE:SS3D Sandbox", OperationAddress = "sandbox.ss3d.space:1151", OperationStep = 1,
            };
            model.Screen = state switch
            {
                PreviewState.Settings => MainMenuScreen.Settings,
                PreviewState.Connecting => MainMenuScreen.Connecting,
                PreviewState.Hosting => MainMenuScreen.Hosting,
                PreviewState.Updating => MainMenuScreen.Updating,
                PreviewState.Servers or PreviewState.ServersOutOfDate or PreviewState.ServerDetails or PreviewState.FirstJoin or PreviewState.PublicEmpty
                    or PreviewState.PublicUnavailable or PreviewState.AddressError or PreviewState.RefusedFull
                    or PreviewState.RefusedTimeout or PreviewState.RefusedUnreachable => MainMenuScreen.Servers,
                _ => MainMenuScreen.Menu,
            };
            if (!first)
            {
                System.Collections.Generic.List<MainMenuServerPresentation> publicOnly = new(model.Public);
                publicOnly.RemoveAt(0);
                model.Public = publicOnly;
            }
            if (state == PreviewState.ServerDetails) { sandbox.Expanded = true; sandbox.AdminToken = "preview-token"; }
            if (state is PreviewState.PublicEmpty or PreviewState.PublicUnavailable)
            {
                model.Public = System.Array.Empty<MainMenuServerPresentation>();
                model.PublicList = state == PreviewState.PublicEmpty ? MainMenuPublicListState.Empty : MainMenuPublicListState.FetchFailed;
            }
            if (state == PreviewState.AddressError) { model.Address = "server:abc"; model.AddressError = "Port must be a number between 1 and 65535."; }
            if (state is PreviewState.RefusedFull or PreviewState.RefusedTimeout or PreviewState.RefusedUnreachable)
            {
                sandbox.Refusal = state == PreviewState.RefusedFull ? "Refused: the server is full." : state == PreviewState.RefusedTimeout
                    ? "Timed out: no answer after 15 s." : "Unreachable: nothing answered at this address.";
                sandbox.RefusalTime = "21:48";
            }
            if (state == PreviewState.Hosting) { model.OperationName = "Local session"; model.OperationAddress = "Port 1151"; }
            if (state == PreviewState.Updating) { model.OperationName = "v0.5.0"; model.OperationAddress = "Keeps maps and settings"; }
            return model;
        }

        /// <summary>Capture the rendered reference frame, excluding editor toolbar and letterboxing.</summary>
        public string CaptureFrame(string path)
        {
            int width = Mathf.RoundToInt(position.width);
            int height = Mathf.RoundToInt(position.height);
            RenderTexture target = new(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            target.Create();
            RenderTexture previous = RenderTexture.active;
            Texture2D image = new(width, height, TextureFormat.RGB24, false);
            Texture2D frame = null;
            try
            {
                if (!UnityEditorInternal.InternalEditorUtility.CaptureEditorWindow(this, target))
                    throw new System.InvalidOperationException("Could not capture the main menu preview window.");
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                image.Apply();
                Rect bounds = _frame.worldBound;
                Rect root = rootVisualElement.worldBound;
                int x = Mathf.Clamp(Mathf.RoundToInt(bounds.x - root.x), 0, width - 1);
                int y = Mathf.Clamp(Mathf.RoundToInt(height - (bounds.y - root.y) - bounds.height), 0, height - 1);
                int cropWidth = Mathf.Min(Mathf.RoundToInt(bounds.width), width - x);
                int cropHeight = Mathf.Min(Mathf.RoundToInt(bounds.height), height - y);
                frame = new Texture2D(cropWidth, cropHeight, TextureFormat.RGB24, false);
                Color32[] source = image.GetPixels32();
                Color32[] cropped = new Color32[cropWidth * cropHeight];
                for (int row = 0; row < cropHeight; row++)
                    System.Array.Copy(source, (y + row) * width + x, cropped, row * cropWidth, cropWidth);
                frame.SetPixels32(cropped);
                frame.Apply();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
                System.IO.File.WriteAllBytes(path, frame.EncodeToPNG());
                return path;
            }
            finally
            {
                RenderTexture.active = previous;
                target.Release();
                DestroyImmediate(target);
                DestroyImmediate(image);
                if (frame != null) DestroyImmediate(frame);
            }
        }

        private void OnDisable() => _view?.Detach();
    }
}
#endif
