using SS3D.UI.MachineInterface.Components;
using System;
using UnityEngine.UIElements;

namespace SS3D.UI.MachineInterface.Bindings
{
    public class VendingMachineBinder : IMachineInterfaceBinder
    {
        public event Action CloseRequested;

        public event Action<byte, bool> BoolControlChanged;

        public event Action<byte, float> NumericControlChanged;

        public event Action<byte, int> ActionControlChanged;

        private readonly DiegeticDeviceShell _shell;
        private readonly ConnectionStatusRow _connectionRow;
        private readonly DeviceIdentityBlock _identity;
        private readonly AtmosIdReaderRow _idReader;
        private readonly ProductGrid _productGrid;
        private readonly DispenseTray _dispenseTray;
        private readonly ActionLog _actionLog;
        private readonly DeviceFooter _footer;

        public VendingMachineBinder(VisualElement root)
        {
            _shell = root.Q<DiegeticDeviceShell>("device-shell") ?? root.Q<DiegeticDeviceShell>();
            VisualElement contentRoot = _shell ?? root;

            _connectionRow = contentRoot.Q<ConnectionStatusRow>("connection-row");
            _identity = contentRoot.Q<DeviceIdentityBlock>("identity");
            _idReader = contentRoot.Q<AtmosIdReaderRow>("id-reader");
            _productGrid = contentRoot.Q<ProductGrid>("product-grid");
            _dispenseTray = contentRoot.Q<DispenseTray>("dispense-tray");
            _actionLog = contentRoot.Q<ActionLog>("action-log");
            _footer = contentRoot.Q<DeviceFooter>("footer");

            if (_shell != null)
            {
                _shell.CloseClicked += HandleCloseRequested;
            }

            if (_productGrid != null)
            {
                _productGrid.ProductSelected += index =>
                    ActionControlChanged?.Invoke(MachineInterfaceControlIds.Vending.SelectProduct, index);
            }

            if (_dispenseTray != null)
            {
                _dispenseTray.TakeRequested += index =>
                    ActionControlChanged?.Invoke(MachineInterfaceControlIds.Vending.TakeTrayItem, index);
            }

            if (_idReader != null)
            {
                _idReader.SetIdleSubline("Read a Medical-tier ID to unlock gated items");
                _idReader.SetGrantedSubline("Gated items unlocked for this session");
                _idReader.ReadRequested += () =>
                    ActionControlChanged?.Invoke(MachineInterfaceControlIds.Vending.ReadId, 0);
            }
        }

        public void Bind(IMachineInterfaceViewModel viewModel)
        {
            if (viewModel is not VendingInterfaceViewModel model)
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
                _connectionRow.StatusText = model.ConnectionStatus;
                _connectionRow.ReadoutText = model.StockedReadout;
                _connectionRow.DotTone = model.PowerOk ? StatusTone.Info : StatusTone.Danger;
            }

            if (_identity != null)
            {
                _identity.Title = model.Title;
                _identity.Subtitle = model.Subtitle;
            }

            if (_idReader != null)
            {
                _idReader.SetAccessState(model.IdScanning, model.IdScanned, denied: false);
            }

            if (_productGrid != null)
            {
                _productGrid.SetProducts(model.Products, model.VendingProductIndex);
            }

            if (_dispenseTray != null)
            {
                _dispenseTray.SetTrayItems(model.TrayItems);
            }

            if (_actionLog != null)
            {
                _actionLog.SetEntries(model.ActionLog);
            }

            if (_footer != null)
            {
                _footer.Text = "SS3D Vending Dispenser — Model VND-7";
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
