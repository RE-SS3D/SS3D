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
        private readonly DiagnosticsList _diagnostics;
        private readonly DeviceFooter _footer;

        public GasPumpGaugeBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement contentRoot = _shell ?? root;

            _connectionRow = contentRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = contentRoot.Q<DeviceIdentityBlock>("identity");
            _diagnostics = contentRoot.Q<DiagnosticsList>("diagnostics");
            _footer = contentRoot.Q<DeviceFooter>("footer");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
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
                _shell.PowerOk = model.PowerOk;
            }

            if (_connectionRow != null)
            {
                _connectionRow.StatusText = model.StatusReadout;
                _connectionRow.ReadoutText = model.FlowReadout;
                _connectionRow.DotTone = model.StatusTone;
            }

            if (_identity != null)
            {
                _identity.Title = model.Title;
                _identity.Subtitle = model.Connected ? "Pipe network linked" : "No pipe connection";
            }

            if (_diagnostics != null)
            {
                _diagnostics.SetLines(new DiagnosticLine[]
                {
                    new DiagnosticLine(">", $"Flow {model.FlowReadout}", model.StatusTone),
                    new DiagnosticLine(">", $"ΔP {model.DifferentialReadout}", model.Stalled ? StatusTone.Warning : StatusTone.Info),
                    new DiagnosticLine(">", model.Enabled ? "Pump enabled" : "Pump disabled", model.Enabled ? StatusTone.Info : StatusTone.Info),
                });
            }

            if (_footer != null)
            {
                _footer.Text = "SS3D Gas Pump — ATP-1 differential gauge";
            }
        }

        public void Disconnect()
        {
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
