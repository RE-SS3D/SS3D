using SS3D.UI.MachineInterface.Components;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class AirAlarmInterfaceBinder : IMachineInterfaceBinder
    {
        private static readonly string[] FilterKeys = { "O2", "N2", "CO2", "Plasma", "Toxins" };

        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly ReadoutMetricTile _pressureTile;
        private readonly ReadoutMetricTile _temperatureTile;
        private readonly VisualElement _gasList;
        private readonly AtmosIdReaderRow _idReader;
        private readonly Label _modeLockedHint;
        private readonly PresetModeButton _filteringMode;
        private readonly PresetModeButton _panicMode;
        private readonly PresetModeButton _fillMode;
        private readonly PresetModeButton _offMode;
        private readonly VisualElement _deviceList;
        private readonly AirAlarmDeviceDetailPanel _deviceDetail;
        private readonly DeviceFooter _footer;

        public AirAlarmInterfaceBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _pressureTile = queryRoot.Q<ReadoutMetricTile>("pressure-tile");
            _temperatureTile = queryRoot.Q<ReadoutMetricTile>("temperature-tile");
            _gasList = queryRoot.Q<VisualElement>("gas-list");
            _idReader = queryRoot.Q<AtmosIdReaderRow>("id-reader");
            _modeLockedHint = queryRoot.Q<Label>("mode-locked-hint");
            _filteringMode = queryRoot.Q<PresetModeButton>("mode-filtering");
            _panicMode = queryRoot.Q<PresetModeButton>("mode-panic");
            _fillMode = queryRoot.Q<PresetModeButton>("mode-fill");
            _offMode = queryRoot.Q<PresetModeButton>("mode-off");
            _deviceList = queryRoot.Q<VisualElement>("device-list");
            _deviceDetail = queryRoot.Q<AirAlarmDeviceDetailPanel>("device-detail");
            _footer = queryRoot.Q<DeviceFooter>("footer");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }

            if (_idReader != null)
            {
                _idReader.ReadRequested += HandleReadRequested;
            }

            if (_filteringMode != null)
            {
                _filteringMode.Clicked += OnFilteringModeClicked;
            }

            if (_panicMode != null)
            {
                _panicMode.Clicked += OnPanicModeClicked;
            }

            if (_fillMode != null)
            {
                _fillMode.Clicked += OnFillModeClicked;
            }

            if (_offMode != null)
            {
                _offMode.Clicked += OnOffModeClicked;
            }

            if (_deviceDetail != null)
            {
                _deviceDetail.CloseRequested += HandleDeviceDetailCloseRequested;
                _deviceDetail.PowerChanged += value =>
                    BoolControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.Power, value);
                _deviceDetail.TargetDeltaRequested += delta =>
                    NumericControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.TargetPressure, delta);
                _deviceDetail.FilterChanged += HandleDeviceFilterChanged;
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not AirAlarmInterfaceViewModel model)
            {
                return;
            }

            if (_shell != null)
            {
                _shell.ModelLabel = model.ModelLabel;
                _shell.PowerOk = model.ChassisPowerOk;
            }

            if (_connectionRow != null)
            {
                _connectionRow.StatusText = model.ConnectionStatus;
                _connectionRow.ReadoutText = model.DeviceCountText;
                _connectionRow.DotTone = GetConnectionTone(model.Scenario);
            }

            if (_identity != null)
            {
                _identity.Title = model.DeviceTitle;
                _identity.Subtitle = model.Subtitle;
            }

            StatusTone statusTone = GetScenarioTone(model.Scenario);
            _statusChip?.SetContent(model.StatusHeadline, model.StatusBadgeText, statusTone, model.AlarmSubline);

            if (_pressureTile != null)
            {
                _pressureTile.Label = "Pressure";
                _pressureTile.Value = model.PressureText;
                _pressureTile.Tone = model.PressureTone;
            }

            if (_temperatureTile != null)
            {
                _temperatureTile.Label = "Temperature";
                _temperatureTile.Value = model.TemperatureText;
                _temperatureTile.Tone = model.TemperatureTone;
            }

            BindGasRows(model.GasReadouts);
            BindAccess(model);
            BindModes(model);
            BindDevices(model);

            if (_footer != null)
            {
                _footer.Text = model.FooterText;
            }
        }

        public void Disconnect()
        {
            if (_shell != null)
            {
                _shell.CloseClicked -= HandleCloseRequested;
            }

            if (_idReader != null)
            {
                _idReader.ReadRequested -= HandleReadRequested;
            }

            if (_filteringMode != null)
            {
                _filteringMode.Clicked -= OnFilteringModeClicked;
            }

            if (_panicMode != null)
            {
                _panicMode.Clicked -= OnPanicModeClicked;
            }

            if (_fillMode != null)
            {
                _fillMode.Clicked -= OnFillModeClicked;
            }

            if (_offMode != null)
            {
                _offMode.Clicked -= OnOffModeClicked;
            }

            if (_deviceDetail != null)
            {
                _deviceDetail.CloseRequested -= HandleDeviceDetailCloseRequested;
                _deviceDetail.FilterChanged -= HandleDeviceFilterChanged;
            }
        }

        private void HandleCloseRequested() => CloseRequested?.Invoke();

        private void HandleReadRequested() =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.ReadId, 0);

        private void OnFilteringModeClicked() => HandlePresetModeSelected(AirAlarmPresetMode.Filtering);

        private void OnPanicModeClicked() => HandlePresetModeSelected(AirAlarmPresetMode.Panic);

        private void OnFillModeClicked() => HandlePresetModeSelected(AirAlarmPresetMode.Fill);

        private void OnOffModeClicked() => HandlePresetModeSelected(AirAlarmPresetMode.Off);

        private void HandlePresetModeSelected(AirAlarmPresetMode mode) =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.PresetMode, (int)mode);

        private void HandleDeviceDetailCloseRequested() =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.CloseDevice, 0);

        private void HandleDeviceRowClicked(int deviceIndex) =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.SelectDevice, deviceIndex);

        private void HandleDeviceFilterChanged(string key, bool _)
        {
            int filterIndex = Array.IndexOf(FilterKeys, key);
            if (filterIndex >= 0)
            {
                ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.DeviceFilter, filterIndex);
            }
        }

        private void BindGasRows(IReadOnlyList<AirAlarmGasReadout> rows)
        {
            if (_gasList == null)
            {
                return;
            }

            _gasList.Clear();
            foreach (AirAlarmGasReadout row in rows)
            {
                GasBarRow barRow = new()
                {
                    GasLabel = row.Label,
                    ValueText = $"{row.Percent:0.0}%",
                    ValueTone = row.ValueTone,
                    BarTone = row.BarTone,
                };
                _gasList.Add(barRow);
                barRow.FillPct = row.Percent;
            }
        }

        private void BindAccess(AirAlarmInterfaceViewModel model)
        {
            if (_idReader == null)
            {
                return;
            }

            _idReader.SubText = model.IdReaderSubline;
            _idReader.SetAccessState(!model.AccessGranted, model.AccessScanning, model.AccessGranted);
            if (model.AccessGranted)
            {
                _idReader.SubText = "Preset modes and device control unlocked for this session";
            }
        }

        private void BindModes(AirAlarmInterfaceViewModel model)
        {
            bool locked = !model.AccessGranted;

            if (_modeLockedHint != null)
            {
                _modeLockedHint.text = locked ? "Locked — read ID to change" : string.Empty;
            }

            BindModeButton(_filteringMode, "Filtering", "Normal operation", AirAlarmPresetMode.Filtering, PresetModeAccent.Rust, model, locked);
            BindModeButton(_panicMode, "Panic Siphon", "Purge all gases", AirAlarmPresetMode.Panic, PresetModeAccent.Danger, model, locked);
            BindModeButton(_fillMode, "Repressurize", "Vents run full", AirAlarmPresetMode.Fill, PresetModeAccent.Info, model, locked);
            BindModeButton(_offMode, "Off / None", "Disable control", AirAlarmPresetMode.Off, PresetModeAccent.Steel, model, locked);
        }

        private static void BindModeButton(
            PresetModeButton button,
            string title,
            string description,
            AirAlarmPresetMode mode,
            PresetModeAccent accent,
            AirAlarmInterfaceViewModel model,
            bool locked)
        {
            if (button == null)
            {
                return;
            }

            button.Title = title;
            button.Description = description;
            button.Active = model.ActiveMode == mode;
            button.Disabled = locked;
            button.Accent = accent;
        }

        private void BindDevices(AirAlarmInterfaceViewModel model)
        {
            if (_deviceList == null)
            {
                return;
            }

            _deviceList.Clear();
            AirAlarmConnectedDevice selected = null;

            for (int index = 0; index < model.ConnectedDevices.Count; index++)
            {
                AirAlarmConnectedDevice device = model.ConnectedDevices[index];
                if (device.Id == model.SelectedDeviceId)
                {
                    selected = device;
                }

                ConnectedDeviceRow row = new()
                {
                    TypeLabel = device.Kind == AirAlarmConnectedDeviceKind.Vent ? "VENT" : "SCRUBBER",
                    DeviceName = device.Name,
                    StateText = device.Powered ? "Online" : "Offline",
                    Powered = device.Powered,
                    Selected = device.Id == model.SelectedDeviceId,
                };

                int deviceIndex = index;
                row.Clicked += () => HandleDeviceRowClicked(deviceIndex);
                _deviceList.Add(row);
            }

            if (_deviceDetail == null)
            {
                return;
            }

            if (selected == null)
            {
                _deviceDetail.style.display = DisplayStyle.None;
                return;
            }

            _deviceDetail.style.display = DisplayStyle.Flex;
            _deviceDetail.DeviceName = selected.Name;
            _deviceDetail.DeviceKind = selected.Kind == AirAlarmConnectedDeviceKind.Vent
                ? AirAlarmDeviceKind.Vent
                : AirAlarmDeviceKind.Scrubber;
            _deviceDetail.Powered = selected.Powered;
            _deviceDetail.TargetText = $"{selected.TargetKpa:0} kPa";
            _deviceDetail.ControlsLocked = !model.AccessGranted;

            if (selected.Filters != null)
            {
                foreach (KeyValuePair<string, bool> filter in selected.Filters)
                {
                    _deviceDetail.SetFilterState(filter.Key, filter.Value);
                }
            }
        }

        private static StatusTone GetScenarioTone(AirAlarmScenario scenario)
        {
            return scenario switch
            {
                AirAlarmScenario.Warning => StatusTone.Warning,
                AirAlarmScenario.Danger => StatusTone.Danger,
                _ => StatusTone.Success,
            };
        }

        private static StatusTone GetConnectionTone(AirAlarmScenario scenario)
        {
            return scenario switch
            {
                AirAlarmScenario.Warning => StatusTone.Warning,
                AirAlarmScenario.Danger => StatusTone.Danger,
                _ => StatusTone.Success,
            };
        }
    }
}
