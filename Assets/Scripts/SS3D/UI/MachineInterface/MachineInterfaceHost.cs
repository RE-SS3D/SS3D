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
    /// <para>
    /// Adding a new machine interface:
    /// </para>
    /// <list type="number">
    /// <item><description>Add a constant to <see cref="MachineInterfaceIds"/>.</description></item>
    /// <item><description>Define snapshot, FishNet serializer, view model, and mapper types.</description></item>
    /// <item><description>Create UXML/USS under Content/Systems/UI/MachineInterface and a binder implementing <see cref="IMachineInterfaceBinder"/>.</description></item>
    /// <item><description>Add a networked controller on the machine prefab (inherit <see cref="MachineInterfaceBehaviour"/>; use concrete TargetRpc snapshot types—FishNet does not support generic RPC parameters).</description></item>
    /// <item><description>Assign serialized templates on this host and register a <see cref="MachineInterfaceUiRegistration"/> in <see cref="RegisterUiEntries"/> (set <c>Wide</c> for layouts wider than the default window).</description></item>
    /// <item><description>Register the snapshot type in <see cref="MachineInterfaceNetworkRegistry"/>.</description></item>
    /// <item><description>Optional: add control IDs to <see cref="MachineInterfaceControlIds"/> and a dev scenario in <see cref="MachineInterfaceDevHarness"/>.</description></item>
    /// </list>
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class MachineInterfaceHost : View
    {
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

        [SerializeField]
        private VisualTreeAsset _vendingTemplate;

        [SerializeField]
        private StyleSheet _vendingTemplateStyle;

        [SerializeField]
        private VisualTreeAsset _idConsoleTemplate;

        [SerializeField]
        private StyleSheet _idConsoleTemplateStyle;

        private VisualElement _overlayRoot;
        private VisualElement _panelRoot;
        private MachineWindow _window;
        private DiegeticDeviceShell _diegeticShell;
        private IMachineInterfaceBinder _binder;
        private string _openInterfaceId;
        private bool _overlayReady;

        public bool IsOpen => _panelRoot != null;

        public bool Open(string interfaceId, IMachineInterfaceViewModel viewModel)
        {
            if (!EnsureDocumentActive())
            {
                return false;
            }

            if (!MachineInterfaceRegistry.TryGetUi(interfaceId, out MachineInterfaceUiRegistration registration))
            {
                Debug.LogWarning($"MachineInterfaceHost has no UI registration for: {interfaceId}", this);
                return false;
            }

            if (registration.Template == null)
            {
                Debug.LogError($"MachineInterfaceHost is missing the template for {interfaceId}.", this);
                return false;
            }

            ClosePanelOnly();
            if (!CreatePanel(viewModel.Title, registration))
            {
                return false;
            }

            _openInterfaceId = interfaceId;
            TemplateContainer template = registration.Template.CloneTree();
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, registration.TemplateStyle);

            if (registration.ShellKind == MachineInterfaceShellKind.DiegeticDevice)
            {
                _diegeticShell = template.Q<DiegeticDeviceShell>();

                // Keep the cloned TemplateContainer in the hierarchy so UXML style sheets stay attached.
                _panelRoot = template;
                VisualElement layoutRoot = _diegeticShell != null ? _diegeticShell : template;
                layoutRoot.style.alignSelf = Align.Center;
                layoutRoot.style.marginTop = 24;
                _overlayRoot.Add(template);
            }
            else
            {
                _window.Content.Add(template);
                _overlayRoot.Add(_window);
                _panelRoot = _window;
            }

            SetOverlayInteractive(true);

            _binder = registration.CreateBinder(_panelRoot);
            WireBinder(_binder);
            _binder.Bind(viewModel);
            WireCloseHandler();
            return true;
        }

        public void Refresh(IMachineInterfaceViewModel viewModel)
        {
            _binder?.Bind(viewModel);
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
            RegisterUiEntries();
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

            if (_vendingTemplate == null)
            {
                _vendingTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/VendingMachineInterface.uxml");
            }

            if (_vendingTemplateStyle == null)
            {
                _vendingTemplateStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/VendingMachineInterface.uss");
            }

            if (_document != null && _document.panelSettings == null)
            {
                _document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    "Assets/Content/Systems/UI/MachineInterface/MachineInterfacePanelSettings.asset");
            }
        }
#endif

        private void RegisterUiEntries()
        {
            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Apc,
                Template = _apcTemplate,
                TemplateStyle = _apcTemplateStyle,
                ShellKind = MachineInterfaceShellKind.ModalWindow,
                Wide = false,
                CreateBinder = root => new ApcPowerControllerBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Smes,
                Template = _smesTemplate,
                TemplateStyle = _smesTemplateStyle,
                ShellKind = MachineInterfaceShellKind.ModalWindow,
                Wide = true,
                CreateBinder = root => new SmesUnitBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.Vending,
                Template = _vendingTemplate,
                TemplateStyle = _vendingTemplateStyle,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new VendingMachineBinder(root),
            });

            MachineInterfaceRegistry.RegisterUi(new MachineInterfaceUiRegistration
            {
                InterfaceId = MachineInterfaceIds.IdConsole,
                Template = _idConsoleTemplate,
                TemplateStyle = _idConsoleTemplateStyle,
                ShellKind = MachineInterfaceShellKind.DiegeticDevice,
                CreateBinder = root => new IdConsoleBinder(root),
            });
        }

        private bool CreatePanel(string title, MachineInterfaceUiRegistration registration)
        {
            if (registration.ShellKind == MachineInterfaceShellKind.DiegeticDevice)
            {
                _window = null;
                _diegeticShell = null;
                return true;
            }

            return CreateWindow(title, registration.Wide);
        }

        private void WireCloseHandler()
        {
            if (_window != null)
            {
                _window.CloseClicked += HandleCloseRequested;
            }

            if (_diegeticShell != null)
            {
                _diegeticShell.CloseClicked += HandleCloseRequested;
            }
        }

        private void UnwireCloseHandler()
        {
            if (_window != null)
            {
                _window.CloseClicked -= HandleCloseRequested;
            }

            if (_diegeticShell != null)
            {
                _diegeticShell.CloseClicked -= HandleCloseRequested;
            }
        }

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
            if (_binder != null)
            {
                _binder.CloseRequested -= HandleCloseRequested;
                _binder.BoolControlChanged -= HandleBoolControlChanged;
                _binder.NumericControlChanged -= HandleNumericControlChanged;
                _binder.ActionControlChanged -= HandleActionControlChanged;
                _binder.Disconnect();
                _binder = null;
            }

            UnwireCloseHandler();

            if (_panelRoot != null)
            {
                _panelRoot.RemoveFromHierarchy();
            }

            _panelRoot = null;
            _window = null;
            _diegeticShell = null;
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

        private void WireBinder(IMachineInterfaceBinder binder)
        {
            binder.CloseRequested += HandleCloseRequested;
            binder.BoolControlChanged += HandleBoolControlChanged;
            binder.NumericControlChanged += HandleNumericControlChanged;
            binder.ActionControlChanged += HandleActionControlChanged;
        }

        private void HandleCloseRequested()
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.RequestCloseFromUi(_openInterfaceId);
            }
        }

        private void HandleBoolControlChanged(byte controlId, bool isOn)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifyBoolControl(controlId, isOn);
            }
        }

        private void HandleNumericControlChanged(byte controlId, float delta)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifyNumericControl(controlId, delta);
            }
        }

        private void HandleActionControlChanged(byte controlId, int value)
        {
            if (SubSystems.TryGet(out MachineInterfaceSubSystem subsystem))
            {
                subsystem.NotifyActionControl(controlId, value);
            }
        }
    }
}
