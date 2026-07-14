using SS3D.UI.MachineInterface.Components;
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class ScrubberInterfaceBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly VisualElement _gasList;
        private readonly AtmosIdReaderRow _idReader;
        private readonly ToggleSwitch _powerToggle;
        private readonly Label _powerGatedHint;
        private readonly Label _filterGatedHint;
        private readonly CompactFilterToggle _o2Filter;
        private readonly CompactFilterToggle _n2Filter;
        private readonly CompactFilterToggle _co2Filter;
        private readonly CompactFilterToggle _plasmaFilter;
        private readonly CompactFilterToggle _toxinsFilter;
        private readonly NumericStepper _flowStepper;
        private readonly DeviceFooter _footer;

        public ScrubberInterfaceBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _gasList = queryRoot.Q<VisualElement>("gas-list");
            _idReader = queryRoot.Q<AtmosIdReaderRow>("id-reader");
            _powerToggle = queryRoot.Q<ToggleSwitch>("power-toggle");
            _powerGatedHint = queryRoot.Q<Label>("power-gated-hint");
            _filterGatedHint = queryRoot.Q<Label>("filter-gated-hint");
            _o2Filter = queryRoot.Q<CompactFilterToggle>("filter-o2");
            _n2Filter = queryRoot.Q<CompactFilterToggle>("filter-n2");
            _co2Filter = queryRoot.Q<CompactFilterToggle>("filter-co2");
            _plasmaFilter = queryRoot.Q<CompactFilterToggle>("filter-plasma");
            _toxinsFilter = queryRoot.Q<CompactFilterToggle>("filter-toxins");
            _flowStepper = queryRoot.Q<NumericStepper>("flow-stepper");
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

            WireFilter(_o2Filter, OnO2FilterChanged);
            WireFilter(_n2Filter, OnN2FilterChanged);
            WireFilter(_co2Filter, OnCo2FilterChanged);
            WireFilter(_plasmaFilter, OnPlasmaFilterChanged);
            WireFilter(_toxinsFilter, OnToxinsFilterChanged);
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not ScrubberInterfaceViewModel model)
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
                _connectionRow.ReadoutText = model.ThroughputText;
                _connectionRow.DotTone = GetConnectionTone(model.Scenario);
            }

            if (_identity != null)
            {
                _identity.Title = model.DeviceTitle;
                _identity.Subtitle = model.Subtitle;
            }

            StatusTone statusTone = GetScenarioTone(model.Scenario);
            _statusChip?.SetContent(model.StatusHeadline, model.StatusBadgeText, statusTone, model.StatusSubline);
            BindGasRows(model.GasReadouts);
            BindAccess(model);

            bool locked = !model.AccessGranted;
            string gatedHint = locked ? "Locked — read ID" : string.Empty;

            if (_powerToggle != null)
            {
                _powerToggle.IsOn = model.Powered;
                _powerToggle.Locked = locked;
            }

            if (_powerGatedHint != null)
            {
                _powerGatedHint.text = gatedHint;
            }

            if (_filterGatedHint != null)
            {
                _filterGatedHint.text = gatedHint;
            }

            BindFilter(_o2Filter, "O2", model, locked);
            BindFilter(_n2Filter, "N2", model, locked);
            BindFilter(_co2Filter, "CO2", model, locked);
            BindFilter(_plasmaFilter, "Plasma", model, locked);
            BindFilter(_toxinsFilter, "Toxins", model, locked);

            if (_flowStepper != null)
            {
                _flowStepper.ValueText = $"{model.FlowRate} L/s";
                _flowStepper.HintText = "Flow rate";
                _flowStepper.Locked = locked;
            }

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

            UnwireFilter(_o2Filter, OnO2FilterChanged);
            UnwireFilter(_n2Filter, OnN2FilterChanged);
            UnwireFilter(_co2Filter, OnCo2FilterChanged);
            UnwireFilter(_plasmaFilter, OnPlasmaFilterChanged);
            UnwireFilter(_toxinsFilter, OnToxinsFilterChanged);
        }

        private void HandleCloseRequested() => CloseRequested?.Invoke();

        private void HandleReadRequested() =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.ReadId, 0);

        private void HandlePowerChanged(bool value) =>
            BoolControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.Power, value);

        private void HandleFlowDeltaRequested(float delta) =>
            NumericControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.FlowRate, delta);

        private void HandleFilterChanged(int filterIndex) =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.DeviceFilter, filterIndex);

        private void OnO2FilterChanged(bool _) => HandleFilterChanged(0);

        private void OnN2FilterChanged(bool _) => HandleFilterChanged(1);

        private void OnCo2FilterChanged(bool _) => HandleFilterChanged(2);

        private void OnPlasmaFilterChanged(bool _) => HandleFilterChanged(3);

        private void OnToxinsFilterChanged(bool _) => HandleFilterChanged(4);

        private static void WireFilter(CompactFilterToggle toggle, Action<bool> handler)
        {
            if (toggle != null)
            {
                toggle.ValueChanged += handler;
            }
        }

        private static void UnwireFilter(CompactFilterToggle toggle, Action<bool> handler)
        {
            if (toggle != null)
            {
                toggle.ValueChanged -= handler;
            }
        }

        private static void BindFilter(
            CompactFilterToggle toggle,
            string key,
            ScrubberInterfaceViewModel model,
            bool locked)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.IsOn = model.Filters.TryGetValue(key, out bool enabled) && enabled;
            toggle.Locked = locked;
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

        private void BindAccess(ScrubberInterfaceViewModel model)
        {
            if (_idReader == null)
            {
                return;
            }

            _idReader.SetIdleSubline(model.IdReaderSubline);
            _idReader.SetGrantedSubline("Gas filter selection and flow rate unlocked for this session");
            _idReader.SetAccessState(model.AccessScanning, model.AccessGranted, model.AccessDenied);
        }

        private static StatusTone GetScenarioTone(ScrubberScenario scenario)
        {
            return scenario switch
            {
                ScrubberScenario.Overloaded => StatusTone.Warning,
                ScrubberScenario.Fault => StatusTone.Danger,
                ScrubberScenario.Idle => StatusTone.Neutral,
                _ => StatusTone.Success,
            };
        }

        private static StatusTone GetConnectionTone(ScrubberScenario scenario)
        {
            return scenario switch
            {
                ScrubberScenario.Overloaded => StatusTone.Warning,
                ScrubberScenario.Fault => StatusTone.Danger,
                ScrubberScenario.Idle => StatusTone.Info,
                _ => StatusTone.Success,
            };
        }
    }
}
