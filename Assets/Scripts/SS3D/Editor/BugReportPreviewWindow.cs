#if UNITY_EDITOR
using System.IO;
using SS3D.Systems.BugReports;
using SS3D.UI.Shell.BugReport;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    /// <summary>
    /// Visual fixture for the bug report dialog: the runtime <see cref="BugReportView"/> with sample data
    /// for each state of the "Bug Report v2" design. Report actions are inert here.
    /// </summary>
    public sealed class BugReportPreviewWindow : EditorWindow
    {
        public enum PreviewState { InRound, MapEditor, Saved, RateLimited, SuccessToast, MetadataExpanded, ScreenshotRemoved }
        private VisualElement _stage;
        private BugReportView _view;
        private Texture2D _frame;
        private PreviewState _state;

        [MenuItem("SS3D/UI/Preview Bug Report")]
        public static void OpenWindow()
        {
            BugReportPreviewWindow window = GetWindow<BugReportPreviewWindow>();
            window.titleContent = new GUIContent("Bug Report · Visual Preview");
            window.minSize = new Vector2(600, 480);
            window.ShowUtility();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            Toolbar toolbar = new();
            EnumField state = new(_state) { name = "preview-state" };
            state.RegisterValueChangedCallback(e => ShowState((PreviewState)e.newValue));
            toolbar.Add(state);
            toolbar.Add(new Label("Visual fixture · sample data · actions are inert"));
            rootVisualElement.Add(toolbar);
            _stage = new VisualElement { name = "preview-stage" };
            _stage.style.flexGrow = 1;
            _stage.style.backgroundColor = new Color(0.086f, 0.086f, 0.086f);
            rootVisualElement.Add(_stage);
            string fixture = Path.Combine(Directory.GetParent(UnityEngine.Application.dataPath).FullName,
                "Testing/ui/fixtures/bug-report-frame.png");
            if (File.Exists(fixture))
            {
                if (_frame != null) DestroyImmediate(_frame);
                _frame = new Texture2D(2, 2) { hideFlags = HideFlags.HideAndDontSave };
                _frame.LoadImage(File.ReadAllBytes(fixture));
                _stage.style.backgroundImage = _frame;
            }
            ShowState(_state);
        }

        public void ShowState(PreviewState state)
        {
            _state = state;
            rootVisualElement.Q<EnumField>("preview-state")?.SetValueWithoutNotify(state);
            _view?.Detach();
            _stage.Clear();
            BugReportAssetCatalog catalog = AssetDatabase.LoadAssetAtPath<BugReportAssetCatalog>(BugReportAssetPaths.Catalog);
            if (catalog == null || !catalog.HasRequiredAssets(out _))
            {
                _stage.Add(new Label("Rebuild All UI Catalogs to load the bug report visuals."));
                return;
            }

            VisualElement host = new() { pickingMode = PickingMode.Ignore };
            host.style.flexGrow = 1;
            host.styleSheets.Add(catalog.Ss3dTokensStyle);
            host.styleSheets.Add(catalog.Ss3dTypographyStyle);
            _stage.Add(host);
            _view = new BugReportView(catalog);
            _view.Attach(host);

            bool map = state == PreviewState.MapEditor;
            float now = (float)EditorApplication.timeSinceStartup;
            _view.ShowForm(new BugReportFormModel
            {
                Info = SampleInfo(state),
                Screenshot = _frame,
                ScreenshotTitle = "Screenshot · taken when you pressed F8",
                ScreenshotFile = "screenshot.png · 1920×1080",
                LogLines = 42,
                Local = map || state == PreviewState.Saved,
                MapOptionLabel = "Include current map (ministation, unsaved edits included)",
                MapOptionDefault = true,
                Summary = map ? "Area tool leaves ghost tiles after undo"
                    : state == PreviewState.RateLimited ? "Ghost follow camera snaps to map origin" : "",
                Severity = map ? BugReportSeverity.Major : BugReportSeverity.None,
            });

            switch (state)
            {
                case PreviewState.Saved:
                    _view.ShowSaved("Data/Exports/bug-1187-001432.zip");
                    break;
                case PreviewState.RateLimited:
                    _view.SetRateLimit(now + 42f, now);
                    break;
                case PreviewState.SuccessToast:
                    _view.Close();
                    _view.ShowToast("Bug report saved (round 1187). Thanks!", true, now);
                    break;
                case PreviewState.MetadataExpanded:
                    _view.SetMetadataExpanded(true);
                    break;
                case PreviewState.ScreenshotRemoved:
                    _view.SetScreenshotIncluded(false);
                    break;
            }
        }

        private static BugReportClientInfo SampleInfo(PreviewState state)
        {
            bool map = state is PreviewState.MapEditor or PreviewState.Saved;
            return new BugReportClientInfo
            {
                version = "0.0.9-prealpha · a3f9c1e",
                roundId = 1187,
                roundSeconds = 14 * 60 + 32,
                roundState = "Ongoing",
                map = map ? "ministation" : "Box Station",
                unsavedEdits = map,
                context = map ? "MAP EDITOR · AREA TOOL" : state == PreviewState.RateLimited ? "GHOST" : "IN ROUND · ALIVE",
                hasPosition = true,
                position = map ? new Vector3(12f, 0f, 4.5f) : new Vector3(142.6f, 0f, -38.2f),
                area = map ? "" : "Engineering / Main Hall",
                networkRole = map ? "Own host" : "Client",
                players = map ? 1 : 24,
                fps = map ? 144 : 58,
                os = "Windows 11",
                gpu = "RTX 3060",
                playerName = "Wrenchman",
            };
        }

        private void OnDisable()
        {
            _view?.Detach();
            if (_frame != null) DestroyImmediate(_frame);
        }
    }
}
#endif
