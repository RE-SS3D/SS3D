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

        private readonly MachineWindow _window;
        private readonly StatusBanner _statusBanner;
        private readonly PowerFlowRow _powerFlow;
        private readonly BatteryBar _batteryBar;
        private readonly ChannelRow _lightingChannel;
        private readonly ChannelRow _equipmentChannel;
        private readonly ChannelRow _environmentChannel;
        private readonly DiagnosticsList _diagnosticsList;

        public ApcPowerControllerBinder(VisualElement root)
        {
            _window = root.Q<MachineWindow>("machine-window");
            VisualElement contentRoot = root.Q<VisualElement>("apc-root") ?? root;

            _statusBanner = contentRoot.Q<StatusBanner>("status-banner");
            _powerFlow = contentRoot.Q<PowerFlowRow>("power-flow");
            _batteryBar = contentRoot.Q<BatteryBar>("battery-bar");
            _lightingChannel = contentRoot.Q<ChannelRow>("channel-lighting");
            _equipmentChannel = contentRoot.Q<ChannelRow>("channel-equipment");
            _environmentChannel = contentRoot.Q<ChannelRow>("channel-environment");
            _diagnosticsList = contentRoot.Q<DiagnosticsList>("diagnostics-list");

            if (_window != null)
            {
                _window.CloseClicked += HandleWindowCloseClicked;
            }

            _lightingChannel.ValueChanged += value =>
                BoolControlChanged?.Invoke(MachineInterfaceControlIds.Apc.Lighting, value);
            _equipmentChannel.ValueChanged += value =>
                BoolControlChanged?.Invoke(MachineInterfaceControlIds.Apc.Equipment, value);
            _environmentChannel.ValueChanged += value =>
                BoolControlChanged?.Invoke(MachineInterfaceControlIds.Apc.Environment, value);
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not ApcInterfaceViewModel model)
            {
                return;
            }

            if (_window != null)
            {
                _window.Title = model.Title;
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

            _statusBanner.SetContent(model.StatusHeadline, badgeText, model.StatusExplanation, tone);
            _powerFlow.GridInputKw = model.GridInputKw;
            _powerFlow.LoadOutputKw = model.LoadOutputKw;
            _batteryBar.Value = model.BatteryCharge;
            _batteryBar.StateText = model.BatteryStateText;
            _batteryBar.EtaText = model.BatteryEtaText;

            _lightingChannel.LoadKw = model.LightingLoadKw;
            _lightingChannel.IsOn = model.LightingOn;
            _equipmentChannel.LoadKw = model.EquipmentLoadKw;
            _equipmentChannel.IsOn = model.EquipmentOn;
            _environmentChannel.LoadKw = model.EnvironmentLoadKw;
            _environmentChannel.IsOn = model.EnvironmentOn;

            _diagnosticsList.SetLines(model.Diagnostics);
        }

        public void Disconnect()
        {
            if (_window != null)
            {
                _window.CloseClicked -= HandleWindowCloseClicked;
            }
        }

        private void HandleWindowCloseClicked()
        {
            CloseRequested?.Invoke();
        }
    }
}
