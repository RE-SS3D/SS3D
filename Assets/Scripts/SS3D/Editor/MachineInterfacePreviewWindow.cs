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
            ApplyStyles(_panel);
            _panel.style.alignSelf = Align.Center;
            _panel.style.marginTop = 16;
            _root.Add(_panel);

            _binder = new ApcPowerControllerBinder(_panel);
            _binder.Bind(_model);
        }

        private static void ApplyStyles(VisualElement root)
        {
            string[] stylePaths =
            {
                "Assets/Content/Systems/UI/Tokens/ss3d-tokens.uss",
                "Assets/Content/Systems/UI/Tokens/ss3d-typography.uss",
                "Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tokens.uss",
                "Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tones.uss",
                "Assets/Content/Systems/UI/MachineInterface/Templates/ApcPowerController.uss",
            };

            foreach (string path in stylePaths)
            {
                StyleSheet styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (styleSheet != null)
                {
                    root.styleSheets.Add(styleSheet);
                }
            }
        }
    }
}
#endif
