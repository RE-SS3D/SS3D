using SS3D.UI.MachineInterface.Components;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class VentInterfaceBinder
    {
        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly PressureFlowReadout _pressureReadout;
        private readonly AtmosIdReaderRow _idReader;
        private readonly ToggleSwitch _powerToggle;
        private readonly Label _targetGatedHint;
        private readonly NumericStepper _targetStepper;
        private readonly DeviceFooter _footer;

        public VentInterfaceBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _pressureReadout = queryRoot.Q<PressureFlowReadout>("pressure-readout");
            _idReader = queryRoot.Q<AtmosIdReaderRow>("id-reader");
            _powerToggle = queryRoot.Q<ToggleSwitch>("power-toggle");
            _targetGatedHint = queryRoot.Q<Label>("target-gated-hint");
            _targetStepper = queryRoot.Q<NumericStepper>("target-stepper");
            _footer = queryRoot.Q<DeviceFooter>("footer");
        }

        public void Bind(VentInterfaceViewModel model)
        {
            if (_shell != null)
            {
                _shell.ModelLabel = model.ModelLabel;
                _shell.PowerOk = model.ChassisPowerOk;
            }

            if (_connectionRow != null)
            {
                _connectionRow.StatusText = model.ConnectionStatus;
                _connectionRow.ReadoutText = string.Empty;
                _connectionRow.DotTone = GetConnectionTone(model.Scenario);
            }

            if (_identity != null)
            {
                _identity.Title = model.DeviceTitle;
                _identity.Subtitle = model.Subtitle;
            }

            StatusTone statusTone = GetScenarioTone(model.Scenario);
            _statusChip?.SetContent(model.StatusHeadline, model.StatusBadgeText, statusTone, model.StatusSubline);

            if (_pressureReadout != null)
            {
                _pressureReadout.ExternalValue = model.ExternalPressureText;
                _pressureReadout.InternalValue = model.InternalPressureText;
                _pressureReadout.FlowGlyph = model.FlowGlyph;
                _pressureReadout.ExternalTone = model.ExternalTone;
                _pressureReadout.FlowTone = model.FlowTone;
            }

            BindAccess(model);

            bool locked = !model.AccessGranted;
            string gatedHint = locked ? "Locked — read ID" : string.Empty;

            if (_powerToggle != null)
            {
                _powerToggle.IsOn = model.Powered;
            }

            if (_targetGatedHint != null)
            {
                _targetGatedHint.text = gatedHint;
            }

            if (_targetStepper != null)
            {
                _targetStepper.ValueText = $"{model.TargetPressureKpa} kPa";
                _targetStepper.HintText = string.Empty;
                _targetStepper.Locked = locked;
            }

            if (_footer != null)
            {
                _footer.Text = model.FooterText;
            }
        }

        private void BindAccess(VentInterfaceViewModel model)
        {
            if (_idReader == null)
            {
                return;
            }

            _idReader.SubText = model.IdReaderSubline;
            _idReader.SetAccessState(!model.AccessGranted, model.AccessScanning, model.AccessGranted);
            if (model.AccessGranted)
            {
                _idReader.SubText = "Power and target pressure unlocked for this session";
            }
        }

        private static StatusTone GetScenarioTone(VentScenario scenario)
        {
            return scenario switch
            {
                VentScenario.Pressurizing => StatusTone.Info,
                VentScenario.Depressurizing => StatusTone.Warning,
                VentScenario.Fault => StatusTone.Danger,
                _ => StatusTone.Neutral,
            };
        }

        private static StatusTone GetConnectionTone(VentScenario scenario)
        {
            return scenario switch
            {
                VentScenario.Pressurizing => StatusTone.Info,
                VentScenario.Depressurizing => StatusTone.Warning,
                VentScenario.Fault => StatusTone.Danger,
                _ => StatusTone.Success,
            };
        }
    }
}
