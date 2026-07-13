#if UNITY_EDITOR
using SS3D.UI.MachineInterface;
using SS3D.UI.MachineInterface.Bindings;
using SS3D.UI.MachineInterface.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    public class MachineInterfacePreviewWindow : EditorWindow
    {
        private ApcPowerControllerBinder _binder;
        private TemplateContainer _panel;
        private VisualElement _root;
        private ApcInterfaceViewModel _model = ApcInterfaceViewModel.CreateNominal();
        private bool _stylesApplied;

        [MenuItem("SS3D/Machine Interface/Preview APC Panel")]
        public static void OpenWindow()
        {
            MachineInterfacePreviewWindow window = GetWindow<MachineInterfacePreviewWindow>();
            window.titleContent = new GUIContent("APC Power Controller");
            window.minSize = new Vector2(1040, 720);
            window.Show();
        }

        private void CreateGUI()
        {
            _root = rootVisualElement;
            _root.style.flexGrow = 1;
            _root.style.backgroundColor = new Color(0.04f, 0.04f, 0.05f);

            ApplyDocumentStyles(_root);
            BuildToolbar();
            RebuildPanel();
        }

        private void BuildToolbar()
        {
            VisualElement toolbar = new();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.paddingLeft = 8;
            toolbar.style.paddingRight = 8;
            toolbar.style.paddingTop = 8;
            toolbar.style.paddingBottom = 8;

            toolbar.Add(CreateStateButton("Nominal", ApcPowerState.Nominal));
            toolbar.Add(CreateStateButton("Overload", ApcPowerState.Overload));
            toolbar.Add(CreateStateButton("Critical", ApcPowerState.Critical));

            _root.Add(toolbar);
        }

        private Button CreateStateButton(string label, ApcPowerState state)
        {
            Button button = new(() => SetState(state)) { text = label };
            button.style.marginRight = 6;
            return button;
        }

        private void SetState(ApcPowerState state)
        {
            _model = state switch
            {
                ApcPowerState.Overload => ApcInterfaceViewModel.CreateOverload(),
                ApcPowerState.Critical => ApcInterfaceViewModel.CreateCritical(),
                _ => ApcInterfaceViewModel.CreateNominal(),
            };

            _binder?.Bind(_model);
        }

        private void RebuildPanel()
        {
            _panel?.RemoveFromHierarchy();

            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Content/Systems/UI/MachineInterface/Templates/ApcPowerController.uxml");

            _panel = template != null ? template.CloneTree() : new TemplateContainer();
            DiegeticDeviceShell shell = _panel.Q<DiegeticDeviceShell>("device-shell");
            ApplyDiegeticPanelStyles(_panel, shell);
            _panel.style.alignSelf = Align.Center;
            _panel.style.marginTop = 16;
            _root.Add(_panel);

            _binder = new ApcPowerControllerBinder(_panel);
            _binder.Bind(_model);
        }

        private void ApplyDocumentStyles(VisualElement documentRoot)
        {
            if (_stylesApplied)
            {
                return;
            }

            AddStyleSheet(
                documentRoot,
                "Assets/Content/Systems/UI/MachineInterface/MachineInterfaceTheme.uss",
                "Assets/Content/Systems/UI/Tokens/ss3d-tokens.uss",
                "Assets/Content/Systems/UI/Tokens/ss3d-typography.uss",
                "Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tokens.uss",
                "Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tones.uss");

            _stylesApplied = true;
        }

        private static void ApplyDiegeticPanelStyles(TemplateContainer panel, DiegeticDeviceShell shell)
        {
            string[] stylePaths =
            {
                "Assets/Content/Systems/UI/MachineInterface/Templates/ApcPowerController.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DiegeticDeviceShell.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StatusDot.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ConnectionStatusRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceIdentityBlock.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/GlanceableStatusChip.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/PanelSection.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/PowerFlowRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/BatteryBar.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DiagnosticsList.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ChannelRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ToggleSwitch.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AccessGatePanel.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AccessGatedRegion.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AccessStrip.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/SteelButton.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/Badge.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StatusBadge.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss",
            };

            AddStyleSheets(panel, stylePaths);

            if (shell == null)
            {
                return;
            }

            AddStyleSheets(shell, stylePaths);

            if (shell.ScreenContent != null)
            {
                AddStyleSheets(shell.ScreenContent, stylePaths);
            }
        }

        private static void AddStyleSheets(VisualElement target, params string[] stylePaths)
        {
            foreach (string path in stylePaths)
            {
                AddStyleSheet(target, path);
            }
        }

        private static void AddStyleSheet(VisualElement target, params string[] stylePaths)
        {
            foreach (string path in stylePaths)
            {
                StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (styleSheet != null && !target.styleSheets.Contains(styleSheet))
                {
                    target.styleSheets.Add(styleSheet);
                }
            }
        }
    }
}
#endif
