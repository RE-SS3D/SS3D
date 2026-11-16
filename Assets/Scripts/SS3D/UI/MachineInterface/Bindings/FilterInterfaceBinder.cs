using SS3D.UI.MachineInterface.Components;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public sealed class FilterInterfaceBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly GasPortCard _inlet;
        private readonly GasPortCard _outlet;
        private readonly GasPortCard _filterOutlet;
        private readonly Label _flowGlyph;
        private readonly AtmosIdReaderRow _idReader;
        private readonly ToggleSwitch _powerToggle;
        private readonly Label _flowGatedHint;
        private readonly NumericStepper _flowStepper;
        private readonly Label _registryText;
        private readonly VisualElement _gasGrid;
        private readonly DeviceFooter _footer;
        private readonly List<GasFilterRow> _gasRows = new();

        public FilterInterfaceBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _inlet = queryRoot.Q<GasPortCard>("inlet-card");
            _outlet = queryRoot.Q<GasPortCard>("outlet-card");
            _filterOutlet = queryRoot.Q<GasPortCard>("filter-outlet-card");
            _flowGlyph = queryRoot.Q<Label>("flow-glyph");
            _idReader = queryRoot.Q<AtmosIdReaderRow>("id-reader");
            _powerToggle = queryRoot.Q<ToggleSwitch>("power-toggle");
            _flowGatedHint = queryRoot.Q<Label>("flow-gated-hint");
            _flowStepper = queryRoot.Q<NumericStepper>("flow-stepper");
            _registryText = queryRoot.Q<Label>("registry-text");
            _gasGrid = queryRoot.Q<VisualElement>("gas-grid");
            _footer = queryRoot.Q<DeviceFooter>("footer");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }

            if (_idReader != null)
            {
                _idReader.ReadRequested += HandleReadRequested;
            }

            if (_powerToggle != null)
            {
                _powerToggle.ValueChanged += HandlePowerChanged;
            }

            if (_flowStepper != null)
            {
                _flowStepper.DeltaRequested += HandleFlowDeltaRequested;
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not FilterInterfaceViewModel model)
            {
                return;
            }

            if (_shell != null)
            {
                _shell.ModelLabel = model.ModelLabel;
                _shell.PowerOk = model.ChassisPowerOk;
            }

            StatusTone tone = GetScenarioTone(model.Scenario);
            if (_connectionRow != null)
            {
                _connectionRow.StatusText = model.ConnectionStatus;
                _connectionRow.ReadoutText = model.ThroughputText;
                _connectionRow.DotTone = tone == StatusTone.Neutral ? StatusTone.Info : tone;
            }

            if (_identity != null)
            {
                _identity.Title = model.DeviceTitle;
                _identity.Subtitle = model.Subtitle;
            }

            _statusChip?.SetContent(model.StatusHeadline, model.StatusBadgeText, tone, model.StatusSubline);
            _inlet?.Bind(model.Inlet);
            _outlet?.Bind(model.Outlet);
            _filterOutlet?.Bind(model.FilterOutlet);

            if (_flowGlyph != null)
            {
                _flowGlyph.text = model.FlowGlyph;
                StatusToneUtility.ApplyTone(_flowGlyph, model.FlowTone);
            }

            if (_idReader != null)
            {
                _idReader.SetIdleSubline("Read an ID to unlock power, flow rate and gas selection");
                _idReader.SetGrantedSubline("Power, flow rate and gas selection unlocked for this session");
                _idReader.SetAccessState(model.AccessScanning, model.AccessGranted, model.AccessDenied);
            }

            bool locked = !model.AccessGranted;
            if (_powerToggle != null)
            {
                _powerToggle.IsOn = model.Powered;
                _powerToggle.Locked = locked;
            }

            if (_flowGatedHint != null)
            {
                _flowGatedHint.text = locked ? "Locked — read ID" : string.Empty;
            }

            if (_flowStepper != null)
            {
                _flowStepper.ValueText = $"{model.FlowLitresPerSecond} L/s";
                _flowStepper.Locked = locked;
            }

            if (_registryText != null)
            {
                _registryText.text = model.RegistryText;
            }

            BindGases(model.Gases, locked);

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

            if (_powerToggle != null)
            {
                _powerToggle.ValueChanged -= HandlePowerChanged;
            }

            if (_flowStepper != null)
            {
                _flowStepper.DeltaRequested -= HandleFlowDeltaRequested;
            }

            foreach (GasFilterRow row in _gasRows)
            {
                row.Toggled -= HandleGasToggled;
            }
        }

        // Rows stay put between refreshes, so a click isn't lost to one arriving under the pointer.
        private void BindGases(IReadOnlyList<GasFilterRowView> gases, bool locked)
        {
            if (_gasGrid == null)
            {
                return;
            }

            while (_gasRows.Count < gases.Count)
            {
                GasFilterRow row = new();
                row.Toggled += HandleGasToggled;
                _gasRows.Add(row);
                _gasGrid.Add(row);
            }

            for (int i = 0; i < gases.Count; i++)
            {
                _gasRows[i].Bind(gases[i], locked);
            }
        }

        private void HandleCloseRequested() => CloseRequested?.Invoke();

        private void HandleReadRequested() =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.ReadId, 0);

        private void HandlePowerChanged(bool value) =>
            BoolControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.Power, value);

        private void HandleFlowDeltaRequested(float delta) =>
            NumericControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.FlowRate, delta);

        private void HandleGasToggled(byte gasId) =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.DeviceFilter, gasId);

        private static StatusTone GetScenarioTone(FilterScenario scenario)
        {
            return scenario switch
            {
                FilterScenario.Filtering => StatusTone.Success,
                FilterScenario.Stalled => StatusTone.Warning,
                _ => StatusTone.Neutral,
            };
        }
    }
}
