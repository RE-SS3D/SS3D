using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.UI.MachineInterface.Bindings;
using SS3D.UI.MachineInterface.Components;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Renders machine interface panels via UI Toolkit.
    /// UIDocument stays disabled while closed so the panel does not clear camera depth
    /// used by the selection pick pass.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
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
        private bool _overlayReady;

        public bool IsOpen => _window != null && _window.style.display != DisplayStyle.None;

        public bool Open(string interfaceId, ApcInterfaceViewModel viewModel)
        {
            if (!EnsureDocumentActive())
            {
                return false;
            }

            if (interfaceId != ApcInterfaceId)
            {
                Debug.LogWarning($"Unknown machine interface id: {interfaceId}");
                return false;
            }

            if (_apcTemplate == null)
            {
                Debug.LogError("MachineInterfaceHost is missing the APC template.", this);
                return false;
            }

            ClosePanelOnly();

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
            SetOverlayInteractive(true);

            _binder = new ApcPowerControllerBinder(_window);
            _binder.CloseRequested += HandleCloseRequested;
            _binder.ChannelToggled += HandleChannelToggled;
            _binder.Bind(viewModel);

            _window.CloseClicked += HandleCloseRequested;
            return true;
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
            ClosePanelOnly();
            ShutdownDocument();
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
            ShutdownDocument();
        }

        protected override void OnDestroyed()
        {
            Close();
            base.OnDestroyed();
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

            if (_document != null && _document.panelSettings == null)
            {
                _document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    "Assets/Content/Systems/UI/MachineInterface/MachineInterfacePanelSettings.asset");
            }
        }
#endif

        private void ClosePanelOnly()
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
            SetOverlayInteractive(false);
        }

        private void ShutdownDocument()
        {
            _overlayReady = false;
            _overlayRoot = null;

            if (_document != null)
            {
                _document.enabled = false;
            }
        }

        private bool EnsureDocumentActive()
        {
            if (_document == null)
            {
                _document = GetComponent<UIDocument>();
            }

            if (_document == null)
            {
                Debug.LogError("MachineInterfaceHost requires a UIDocument on the same GameObject.", this);
                return false;
            }

            if (!_document.enabled)
            {
                _document.enabled = true;
                _overlayReady = false;
                _overlayRoot = null;
            }

            return EnsureOverlay();
        }

        private bool EnsureOverlay()
        {
            if (_overlayReady)
            {
                return true;
            }

            VisualElement root = _document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError(
                    "MachineInterfaceHost could not access UIDocument.rootVisualElement. Assign Panel Settings on the UIDocument.",
                    this);
                return false;
            }

            _overlayRoot = root;
            _overlayRoot.style.flexGrow = 0;
            _overlayReady = true;
            SetOverlayInteractive(false);
            return true;
        }

        private void SetOverlayInteractive(bool interactive)
        {
            if (_overlayRoot == null)
            {
                return;
            }

            if (interactive)
            {
                _overlayRoot.style.display = DisplayStyle.Flex;
                _overlayRoot.style.flexGrow = 1;
                _overlayRoot.pickingMode = PickingMode.Position;
            }
            else
            {
                _overlayRoot.style.display = DisplayStyle.None;
                _overlayRoot.style.flexGrow = 0;
                _overlayRoot.pickingMode = PickingMode.Ignore;
            }
        }

        private void HandleCloseRequested()
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.RequestCloseFromUi(_openInterfaceId);
            }
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
