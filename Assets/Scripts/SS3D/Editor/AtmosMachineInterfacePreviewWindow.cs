#if UNITY_EDITOR
using SS3D.UI.MachineInterface;
using SS3D.UI.MachineInterface.Bindings;
using SS3D.UI.MachineInterface.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.Editor
{
    public class AtmosMachineInterfacePreviewWindow : EditorWindow
    {
        private enum PreviewPanel
        {
            AirAlarm,
            Scrubber,
            Vent,
        }

        private PreviewPanel _panel = PreviewPanel.AirAlarm;
        private TemplateContainer _panelRoot;
        private VisualElement _root;
        private bool _stylesApplied;

        private AirAlarmInterfaceBinder _airAlarmBinder;
        private ScrubberInterfaceBinder _scrubberBinder;
        private VentInterfaceBinder _ventBinder;

        private AirAlarmInterfaceViewModel _airAlarmModel = AirAlarmInterfaceViewModel.CreateNormal();
        private ScrubberInterfaceViewModel _scrubberModel = ScrubberInterfaceViewModel.CreateFiltering();
        private VentInterfaceViewModel _ventModel = VentInterfaceViewModel.CreateIdle();

        [MenuItem("SS3D/Machine Interface/Preview Air Alarm Panel")]
        public static void OpenAirAlarm()
        {
            Open(PreviewPanel.AirAlarm);
        }

        [MenuItem("SS3D/Machine Interface/Preview Scrubber Panel")]
        public static void OpenScrubber()
        {
            Open(PreviewPanel.Scrubber);
        }

        [MenuItem("SS3D/Machine Interface/Preview Vent Panel")]
        public static void OpenVent()
        {
            Open(PreviewPanel.Vent);
        }

        private static void Open(PreviewPanel panel)
        {
            AtmosMachineInterfacePreviewWindow window = GetWindow<AtmosMachineInterfacePreviewWindow>();
            window._panel = panel;
            window.titleContent = new GUIContent(GetTitle(panel));
            window.minSize = new Vector2(900, 760);
            window.RebuildPanel();
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
            VisualElement existing = _root.Q<VisualElement>("preview-toolbar");
            existing?.RemoveFromHierarchy();

            VisualElement toolbar = new();
            toolbar.name = "preview-toolbar";
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.paddingLeft = 8;
            toolbar.style.paddingRight = 8;
            toolbar.style.paddingTop = 8;
            toolbar.style.paddingBottom = 8;

            toolbar.Add(CreatePanelButton("Air Alarm", PreviewPanel.AirAlarm));
            toolbar.Add(CreatePanelButton("Scrubber", PreviewPanel.Scrubber));
            toolbar.Add(CreatePanelButton("Vent", PreviewPanel.Vent));
            toolbar.Add(CreateActionButton("Toggle ID Access", ToggleAccess));
            toolbar.Add(CreateActionButton("Select Device", CycleSelectedDevice));

            foreach (Button scenarioButton in CreateScenarioButtons())
            {
                toolbar.Add(scenarioButton);
            }

            _root.Insert(0, toolbar);
        }

        private Button CreatePanelButton(string label, PreviewPanel panel)
        {
            Button button = new(() =>
            {
                _panel = panel;
                titleContent = new GUIContent(GetTitle(panel));
                RebuildPanel();
            })
            { text = label };
            button.style.marginRight = 6;
            button.style.marginBottom = 6;
            return button;
        }

        private Button CreateActionButton(string label, System.Action action)
        {
            Button button = new(action) { text = label };
            button.style.marginRight = 6;
            button.style.marginBottom = 6;
            return button;
        }

        private Button[] CreateScenarioButtons()
        {
            return _panel switch
            {
                PreviewPanel.AirAlarm => new[]
                {
                    CreateScenarioButton("Normal", () => SetAirAlarmScenario(AirAlarmInterfaceViewModel.CreateNormal())),
                    CreateScenarioButton("Warning", () => SetAirAlarmScenario(AirAlarmInterfaceViewModel.CreateWarning())),
                    CreateScenarioButton("Danger", () => SetAirAlarmScenario(AirAlarmInterfaceViewModel.CreateDanger())),
                },
                PreviewPanel.Scrubber => new[]
                {
                    CreateScenarioButton("Filtering", () => SetScrubberScenario(ScrubberInterfaceViewModel.CreateFiltering())),
                    CreateScenarioButton("Idle", () => SetScrubberScenario(ScrubberInterfaceViewModel.CreateIdle())),
                    CreateScenarioButton("Overload", () => SetScrubberScenario(ScrubberInterfaceViewModel.CreateOverloaded())),
                    CreateScenarioButton("Fault", () => SetScrubberScenario(ScrubberInterfaceViewModel.CreateFault())),
                },
                _ => new[]
                {
                    CreateScenarioButton("Idle", () => SetVentScenario(VentInterfaceViewModel.CreateIdle())),
                    CreateScenarioButton("Pressurize", () => SetVentScenario(VentInterfaceViewModel.CreatePressurizing())),
                    CreateScenarioButton("Depressurize", () => SetVentScenario(VentInterfaceViewModel.CreateDepressurizing())),
                    CreateScenarioButton("Fault", () => SetVentScenario(VentInterfaceViewModel.CreateFault())),
                },
            };
        }

        private Button CreateScenarioButton(string label, System.Action action)
        {
            Button button = new(action) { text = label };
            button.style.marginRight = 6;
            button.style.marginBottom = 6;
            return button;
        }

        private void ToggleAccess()
        {
            switch (_panel)
            {
                case PreviewPanel.AirAlarm:
                    _airAlarmModel.AccessGranted = !_airAlarmModel.AccessGranted;
                    _airAlarmBinder?.Bind(_airAlarmModel);
                    break;
                case PreviewPanel.Scrubber:
                    _scrubberModel.AccessGranted = !_scrubberModel.AccessGranted;
                    _scrubberBinder?.Bind(_scrubberModel);
                    break;
                default:
                    _ventModel.AccessGranted = !_ventModel.AccessGranted;
                    _ventBinder?.Bind(_ventModel);
                    break;
            }
        }

        private void CycleSelectedDevice()
        {
            if (_panel != PreviewPanel.AirAlarm || _airAlarmModel.ConnectedDevices.Count == 0)
            {
                return;
            }

            int currentIndex = _airAlarmModel.ConnectedDevices.FindIndex(d => d.Id == _airAlarmModel.SelectedDeviceId);
            int nextIndex = (currentIndex + 1) % (_airAlarmModel.ConnectedDevices.Count + 1);
            _airAlarmModel.SelectedDeviceId = nextIndex >= _airAlarmModel.ConnectedDevices.Count
                ? null
                : _airAlarmModel.ConnectedDevices[nextIndex].Id;
            _airAlarmBinder?.Bind(_airAlarmModel);
        }

        private void WireAirAlarmPreviewBinder(AirAlarmInterfaceBinder binder)
        {
            binder.BoolControlChanged += (controlId, isOn) =>
            {
                AirAlarmInterfaceInteractionLogic.ApplyBool(_airAlarmModel, controlId, isOn);
                binder.Bind(_airAlarmModel);
            };

            binder.NumericControlChanged += (controlId, delta) =>
            {
                AirAlarmInterfaceInteractionLogic.ApplyNumeric(_airAlarmModel, controlId, delta);
                binder.Bind(_airAlarmModel);
            };

            binder.ActionControlChanged += (controlId, value) =>
            {
                if (controlId == MachineInterfaceControlIds.Atmos.ReadId)
                {
                    if (_airAlarmModel.AccessGranted)
                    {
                        _airAlarmModel.AccessGranted = false;
                        _airAlarmModel.AccessScanning = false;
                        _airAlarmModel.SelectedDeviceId = null;
                    }
                    else if (!_airAlarmModel.AccessScanning)
                    {
                        _airAlarmModel.AccessScanning = true;
                        _airAlarmModel.AccessGranted = true;
                        _airAlarmModel.AccessScanning = false;
                    }
                }
                else
                {
                    AirAlarmInterfaceInteractionLogic.ApplyAction(_airAlarmModel, controlId, value);
                }

                binder.Bind(_airAlarmModel);
            };
        }

        private void SetAirAlarmScenario(AirAlarmInterfaceViewModel model)
        {
            model.AccessGranted = _airAlarmModel.AccessGranted;
            model.SelectedDeviceId = _airAlarmModel.SelectedDeviceId;
            _airAlarmModel = model;
            _airAlarmBinder?.Bind(_airAlarmModel);
        }

        private void SetScrubberScenario(ScrubberInterfaceViewModel model)
        {
            model.AccessGranted = _scrubberModel.AccessGranted;
            _scrubberModel = model;
            _scrubberBinder?.Bind(_scrubberModel);
        }

        private void SetVentScenario(VentInterfaceViewModel model)
        {
            model.AccessGranted = _ventModel.AccessGranted;
            _ventModel = model;
            _ventBinder?.Bind(_ventModel);
        }

        private void RebuildPanel()
        {
            _panelRoot?.RemoveFromHierarchy();

            string templatePath = _panel switch
            {
                PreviewPanel.Scrubber => "Assets/Content/Systems/UI/MachineInterface/Templates/ScrubberUnitInterface.uxml",
                PreviewPanel.Vent => "Assets/Content/Systems/UI/MachineInterface/Templates/VentUnitInterface.uxml",
                _ => "Assets/Content/Systems/UI/MachineInterface/Templates/AirAlarmInterface.uxml",
            };

            VisualTreeAsset template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath);
            _panelRoot = template != null ? template.CloneTree() : new TemplateContainer();

            DiegeticDeviceShell shell = _panelRoot.Q<DiegeticDeviceShell>("device-shell");
            ApplyPanelStyles(_panelRoot, shell, templatePath);
            _panelRoot.style.alignSelf = Align.Center;
            _panelRoot.style.flexShrink = 1;
            _panelRoot.style.maxHeight = Length.Percent(100);
            _panelRoot.style.marginTop = 16;
            _root.Add(_panelRoot);

            switch (_panel)
            {
                case PreviewPanel.Scrubber:
                    _scrubberBinder = new ScrubberInterfaceBinder(_panelRoot);
                    _scrubberBinder.Bind(_scrubberModel);
                    break;
                case PreviewPanel.Vent:
                    _ventBinder = new VentInterfaceBinder(_panelRoot);
                    _ventBinder.Bind(_ventModel);
                    break;
                default:
                    _airAlarmBinder = new AirAlarmInterfaceBinder(_panelRoot);
                    WireAirAlarmPreviewBinder(_airAlarmBinder);
                    _airAlarmBinder.Bind(_airAlarmModel);
                    break;
            }

            BuildToolbar();
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

        private static void ApplyPanelStyles(TemplateContainer panel, DiegeticDeviceShell shell, string templatePath)
        {
            string templateUss = templatePath.Replace(".uxml", ".uss");
            string[] sharedPaths =
            {
                templateUss,
                "Assets/Content/Systems/UI/MachineInterface/Components/DiegeticDeviceShell.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StatusDot.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ConnectionStatusRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceIdentityBlock.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/GlanceableStatusChip.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/PanelSection.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ReadoutMetricTile.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/GasBarRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AtmosIdReaderRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/PresetModeButton.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ConnectedDeviceRow.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/AirAlarmDeviceDetailPanel.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/PressureFlowReadout.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/ToggleSwitch.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/CompactFilterToggle.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/NumericStepper.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/SteelButton.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/StatusBadge.uss",
                "Assets/Content/Systems/UI/MachineInterface/Components/DeviceFooter.uss",
            };

            AddStyleSheets(panel, sharedPaths);

            if (shell == null)
            {
                return;
            }

            AddStyleSheets(shell, sharedPaths);
            if (shell.ScreenContent != null)
            {
                AddStyleSheets(shell.ScreenContent, sharedPaths);
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

        private static string GetTitle(PreviewPanel panel)
        {
            return panel switch
            {
                PreviewPanel.Scrubber => "Scrubber Unit",
                PreviewPanel.Vent => "Vent Unit",
                _ => "Air Alarm",
            };
        }
    }
}
#endif
