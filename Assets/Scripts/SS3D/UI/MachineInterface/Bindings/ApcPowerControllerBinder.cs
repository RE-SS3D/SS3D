using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine.UIElements;
using SS3D.Systems.Electricity;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class ApcPowerControllerBinder : IMachineInterfaceBinder
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
        private readonly PowerFlowRow _powerFlow;
        private readonly Label _batteryStateHeader;
        private readonly BatteryBar _batteryBar;
        private readonly DiagnosticsList _diagnosticsList;
        private readonly ChannelRow _lightingChannel;
        private readonly ChannelRow _equipmentChannel;
        private readonly ChannelRow _environmentChannel;
        private readonly DeviceFooter _footer;
        private readonly AccessGatedRegion _accessRegion;

        public ApcPowerControllerBinder(VisualElement root)
        {
            _root = root;
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement queryRoot = _shell?.ScreenContent ?? _root;

            _connectionRow = queryRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = queryRoot.Q<DeviceIdentityBlock>("identity");
            _statusChip = queryRoot.Q<GlanceableStatusChip>("status-chip");
            _powerFlow = queryRoot.Q<PowerFlowRow>("power-flow");
            _batteryStateHeader = queryRoot.Q<Label>("battery-state-header");
            _batteryBar = queryRoot.Q<BatteryBar>("battery-bar");
            _diagnosticsList = queryRoot.Q<DiagnosticsList>("diagnostics-list");
            _lightingChannel = queryRoot.Q<ChannelRow>("channel-lighting");
            _equipmentChannel = queryRoot.Q<ChannelRow>("channel-equipment");
            _environmentChannel = queryRoot.Q<ChannelRow>("channel-environment");
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
                    ActionControlChanged?.Invoke(MachineInterfaceControlIds.Apc.ReadId, 0);
            }

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
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

            if (_batteryBar != null)
            {
                _batteryBar.Value = model.BatteryCharge;
                _batteryBar.StateText = model.BatteryStateText;
            }

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

            _accessRegion?.ApplyAccessState(model.AccessGranted, model.AccessScanning, model.AccessDenied);

            _diagnosticsList?.SetLines(model.Diagnostics);

            if (_footer != null)
            {
                _footer.Text = model.FooterText;
            }
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

        private void BindPowerFlow(ApcInterfaceViewModel model, StatusTone tone)
        {
            if (_powerFlow == null)
            {
                return;
            }

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
                    _powerFlow.SetFlowDisplay("→", StatusTone.Success, StatusTone.Neutral);
                    break;
                }
            }
        }

        private void HandleCloseRequested()
        {
            CloseRequested?.Invoke();
        }

        private void HandleSwipeRequested() =>
            ActionControlChanged?.Invoke(MachineInterfaceControlIds.Apc.ReadId, 0);
    }
}
