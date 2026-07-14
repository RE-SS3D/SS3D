using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class SmesUnitBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private readonly VisualElement _root;
        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly StorageCellRow _storageCells;
        private readonly Label _chargePct;
        private readonly Label _chargeTrendText;
        private readonly SmesPowerFlowRow _powerFlow;
        private readonly RateControlSection _inputControl;
        private readonly RateControlSection _outputControl;
        private readonly DeviceFooter _footer;
        private readonly AccessGatedRegion _accessRegion;

        public SmesUnitBinder(VisualElement root)
        {
            _root = root;
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? _root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _storageCells = queryRoot.Q<StorageCellRow>("storage-cells");
            _chargePct = queryRoot.Q<Label>("charge-pct");
            _chargeTrendText = queryRoot.Q<Label>("charge-trend-text");
            _powerFlow = queryRoot.Q<SmesPowerFlowRow>("power-flow");
            _inputControl = queryRoot.Q<RateControlSection>("input-control");
            _outputControl = queryRoot.Q<RateControlSection>("output-control");
            _footer = queryRoot.Q<DeviceFooter>("footer");

            _accessRegion = queryRoot.Q<AccessGatedRegion>("access-region");
            AccessStrip accessStrip = queryRoot.Q<AccessStrip>("access-strip");

            _accessRegion?.Initialize(serverDriven: true);

            if (_accessRegion?.Gate != null)
            {
                _accessRegion.Gate.SwipeRequested += HandleSwipeRequested;
            }

            if (accessStrip != null && _accessRegion != null)
            {
                accessStrip.LockRequested += () =>
                    ActionControlChanged?.Invoke(MachineInterfaceControlIds.Smes.ReadId, 0);
            }

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }

            if (_inputControl != null)
            {
                _inputControl.SectionTitle = "INPUT";
                _inputControl.ToggleChanged += value =>
                    BoolControlChanged?.Invoke(MachineInterfaceControlIds.Smes.Input, value);
                _inputControl.RateDeltaRequested += delta =>
                    NumericControlChanged?.Invoke(MachineInterfaceControlIds.Smes.Input, delta);
            }

            if (_outputControl != null)
            {
                _outputControl.SectionTitle = "OUTPUT";
                _outputControl.ToggleChanged += value =>
                    BoolControlChanged?.Invoke(MachineInterfaceControlIds.Smes.Output, value);
                _outputControl.RateDeltaRequested += delta =>
                    NumericControlChanged?.Invoke(MachineInterfaceControlIds.Smes.Output, delta);
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not SmesInterfaceViewModel model)
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
                _connectionRow.DotTone = model.ChassisPowerOk ? StatusTone.Success : StatusTone.Danger;
            }

            if (_identity != null)
            {
                _identity.Title = model.DeviceTitle;
                _identity.Subtitle = model.Subtitle;
            }

            StatusTone tone = SmesUnitBinderHelpers.GetStatusTone(model.State);
            _statusChip?.SetContent(
                model.ExteriorStatusWord,
                model.StatusBadgeText,
                tone,
                model.ConnectionStateText);

            StatusTone chargeTone = SmesUnitBinderHelpers.GetChargeTone(model.ChargePct);
            _storageCells?.SetCharge(model.ChargePct, chargeTone);

            if (_chargePct != null)
            {
                _chargePct.text = $"{Mathf.RoundToInt(model.ChargePct * 100f)}%";
                StatusToneUtility.ApplyTone(_chargePct, chargeTone);
            }

            if (_chargeTrendText != null)
            {
                _chargeTrendText.text = SmesUnitBinderHelpers.GetTrendText(model.ChargeTrend);
            }

            StatusTone inputTone = SmesUnitBinderHelpers.GetInputTone(model);
            StatusTone outputTone = SmesUnitBinderHelpers.GetOutputTone(model);
            _powerFlow?.SetFlow(model.InputActive, model.OutputActive, inputTone, outputTone);

            if (_inputControl != null)
            {
                _inputControl.IsEnabled = model.InputEnabled;
                _inputControl.CurrentKw = model.InputCurrentKw;
                _inputControl.MaxKw = model.InputMaxKw;
                _inputControl.SetAccentTone(inputTone);
            }

            if (_outputControl != null)
            {
                _outputControl.IsEnabled = model.OutputEnabled;
                _outputControl.CurrentKw = model.OutputCurrentKw;
                _outputControl.MaxKw = model.OutputMaxKw;
                _outputControl.SetAccentTone(outputTone);
            }

            if (_footer != null)
            {
                _footer.Text = model.FooterText;
            }

            _accessRegion?.ApplyAccessState(model.AccessGranted, model.AccessScanning, model.AccessDenied);
        }

        public void Disconnect()
        {
            if (_accessRegion?.Gate != null)
            {
                _accessRegion.Gate.SwipeRequested -= HandleSwipeRequested;
            }

            if (_shell != null)
            {
                _shell.CloseClicked -= HandleCloseRequested;
            }
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void HandleSwipeRequested() =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Smes.ReadId, 0);
    }
}
