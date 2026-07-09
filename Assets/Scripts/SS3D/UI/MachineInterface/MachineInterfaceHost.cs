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

        public const string SmesInterfaceId = "power.smes";

        [SerializeField]
        private UIDocument _document;

        [SerializeField]
        private VisualTreeAsset _apcTemplate;

        [SerializeField]
        private VisualTreeAsset _smesTemplate;

        [SerializeField]
        private StyleSheet _machineWindowStyle;

        [SerializeField]
        private StyleSheet _apcTemplateStyle;

        [SerializeField]
        private StyleSheet _smesTemplateStyle;

        private VisualElement _overlayRoot;
        private MachineWindow _window;
        private ApcPowerControllerBinder _apcBinder;
        private SmesUnitBinder _smesBinder;
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
                Debug.LogWarning($"MachineInterfaceHost expected APC interface id but received: {interfaceId}");
                return false;
            }

            if (_apcTemplate == null)
            {
                Debug.LogError("MachineInterfaceHost is missing the APC template.", this);
                return false;
            }

            ClosePanelOnly();
            if (!CreateWindow(viewModel.Title))
            {
                return false;
            }

            _openInterfaceId = interfaceId;
            TemplateContainer template = _apcTemplate.CloneTree();
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, _apcTemplateStyle);
            _window.Content.Add(template);
            _overlayRoot.Add(_window);
            SetOverlayInteractive(true);

            _apcBinder = new ApcPowerControllerBinder(_window);
            _apcBinder.CloseRequested += HandleCloseRequested;
            _apcBinder.ChannelToggled += HandleChannelToggled;
            _apcBinder.Bind(viewModel);
            _window.CloseClicked += HandleCloseRequested;
            return true;
        }

        public bool Open(string interfaceId, SmesInterfaceViewModel viewModel)
        {
            if (!EnsureDocumentActive())
            {
                return false;
            }

            if (interfaceId != SmesInterfaceId)
            {
                Debug.LogWarning($"MachineInterfaceHost expected SMES interface id but received: {interfaceId}");
                return false;
            }

            if (_smesTemplate == null)
            {
                Debug.LogError("MachineInterfaceHost is missing the SMES template.", this);
                return false;
            }

            ClosePanelOnly();
            if (!CreateWindow(viewModel.Title, wide: true))
            {
                return false;
            }

            _openInterfaceId = interfaceId;
            TemplateContainer template = _smesTemplate.CloneTree();
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, _smesTemplateStyle);
            _window.Content.Add(template);
            _overlayRoot.Add(_window);
            SetOverlayInteractive(true);

            _smesBinder = new SmesUnitBinder(_window);
            _smesBinder.CloseRequested += HandleCloseRequested;
            _smesBinder.InputToggled += HandleSmesInputToggled;
            _smesBinder.OutputToggled += HandleSmesOutputToggled;
            _smesBinder.InputRateDeltaRequested += HandleSmesInputRateDelta;
            _smesBinder.OutputRateDeltaRequested += HandleSmesOutputRateDelta;
            _smesBinder.Bind(viewModel);
            _window.CloseClicked += HandleCloseRequested;
            return true;
        }

        public void Refresh(ApcInterfaceViewModel viewModel)
        {
            _apcBinder?.Bind(viewModel);
        }

        public void Refresh(SmesInterfaceViewModel viewModel)
        {
            _smesBinder?.Bind(viewModel);
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

            if (_smesTemplate == null)
            {
                _smesTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/SmesUnitInterface.uxml");
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

            if (_smesTemplateStyle == null)
            {
                _smesTemplateStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/SmesUnitInterface.uss");
            }

            if (_document != null && _document.panelSettings == null)
            {
                _document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    "Assets/Content/Systems/UI/MachineInterface/MachineInterfacePanelSettings.asset");
            }
        }
#endif

        private bool CreateWindow(string title, bool wide = false)
        {
            _window = new MachineWindow { Title = title };
            if (wide)
            {
                _window.AddToClassList("machine-window--wide");
            }

            _window.style.left = Length.Percent(50);
            _window.style.top = Length.Percent(50);
            _window.style.translate = new Translate(Length.Percent(-50), Length.Percent(-50));
            _window.pickingMode = PickingMode.Position;

            if (_machineWindowStyle != null)
            {
                _window.styleSheets.Add(_machineWindowStyle);
            }

            return true;
        }

        private void ClosePanelOnly()
        {
            if (_apcBinder != null)
            {
                _apcBinder.CloseRequested -= HandleCloseRequested;
                _apcBinder.ChannelToggled -= HandleChannelToggled;
                _apcBinder = null;
            }

            if (_smesBinder != null)
            {
                _smesBinder.CloseRequested -= HandleCloseRequested;
                _smesBinder.InputToggled -= HandleSmesInputToggled;
                _smesBinder.OutputToggled -= HandleSmesOutputToggled;
                _smesBinder.InputRateDeltaRequested -= HandleSmesInputRateDelta;
                _smesBinder.OutputRateDeltaRequested -= HandleSmesOutputRateDelta;
                _smesBinder = null;
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

        private void HandleSmesInputToggled(bool isOn)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifySmesControlToggled(0, isOn);
            }
        }

        private void HandleSmesOutputToggled(bool isOn)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifySmesControlToggled(1, isOn);
            }
        }

        private void HandleSmesInputRateDelta(float delta)
        {
            HandleSmesRateDelta(0, delta);
        }

        private void HandleSmesOutputRateDelta(float delta)
        {
            HandleSmesRateDelta(1, delta);
        }

        private void HandleSmesRateDelta(byte controlId, float delta)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifySmesRateDelta(controlId, delta);
            }
        }
    }
}
