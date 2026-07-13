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
        private readonly MachineInterfaceAccessGate _accessGate;

        public SmesUnitBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement contentRoot = _shell ?? root;

            _connectionRow = contentRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = contentRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = contentRoot.Q<GlanceableStatusChip>("status-chip");
            _storageCells = contentRoot.Q<StorageCellRow>("storage-cells");
            _chargePct = contentRoot.Q<Label>("charge-pct");
            _chargeTrendText = contentRoot.Q<Label>("charge-trend-text");
            _powerFlow = contentRoot.Q<SmesPowerFlowRow>("power-flow");
            _inputControl = contentRoot.Q<RateControlSection>("input-control");
            _outputControl = contentRoot.Q<RateControlSection>("output-control");
            _footer = contentRoot.Q<DeviceFooter>("footer");

            VisualElement lockedPanel = contentRoot.Q<VisualElement>("access-locked");
            VisualElement unlockedPanel = contentRoot.Q<VisualElement>("access-unlocked");
            AccessGatePanel gatePanel = contentRoot.Q<AccessGatePanel>("access-gate");
            AccessStrip accessStrip = contentRoot.Q<AccessStrip>("access-strip");

            if (gatePanel != null)
            {
                gatePanel.LockedSubline = "Insert ID card to access input/output controls";
            }

            _accessGate = new MachineInterfaceAccessGate(lockedPanel, unlockedPanel, gatePanel);
            _accessGate.Reset();

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }

            if (accessStrip != null)
            {
                accessStrip.LockRequested += () => _accessGate.Lock();
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
        }

        public void Disconnect()
        {
            _accessGate?.Disconnect();

            if (_shell != null)
            {
                _shell.CloseClicked -= HandleCloseRequested;
            }
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }
    }
}
