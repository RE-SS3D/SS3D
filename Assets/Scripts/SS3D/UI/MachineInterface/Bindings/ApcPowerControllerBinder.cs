using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class ApcPowerControllerBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly GlanceableStatusChip _statusChip;
        private readonly PowerFlowRow _powerFlow;
        private readonly Label _batteryStateHeader;
        private readonly BatteryBar _batteryBar;
        private readonly DiagnosticsList _diagnosticsList;
        private readonly ChannelRow _lightingChannel;
        private readonly ChannelRow _equipmentChannel;
        private readonly ChannelRow _environmentChannel;
        private readonly DeviceFooter _footer;
        private readonly MachineInterfaceAccessGate _accessGate;

        public ApcPowerControllerBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement contentRoot = _shell ?? root;

            _connectionRow = contentRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = contentRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = contentRoot.Q<GlanceableStatusChip>("status-chip");
            _powerFlow = contentRoot.Q<PowerFlowRow>("power-flow");
            _batteryStateHeader = contentRoot.Q<Label>("battery-state-header");
            _batteryBar = contentRoot.Q<BatteryBar>("battery-bar");
            _diagnosticsList = contentRoot.Q<DiagnosticsList>("diagnostics-list");
            _lightingChannel = contentRoot.Q<ChannelRow>("channel-lighting");
            _equipmentChannel = contentRoot.Q<ChannelRow>("channel-equipment");
            _environmentChannel = contentRoot.Q<ChannelRow>("channel-environment");
            _footer = contentRoot.Q<DeviceFooter>("footer");

            VisualElement lockedPanel = contentRoot.Q<VisualElement>("access-locked");
            VisualElement unlockedPanel = contentRoot.Q<VisualElement>("access-unlocked");
            AccessGatePanel gatePanel = contentRoot.Q<AccessGatePanel>("access-gate");
            AccessStrip accessStrip = contentRoot.Q<AccessStrip>("access-strip");

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

            if (_lightingChannel != null)
            {
                _lightingChannel.ValueChanged += value =>
                    BoolControlChanged?.Invoke(MachineInterfaceControlIds.Apc.Lighting, value);
            }

            if (_equipmentChannel != null)
            {
                _equipmentChannel.ValueChanged += value =>
                    BoolControlChanged?.Invoke(MachineInterfaceControlIds.Apc.Equipment, value);
            }

            if (_environmentChannel != null)
            {
                _environmentChannel.ValueChanged += value =>
                    BoolControlChanged?.Invoke(MachineInterfaceControlIds.Apc.Environment, value);
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not ApcInterfaceViewModel model)
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
                _connectionRow.ReadoutText = model.HeaderReadout;
                _connectionRow.DotTone = StatusTone.Info;
            }

            if (_identity != null)
            {
                _identity.Title = model.DeviceTitle;
                _identity.Subtitle = model.Subtitle;
            }

            StatusTone tone = model.State switch
            {
                ApcPowerState.Overload => StatusTone.Warning,
                ApcPowerState.Critical => StatusTone.Danger,
                _ => StatusTone.Success,
            };

            string badgeText = model.State switch
            {
                ApcPowerState.Overload => "OVERLOAD",
                ApcPowerState.Critical => "CRITICAL",
                _ => "NOMINAL",
            };

            _statusChip?.SetContent(model.StatusHeadline, badgeText, tone);
            BindPowerFlow(model, tone);
            _batteryBar.Value = model.BatteryCharge;
            _batteryBar.StateText = model.BatteryStateText;
            if (_batteryStateHeader != null)
            {
                _batteryStateHeader.text = model.BatteryStateText;
            }

            if (_lightingChannel != null)
            {
                _lightingChannel.LoadKw = model.LightingLoadKw;
                _lightingChannel.IsOn = model.LightingOn;
            }

            if (_equipmentChannel != null)
            {
                _equipmentChannel.LoadKw = model.EquipmentLoadKw;
                _equipmentChannel.IsOn = model.EquipmentOn;
            }

            if (_environmentChannel != null)
            {
                _environmentChannel.LoadKw = model.EnvironmentLoadKw;
                _environmentChannel.IsOn = model.EnvironmentOn;
            }

            _diagnosticsList?.SetLines(model.Diagnostics);

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

        private void BindPowerFlow(ApcInterfaceViewModel model, StatusTone tone)
        {
            _powerFlow.GridInputKw = model.GridInputKw;
            _powerFlow.LoadOutputKw = model.LoadOutputKw;

            switch (model.State)
            {
                case ApcPowerState.Critical:
                {
                    _powerFlow.SetFlowDisplay("✕", StatusTone.Danger, StatusTone.Danger);
                    break;
                }

                case ApcPowerState.Overload:
                {
                    _powerFlow.SetFlowDisplay("⇄", StatusTone.Warning, StatusTone.Warning);
                    break;
                }

                default:
                {
                    _powerFlow.SetFlowDisplay("→", StatusTone.Success, StatusTone.Success);
                    break;
                }
            }
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }
    }
}
