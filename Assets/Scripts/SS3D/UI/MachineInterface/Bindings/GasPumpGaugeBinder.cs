using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public sealed class GasPumpGaugeBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly PressureFlowReadout _pressureReadout;
        private readonly Label _flowStatusValue;
        private readonly AtmosIdReaderRow _idReader;
        private readonly ToggleSwitch _powerToggle;
        private readonly Label _targetGatedHint;
        private readonly NumericStepper _targetStepper;
        private readonly DeviceFooter _footer;

        public GasPumpGaugeBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _pressureReadout = queryRoot.Q<PressureFlowReadout>("pressure-readout");
            _flowStatusValue = queryRoot.Q<Label>("flow-status-value");
            _idReader = queryRoot.Q<AtmosIdReaderRow>("id-reader");
            _powerToggle = queryRoot.Q<ToggleSwitch>("power-toggle");
            _targetGatedHint = queryRoot.Q<Label>("target-gated-hint");
            _targetStepper = queryRoot.Q<NumericStepper>("target-stepper");
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

            if (_targetStepper != null)
            {
                _targetStepper.DeltaRequested += HandleTargetDeltaRequested;
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not GasPumpInterfaceViewModel model)
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
                _pressureReadout.ExternalLabel = "Inlet";
                _pressureReadout.InternalLabel = "Outlet";
                _pressureReadout.ExternalValue = model.InletPressureText;
                _pressureReadout.InternalValue = model.OutletPressureText;
                _pressureReadout.FlowGlyph = model.FlowGlyph;
                _pressureReadout.ExternalTone = model.InletTone;
                _pressureReadout.InternalTone = model.OutletTone;
                _pressureReadout.FlowTone = model.FlowTone;
            }

            if (_flowStatusValue != null)
            {
                _flowStatusValue.text = model.FlowStatusText;
                StatusToneUtility.ApplyTone(_flowStatusValue, model.FlowStatusTone);
            }

            BindAccess(model);

            bool locked = !model.AccessGranted;
            string gatedHint = locked ? "Locked — read ID" : string.Empty;

            if (_powerToggle != null)
            {
                _powerToggle.IsOn = model.Powered;
                _powerToggle.Locked = locked;
            }

            if (_targetGatedHint != null)
            {
                _targetGatedHint.text = gatedHint;
            }

            if (_targetStepper != null)
            {
                _targetStepper.ValueText = $"{model.TargetOutletPressureKpa} kPa";
                _targetStepper.HintText = string.Empty;
                _targetStepper.Locked = locked;
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

            if (_targetStepper != null)
            {
                _targetStepper.DeltaRequested -= HandleTargetDeltaRequested;
            }
        }

        private void HandleCloseRequested() => CloseRequested?.Invoke();

        private void HandleReadRequested() =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.ReadId, 0);

        private void HandlePowerChanged(bool value) =>
            BoolControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.Power, value);

        private void HandleTargetDeltaRequested(float delta) =>
            NumericControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.TargetPressure, delta);

        private void BindAccess(GasPumpInterfaceViewModel model)
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

        private static StatusTone GetScenarioTone(PumpScenario scenario)
        {
            return scenario switch
            {
                PumpScenario.Pumping => StatusTone.Success,
                PumpScenario.Starved => StatusTone.Warning,
                PumpScenario.Fault => StatusTone.Danger,
                _ => StatusTone.Neutral,
            };
        }

        private static StatusTone GetConnectionTone(PumpScenario scenario)
        {
            return scenario switch
            {
                PumpScenario.Pumping => StatusTone.Success,
                PumpScenario.Starved => StatusTone.Warning,
                PumpScenario.Fault => StatusTone.Danger,
                _ => StatusTone.Info,
            };
        }
    }
}
