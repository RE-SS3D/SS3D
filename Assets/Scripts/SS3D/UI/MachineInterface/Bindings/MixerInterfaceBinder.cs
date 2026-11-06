using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public sealed class MixerInterfaceBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly GasPortCard _inlet1;
        private readonly GasPortCard _inlet2;
        private readonly GasPortCard _outlet;
        private readonly Label _outletTarget;
        private readonly Label _flowGlyph;
        private readonly AtmosIdReaderRow _idReader;
        private readonly ToggleSwitch _powerToggle;
        private readonly Label _targetGatedHint;
        private readonly NumericStepper _targetStepper;
        private readonly Label _ratioGatedHint;
        private readonly Button _ratioDown;
        private readonly Button _ratioUp;
        private readonly Label _ratio1Text;
        private readonly Label _ratio2Text;
        private readonly VisualElement _ratioBar;
        private readonly VisualElement _ratio1Fill;
        private readonly VisualElement _ratio2Fill;
        private readonly DeviceFooter _footer;

        public MixerInterfaceBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _inlet1 = queryRoot.Q<GasPortCard>("inlet-1-card");
            _inlet2 = queryRoot.Q<GasPortCard>("inlet-2-card");
            _outlet = queryRoot.Q<GasPortCard>("outlet-card");
            _outletTarget = queryRoot.Q<Label>("outlet-target");
            _flowGlyph = queryRoot.Q<Label>("flow-glyph");
            _idReader = queryRoot.Q<AtmosIdReaderRow>("id-reader");
            _powerToggle = queryRoot.Q<ToggleSwitch>("power-toggle");
            _targetGatedHint = queryRoot.Q<Label>("target-gated-hint");
            _targetStepper = queryRoot.Q<NumericStepper>("target-stepper");
            _ratioGatedHint = queryRoot.Q<Label>("ratio-gated-hint");
            _ratioDown = queryRoot.Q<Button>("ratio-down");
            _ratioUp = queryRoot.Q<Button>("ratio-up");
            _ratio1Text = queryRoot.Q<Label>("ratio-1-text");
            _ratio2Text = queryRoot.Q<Label>("ratio-2-text");
            _ratioBar = queryRoot.Q<VisualElement>("ratio-bar");
            _ratio1Fill = queryRoot.Q<VisualElement>("ratio-1-fill");
            _ratio2Fill = queryRoot.Q<VisualElement>("ratio-2-fill");
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

            if (_ratioDown != null)
            {
                _ratioDown.clicked += HandleRatioDown;
            }

            if (_ratioUp != null)
            {
                _ratioUp.clicked += HandleRatioUp;
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not MixerInterfaceViewModel model)
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
                _connectionRow.ReadoutText = string.Empty;
                _connectionRow.DotTone = tone == StatusTone.Neutral ? StatusTone.Info : tone;
            }

            if (_identity != null)
            {
                _identity.Title = model.DeviceTitle;
                _identity.Subtitle = model.Subtitle;
            }

            _statusChip?.SetContent(model.StatusHeadline, model.StatusBadgeText, tone, model.StatusSubline);
            _inlet1?.Bind(model.Inlet1);
            _inlet2?.Bind(model.Inlet2);
            _outlet?.Bind(model.Outlet);

            if (_outletTarget != null)
            {
                _outletTarget.text = model.OutletTargetText;
            }

            if (_flowGlyph != null)
            {
                _flowGlyph.text = model.FlowGlyph;
                StatusToneUtility.ApplyTone(_flowGlyph, model.FlowTone);
            }

            if (_idReader != null)
            {
                _idReader.SetIdleSubline("Read an ID to unlock power, inlet ratio and target pressure");
                _idReader.SetGrantedSubline("Power, inlet ratio and target pressure unlocked for this session");
                _idReader.SetAccessState(model.AccessScanning, model.AccessGranted, model.AccessDenied);
            }

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
                _targetStepper.Locked = locked;
            }

            BindRatio(model, locked, gatedHint);

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

            if (_ratioDown != null)
            {
                _ratioDown.clicked -= HandleRatioDown;
            }

            if (_ratioUp != null)
            {
                _ratioUp.clicked -= HandleRatioUp;
            }
        }

        private void BindRatio(MixerInterfaceViewModel model, bool locked, string gatedHint)
        {
            int percent1 = model.Inlet1Percent;
            int percent2 = 100 - percent1;

            if (_ratioGatedHint != null)
            {
                _ratioGatedHint.text = gatedHint;
            }

            _ratioDown?.SetEnabled(!locked);
            _ratioUp?.SetEnabled(!locked);

            if (_ratio1Text != null)
            {
                _ratio1Text.text = $"IN1 {percent1}%";
            }

            if (_ratio2Text != null)
            {
                _ratio2Text.text = $"{percent2}% IN2";
            }

            _ratioBar?.EnableInClassList("mixer-ratio-bar--locked", locked);

            if (_ratio1Fill != null)
            {
                _ratio1Fill.style.flexGrow = percent1;
                _ratio1Fill.style.backgroundColor = model.Inlet1Color;
            }

            if (_ratio2Fill != null)
            {
                _ratio2Fill.style.flexGrow = percent2;
                _ratio2Fill.style.backgroundColor = model.Inlet2Color;
            }
        }

        private void HandleCloseRequested() => CloseRequested?.Invoke();

        private void HandleReadRequested() =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.ReadId, 0);

        private void HandlePowerChanged(bool value) =>
            BoolControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.Power, value);

        private void HandleTargetDeltaRequested(float delta) =>
            NumericControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.TargetPressure, delta);

        private void HandleRatioDown() =>
            NumericControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.InletRatio, -1f);

        private void HandleRatioUp() =>
            NumericControlChanged?.Invoke(MachineInterfaceControlIds.Atmos.InletRatio, 1f);

        private static StatusTone GetScenarioTone(MixerScenario scenario)
        {
            return scenario switch
            {
                MixerScenario.Mixing => StatusTone.Success,
                MixerScenario.Stopped => StatusTone.Warning,
                _ => StatusTone.Neutral,
            };
        }
    }
}
