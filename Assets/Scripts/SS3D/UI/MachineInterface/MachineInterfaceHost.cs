using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.UI.MachineInterface.Bindings;
using SS3D.UI.MachineInterface.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface
{
    public class MachineInterfaceHost : View
    {
        public const string ApcInterfaceId = "power.apc";

        [SerializeField]
        private UIDocument _document;

        [SerializeField]
        private VisualTreeAsset _apcTemplate;

        [SerializeField]
        private StyleSheet _machineWindowStyle;

        [SerializeField]
        private StyleSheet _apcTemplateStyle;

        private VisualElement _overlayRoot;
        private MachineWindow _window;
        private ApcPowerControllerBinder _binder;
        private string _openInterfaceId;

        public bool IsOpen => _window != null && _window.style.display != DisplayStyle.None;

        public void Open(string interfaceId, ApcInterfaceViewModel viewModel)
        {
            if (interfaceId != ApcInterfaceId)
            {
                Debug.LogWarning($"Unknown machine interface id: {interfaceId}");
                return;
            }

            Close();

            _openInterfaceId = interfaceId;
            _window = new MachineWindow { Title = viewModel.Title };
            _window.style.left = Length.Percent(50);
            _window.style.top = Length.Percent(50);
            _window.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            _window.pickingMode = PickingMode.Position;

            if (_machineWindowStyle != null)
            {
                _window.styleSheets.Add(_machineWindowStyle);
            }

            TemplateContainer template = _apcTemplate.CloneTree();
            if (_apcTemplateStyle != null)
            {
                template.styleSheets.Add(_apcTemplateStyle);
            }

            _window.Content.Add(template);
            _overlayRoot.Add(_window);
            _overlayRoot.pickingMode = PickingMode.Position;

            _binder = new ApcPowerControllerBinder(_window);
            _binder.CloseRequested += HandleCloseRequested;
            _binder.ChannelToggled += HandleChannelToggled;
            _binder.Bind(viewModel);

            _window.CloseClicked += HandleCloseRequested;
        }

        public void Refresh(ApcInterfaceViewModel viewModel)
        {
            if (_binder == null)
            {
                return;
            }

            _binder.Bind(viewModel);
        }

        public void Close()
        {
            if (_binder != null)
            {
                _binder.CloseRequested -= HandleCloseRequested;
                _binder.ChannelToggled -= HandleChannelToggled;
                _binder = null;
            }

            if (_window != null)
            {
                _window.CloseClicked -= HandleCloseRequested;
                _window.RemoveFromHierarchy();
                _window = null;
            }

            _openInterfaceId = null;
            _overlayRoot.pickingMode = PickingMode.Ignore;
        }

        protected override void OnAwake()
        {
            base.OnAwake();

            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

#if UNITY_EDITOR
            EnsureEditorAssets();
#endif
            BuildOverlay();
            Hide();
        }

#if UNITY_EDITOR
        private void EnsureEditorAssets()
        {
            if (_apcTemplate == null)
            {
                _apcTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/ApcPowerController.uxml");
            }

            if (_machineWindowStyle == null)
            {
                _machineWindowStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Components/MachineWindow.uss");
            }

            if (_apcTemplateStyle == null)
            {
                _apcTemplateStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/ApcPowerController.uss");
            }
        }
#endif

        private void BuildOverlay()
        {
            _overlayRoot = _document.rootVisualElement;
            _overlayRoot.style.flexGrow = 1;
            _overlayRoot.pickingMode = PickingMode.Ignore;
        }

        private void Hide()
        {
            Close();
        }

        private void HandleCloseRequested()
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifyClosed(_openInterfaceId);
            }

            Close();
        }

        private void HandleChannelToggled(string channelId, bool isOn)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifyChannelToggled(channelId, isOn);
            }
        }
    }
}
