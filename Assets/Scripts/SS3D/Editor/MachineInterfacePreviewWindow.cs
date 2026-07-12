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
        private MachineWindow _window;
        private VisualElement _root;
        private ApcInterfaceViewModel _model = ApcInterfaceViewModel.CreateNominal();

        [MenuItem("SS3D/Machine Interface/Preview APC Panel")]
        public static void OpenWindow()
        {
            MachineInterfacePreviewWindow window = GetWindow<MachineInterfacePreviewWindow>();
            window.titleContent = new GUIContent("APC Power Controller");
            window.minSize = new Vector2(480, 640);
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
            _window?.RemoveFromHierarchy();

            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Content/Systems/UI/MachineInterface/Templates/ApcPowerController.uxml");
            StyleSheet windowStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Content/Systems/UI/MachineInterface/Components/MachineWindow.uss");
            StyleSheet templateStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Content/Systems/UI/MachineInterface/Templates/ApcPowerController.uss");

            _window = new MachineWindow { Title = _model.Title };
            _window.style.alignSelf = Align.Center;
            _window.style.marginTop = 16;

            if (windowStyle != null)
            {
                _window.styleSheets.Add(windowStyle);
            }

            TemplateContainer content = template != null ? template.CloneTree() : new TemplateContainer();
            if (templateStyle != null)
            {
                content.styleSheets.Add(templateStyle);
            }

            _window.Content.Add(content);
            _root.Add(_window);

            _binder = new ApcPowerControllerBinder(_window);
            _binder.Bind(_model);
        }
    }
}
#endif
