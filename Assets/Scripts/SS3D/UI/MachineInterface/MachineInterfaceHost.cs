using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.UI.MachineInterface.Components;
using System.Collections.Generic;
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
    /// <item><description>Assign serialized templates on this host; <see cref="MachineUiCatalog"/> registers UI entries (set <c>Wide</c> for layouts wider than the default window).</description></item>
    /// <item><description>Register the snapshot type in <see cref="MachineInterfaceNetworkRegistry"/>.</description></item>
    /// <item><description>Register an <see cref="IMachineOptimisticControlHandler"/> for client optimistic controls when adding interactive controls.</description></item>
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

        [SerializeField]
        private VisualTreeAsset _gasPumpTemplate;

        [SerializeField]
        private StyleSheet _gasPumpTemplateStyle;

        [SerializeField]
        private VisualTreeAsset _airAlarmTemplate;

        [SerializeField]
        private StyleSheet _airAlarmTemplateStyle;

        [SerializeField]
        private VisualTreeAsset _scrubberTemplate;

        [SerializeField]
        private StyleSheet _scrubberTemplateStyle;

        [SerializeField]
        private VisualTreeAsset _ventTemplate;

        [SerializeField]
        private StyleSheet _ventTemplateStyle;

        [SerializeField]
        private StyleSheet _ss3dTokensStyle;

        [SerializeField]
        private StyleSheet _diegeticTokensStyle;

        [SerializeField]
        private StyleSheet _diegeticTonesStyle;

        [SerializeField]
        private StyleSheet _ss3dTypographyStyle;

        [SerializeField]
        private StyleSheet[] _apcComponentStyles;

        [SerializeField]
        private StyleSheet[] _smesComponentStyles;

        [SerializeField]
        private StyleSheet[] _vendingComponentStyles;

        [SerializeField]
        private StyleSheet[] _gasPumpComponentStyles;

        [SerializeField]
        private StyleSheet[] _airAlarmComponentStyles;

        [SerializeField]
        private StyleSheet[] _scrubberComponentStyles;

        [SerializeField]
        private StyleSheet[] _ventComponentStyles;

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
            bool isDiegetic = registration.ShellKind == MachineInterfaceShellKind.DiegeticDevice;
            template.style.backgroundColor = Color.clear;

            if (isDiegetic)
            {
                _diegeticShell = template.Q<DiegeticDeviceShell>();

                // Keep the cloned TemplateContainer in the hierarchy so UXML style sheets stay attached.
                _panelRoot = template;
                template.style.flexShrink = 1;
                template.style.maxHeight = Length.Percent(100);
                VisualElement layoutRoot = _diegeticShell != null ? _diegeticShell : template;
                layoutRoot.style.alignSelf = Align.Center;
                layoutRoot.style.flexShrink = 1;
                layoutRoot.style.maxHeight = Length.Percent(100);
                layoutRoot.style.marginTop = 24;
                _overlayRoot.Add(template);

                ApplyDiegeticPanelStyles(template, _diegeticShell, registration);
            }
            else
            {
                ApplyModalPanelStyles(_window, template, registration);

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
            EnsureRuntimeAssets();
            RegisterUiEntries();
            ShutdownDocument();
        }

        protected override void OnDestroyed()
        {
            Close();
            base.OnDestroyed();
        }

#if UNITY_EDITOR
        private static StyleSheet[] EnsureComponentStyles(StyleSheet[] current, params string[] paths)
        {
            if (current != null && current.Length > 0)
            {
                return current;
            }

            StyleSheet[] loaded = new StyleSheet[paths.Length];
            int count = 0;
            for (int i = 0; i < paths.Length; i++)
            {
                StyleSheet styleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(paths[i]);
                if (styleSheet != null)
                {
                    loaded[count++] = styleSheet;
                }
            }

            if (count == 0)
            {
                return current ?? System.Array.Empty<StyleSheet>();
            }

            if (count == loaded.Length)
            {
                return loaded;
            }

            StyleSheet[] trimmed = new StyleSheet[count];
            System.Array.Copy(loaded, trimmed, count);
            return trimmed;
        }

        private static StyleSheet[] EnsureAtmosComponentStyles(
            StyleSheet[] current,
            bool includeAirAlarm,
            bool includeVent = false)
        {
            List<string> paths = new()
            {
                "Assets/Content/Systems/UI/MachineInterface/Components/DiegeticDeviceShell.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StatusDot.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ConnectionStatusRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceIdentityBlock.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/GlanceableStatusChip.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/PanelSection.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ReadoutMetricTile.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/GasBarRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AtmosIdReaderRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ToggleSwitch.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/CompactFilterToggle.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/NumericStepper.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/SteelButton.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StatusBadge.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss",
            };

            if (includeAirAlarm)
            {
                paths.Add("Assets/Content/Systems/UI/MachineInterface/Components/PresetModeButton.uss");
                paths.Add("Assets/Content/Systems/UI/MachineInterface/Components/ConnectedDeviceRow.uss");
                paths.Add("Assets/Content/Systems/UI/MachineInterface/Components/AirAlarmDeviceDetailPanel.uss");
            }

            if (includeVent)
            {
                paths.Add("Assets/Content/Systems/UI/MachineInterface/Components/PressureFlowReadout.uss");
            }

            return EnsureComponentStyles(current, paths.ToArray());
        }

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

            if (_idConsoleTemplate == null)
            {
                _idConsoleTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/IdConsoleInterface.uxml");
            }

            if (_idConsoleTemplateStyle == null)
            {
                _idConsoleTemplateStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/IdConsoleInterface.uss");
            }

            if (_gasPumpTemplate == null)
            {
                _gasPumpTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/PumpUnitInterface.uxml");
            }

            if (_gasPumpTemplateStyle == null)
            {
                _gasPumpTemplateStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/PumpUnitInterface.uss");
            }

            if (_airAlarmTemplate == null)
            {
                _airAlarmTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/AirAlarmInterface.uxml");
            }

            if (_airAlarmTemplateStyle == null)
            {
                _airAlarmTemplateStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/AirAlarmInterface.uss");
            }

            if (_scrubberTemplate == null)
            {
                _scrubberTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/ScrubberUnitInterface.uxml");
            }

            if (_scrubberTemplateStyle == null)
            {
                _scrubberTemplateStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/ScrubberUnitInterface.uss");
            }

            if (_ventTemplate == null)
            {
                _ventTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/VentUnitInterface.uxml");
            }

            if (_ventTemplateStyle == null)
            {
                _ventTemplateStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Templates/VentUnitInterface.uss");
            }

            if (_ss3dTokensStyle == null)
            {
                _ss3dTokensStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/Tokens/ss3d-tokens.uss");
            }

            if (_diegeticTokensStyle == null)
            {
                _diegeticTokensStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tokens.uss");
            }

            if (_diegeticTonesStyle == null)
            {
                _diegeticTonesStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/MachineInterface/Tokens/diegetic-tones.uss");
            }

            if (_ss3dTypographyStyle == null)
            {
                _ss3dTypographyStyle = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(
                    "Assets/Content/Systems/UI/Tokens/ss3d-typography.uss");
            }

            _apcComponentStyles = EnsureComponentStyles(
                _apcComponentStyles,
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
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss");

            _smesComponentStyles = EnsureComponentStyles(
                _smesComponentStyles,
                "Assets/Content/Systems/UI/MachineInterface/Components/DiegeticDeviceShell.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StatusDot.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ConnectionStatusRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceIdentityBlock.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/GlanceableStatusChip.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/PanelSection.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StorageCellRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/SmesPowerFlowRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/RateControlSection.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ToggleSwitch.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AccessGatePanel.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AccessGatedRegion.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AccessStrip.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/SteelButton.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/Badge.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StatusBadge.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss");

            _vendingComponentStyles = EnsureComponentStyles(
                _vendingComponentStyles,
                "Assets/Content/Systems/UI/MachineInterface/Components/DiegeticDeviceShell.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StatusDot.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ConnectionStatusRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceIdentityBlock.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/PanelSection.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AtmosIdReaderRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/SteelButton.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/InventorySlot.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ProductCard.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ProductGrid.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DispenseTray.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ActionLog.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss");

            _gasPumpComponentStyles = EnsureAtmosComponentStyles(_gasPumpComponentStyles, includeAirAlarm: false, includeVent: true);

            _airAlarmComponentStyles = EnsureAtmosComponentStyles(_airAlarmComponentStyles, includeAirAlarm: true);
            _scrubberComponentStyles = EnsureAtmosComponentStyles(_scrubberComponentStyles, includeAirAlarm: false);
            _ventComponentStyles = EnsureAtmosComponentStyles(_ventComponentStyles, includeAirAlarm: false, includeVent: true);

            if (_document != null && _document.panelSettings == null)
            {
                _document.panelSettings = UnityEditor.AssetDatabase.LoadAssetAtPath<PanelSettings>(
                    "Assets/Content/Systems/UI/MachineInterface/MachineInterfacePanelSettings.asset");
            }
        }
#endif

        private void EnsureRuntimeAssets()
        {
#if !UNITY_EDITOR
            if (_vendingTemplate == null || _vendingTemplateStyle == null || _gasPumpTemplate == null
                || _gasPumpTemplateStyle == null || _ss3dTokensStyle == null || _diegeticTokensStyle == null
                || _diegeticTonesStyle == null || _ss3dTypographyStyle == null)
            {
                Debug.LogWarning(
                    "MachineInterfaceHost is missing diegetic UI assets. Assign templates and token style sheets on the Game scene host.",
                    this);
            }
#endif
        }

        private void ApplySharedTokenStyles(VisualElement target)
        {
            MachineInterfaceHostHelpers.ApplyTemplateStyle(target, _ss3dTokensStyle);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(target, _ss3dTypographyStyle);
        }

        private void ApplyDiegeticPanelStyles(
            TemplateContainer template,
            DiegeticDeviceShell shell,
            MachineInterfaceUiRegistration registration)
        {
            ApplyDiegeticBaseStyles(template);
            MachineInterfaceHostHelpers.ApplyStyleSheets(template, registration.ComponentStyles);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, registration.TemplateStyle);

            if (shell == null)
            {
                return;
            }

            ApplyDiegeticBaseStyles(shell);
            MachineInterfaceHostHelpers.ApplyStyleSheets(shell, registration.ComponentStyles);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(shell, registration.TemplateStyle);

            if (shell.ScreenContent != null)
            {
                ApplyDiegeticBaseStyles(shell.ScreenContent);
                MachineInterfaceHostHelpers.ApplyStyleSheets(shell.ScreenContent, registration.ComponentStyles);
                MachineInterfaceHostHelpers.ApplyTemplateStyle(shell.ScreenContent, registration.TemplateStyle);
            }
        }

        private void ApplyModalPanelStyles(
            MachineWindow window,
            TemplateContainer template,
            MachineInterfaceUiRegistration registration)
        {
            ApplySharedTokenStyles(template);
            MachineInterfaceHostHelpers.ApplyStyleSheets(template, registration.ComponentStyles);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, registration.TemplateStyle);

            if (window == null)
            {
                return;
            }

            ApplySharedTokenStyles(window);
            MachineInterfaceHostHelpers.ApplyStyleSheets(window, registration.ComponentStyles);
        }

        private void ApplyDiegeticBaseStyles(VisualElement template)
        {
            ApplySharedTokenStyles(template);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, _diegeticTokensStyle);
            MachineInterfaceHostHelpers.ApplyTemplateStyle(template, _diegeticTonesStyle);
        }

        private void RegisterUiEntries()
        {
            MachineUiCatalog.RegisterAll(new MachineUiCatalogAssets
            {
                ApcTemplate = _apcTemplate,
                ApcTemplateStyle = _apcTemplateStyle,
                ApcComponentStyles = _apcComponentStyles,
                SmesTemplate = _smesTemplate,
                SmesTemplateStyle = _smesTemplateStyle,
                SmesComponentStyles = _smesComponentStyles,
                VendingTemplate = _vendingTemplate,
                VendingTemplateStyle = _vendingTemplateStyle,
                VendingComponentStyles = _vendingComponentStyles,
                IdConsoleTemplate = _idConsoleTemplate,
                IdConsoleTemplateStyle = _idConsoleTemplateStyle,
                GasPumpTemplate = _gasPumpTemplate,
                GasPumpTemplateStyle = _gasPumpTemplateStyle,
                GasPumpComponentStyles = _gasPumpComponentStyles,
                AirAlarmTemplate = _airAlarmTemplate,
                AirAlarmTemplateStyle = _airAlarmTemplateStyle,
                AirAlarmComponentStyles = _airAlarmComponentStyles,
                ScrubberTemplate = _scrubberTemplate,
                ScrubberTemplateStyle = _scrubberTemplateStyle,
                ScrubberComponentStyles = _scrubberComponentStyles,
                VentTemplate = _ventTemplate,
                VentTemplateStyle = _ventTemplateStyle,
                VentComponentStyles = _ventComponentStyles,
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

            ApplySharedTokenStyles(_window);

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
            _overlayRoot.style.flexShrink = 1;
            _overlayRoot.style.minHeight = 0;
            _overlayRoot.style.backgroundColor = Color.clear;
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
                _overlayRoot.style.flexDirection = FlexDirection.Column;
                _overlayRoot.style.justifyContent = Justify.Center;
                _overlayRoot.style.flexGrow = 1;
                _overlayRoot.style.flexShrink = 1;
                _overlayRoot.style.minHeight = 0;
                _overlayRoot.pickingMode = PickingMode.Position;
            }
            else
            {
                _overlayRoot.style.display = DisplayStyle.None;
                _overlayRoot.style.flexGrow = 0;
                _overlayRoot.style.flexShrink = 1;
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
