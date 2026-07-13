using SS3D.UI.MachineInterface.Components;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class ScrubberInterfaceBinder
    {
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
        }

        public void Bind(ScrubberInterfaceViewModel model)
        {
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
                    FillPct = row.Percent,
                    ValueTone = row.ValueTone,
                    BarTone = row.BarTone,
                };
                _gasList.Add(barRow);
            }
        }

        private void BindAccess(ScrubberInterfaceViewModel model)
        {
            if (_idReader == null)
            {
                return;
            }

            _idReader.SubText = model.IdReaderSubline;
            _idReader.SetAccessState(!model.AccessGranted, model.AccessScanning, model.AccessGranted);
            if (model.AccessGranted)
            {
                _idReader.SubText = "Gas filter selection and flow rate unlocked for this session";
            }
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
