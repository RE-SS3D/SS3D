using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using System;
using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Client-side coordinator for machine interface panels.
    /// </summary>
    public class MachineInterfaceSubSystem : SubSystem
    {
        public event Action<string, bool> ChannelToggled;

        public event Action InterfaceClosed;

        private ApcInterfaceViewModel _apcModel;
        private SmesInterfaceViewModel _smesModel;
        private string _openInterfaceId;
        private IMachineInterfaceClientBridge _clientBridge;
        private InputSubSystem _inputSystem;
        private bool _inputBlocked;

        public bool IsOpen => !string.IsNullOrEmpty(_openInterfaceId);

        public void Open(string interfaceId, ApcInterfaceViewModel viewModel)
        {
            MachineInterfaceHost host = GetHost();
            if (host == null || !host.Open(interfaceId, viewModel))
            {
                return;
            }

            _openInterfaceId = interfaceId;
            _apcModel = viewModel;
            _smesModel = null;
            SetGameplayInputBlocked(true);
        }

        public void Open(string interfaceId, SmesInterfaceViewModel viewModel)
        {
            MachineInterfaceHost host = GetHost();
            if (host == null || !host.Open(interfaceId, viewModel))
            {
                return;
            }

            _openInterfaceId = interfaceId;
            _smesModel = viewModel;
            _apcModel = null;
            SetGameplayInputBlocked(true);
        }

        public void Refresh(ApcInterfaceViewModel viewModel)
        {
            _apcModel = viewModel;

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Refresh(viewModel);
            }
        }

        public void Refresh(SmesInterfaceViewModel viewModel)
        {
            _smesModel = viewModel;

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Refresh(viewModel);
            }
        }

        public void Close()
        {
            _openInterfaceId = null;
            _apcModel = null;
            _smesModel = null;
            _clientBridge = null;

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Close();
            }

            SetGameplayInputBlocked(false);
            InterfaceClosed?.Invoke();
        }

        public void OpenFromNetwork(ApcInterfaceSnapshot snapshot, IMachineInterfaceClientBridge bridge)
        {
            _clientBridge = bridge;
            Open(snapshot.InterfaceId, ApcInterfaceSnapshotMapper.ToViewModel(snapshot));

            if (!IsOpen)
            {
                _clientBridge = null;
            }
        }

        public void OpenFromNetwork(SmesInterfaceSnapshot snapshot, IMachineInterfaceClientBridge bridge)
        {
            _clientBridge = bridge;
            Open(snapshot.InterfaceId, SmesInterfaceSnapshotMapper.ToViewModel(snapshot));

            if (!IsOpen)
            {
                _clientBridge = null;
            }
        }

        public void RefreshFromNetwork(ApcInterfaceSnapshot snapshot)
        {
            if (!IsOpen || _openInterfaceId != snapshot.InterfaceId)
            {
                return;
            }

            Refresh(ApcInterfaceSnapshotMapper.ToViewModel(snapshot));
        }

        public void RefreshFromNetwork(SmesInterfaceSnapshot snapshot)
        {
            if (!IsOpen || _openInterfaceId != snapshot.InterfaceId)
            {
                return;
            }

            Refresh(SmesInterfaceSnapshotMapper.ToViewModel(snapshot));
        }

        public void CloseFromNetwork(IMachineInterfaceClientBridge bridge)
        {
            if (_clientBridge != bridge)
            {
                return;
            }

            Close();
        }

        public void RequestCloseFromUi(string interfaceId)
        {
            if (_openInterfaceId != interfaceId)
            {
                return;
            }

            _clientBridge?.RequestClose();
            Close();
        }

        public void NotifyClosed(string interfaceId)
        {
            if (_openInterfaceId != interfaceId)
            {
                return;
            }

            _clientBridge?.RequestClose();
            _openInterfaceId = null;
            _apcModel = null;
            _smesModel = null;
            _clientBridge = null;

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Close();
            }

            SetGameplayInputBlocked(false);
            InterfaceClosed?.Invoke();
        }

        public void NotifyChannelToggled(string channelId, bool isOn)
        {
            if (_apcModel == null)
            {
                return;
            }

            switch (channelId)
            {
                case "lighting":
                {
                    _apcModel.LightingOn = isOn;
                    _clientBridge?.SetControl(0, isOn);
                    break;
                }

                case "equipment":
                {
                    _apcModel.EquipmentOn = isOn;
                    _clientBridge?.SetControl(1, isOn);
                    break;
                }

                case "environment":
                {
                    _apcModel.EnvironmentOn = isOn;
                    _clientBridge?.SetControl(2, isOn);
                    break;
                }
            }

            ChannelToggled?.Invoke(channelId, isOn);
        }

        public void NotifySmesControlToggled(byte controlId, bool isOn)
        {
            if (_smesModel == null)
            {
                return;
            }

            switch (controlId)
            {
                case 0:
                {
                    _smesModel.InputEnabled = isOn;
                    break;
                }

                case 1:
                {
                    _smesModel.OutputEnabled = isOn;
                    break;
                }
            }

            _clientBridge?.SetControl(controlId, isOn);
        }

        public void NotifySmesRateDelta(byte controlId, float delta)
        {
            if (_smesModel == null)
            {
                return;
            }

            switch (controlId)
            {
                case 0:
                {
                    _smesModel.InputMaxKw = Math.Max(1f, _smesModel.InputMaxKw + delta);
                    break;
                }

                case 1:
                {
                    _smesModel.OutputMaxKw = Math.Max(1f, _smesModel.OutputMaxKw + delta);
                    break;
                }
            }

            _clientBridge?.SetNumericControl(controlId, delta);
        }

        public void SimulateApcState(ApcPowerState state)
        {
            ApcInterfaceViewModel model = state switch
            {
                ApcPowerState.Overload => ApcInterfaceViewModel.CreateOverload(),
                ApcPowerState.Critical => ApcInterfaceViewModel.CreateCritical(),
                _ => ApcInterfaceViewModel.CreateNominal(),
            };

            if (IsOpen && _openInterfaceId == MachineInterfaceHost.ApcInterfaceId)
            {
                Refresh(model);
            }
            else
            {
                Open(MachineInterfaceHost.ApcInterfaceId, model);
            }
        }

        public void SimulateSmesState(SmesPowerState state)
        {
            SmesInterfaceViewModel model = state switch
            {
                SmesPowerState.Degraded => SmesInterfaceViewModel.CreateDegraded(),
                SmesPowerState.Overload => SmesInterfaceViewModel.CreateOverload(),
                SmesPowerState.Fault => SmesInterfaceViewModel.CreateFault(),
                _ => SmesInterfaceViewModel.CreateNominal(),
            };

            if (IsOpen && _openInterfaceId == MachineInterfaceHost.SmesInterfaceId)
            {
                Refresh(model);
            }
            else
            {
                Open(MachineInterfaceHost.SmesInterfaceId, model);
            }
        }

        protected override void OnDestroyed()
        {
            SetGameplayInputBlocked(false);
            base.OnDestroyed();
        }

        protected override void OnDisabled()
        {
            SetGameplayInputBlocked(false);
            base.OnDisabled();
        }

        private static MachineInterfaceHost GetHost()
        {
            List<MachineInterfaceHost> hosts = ViewLocator.Get<MachineInterfaceHost>();
            return hosts is { Count: > 0 } ? hosts[0] : null;
        }

        private void SetGameplayInputBlocked(bool blocked)
        {
            if (!SubSystems.TryGet(out _inputSystem))
            {
                return;
            }

            if (blocked && !_inputBlocked)
            {
                _inputSystem.ToggleActionMap(_inputSystem.Inputs.Movement, false);
                _inputSystem.ToggleActionMap(_inputSystem.Inputs.Camera, false);
                _inputBlocked = true;
            }
            else if (!blocked && _inputBlocked)
            {
                _inputSystem.ToggleActionMap(_inputSystem.Inputs.Movement, true);
                _inputSystem.ToggleActionMap(_inputSystem.Inputs.Camera, true);
                _inputBlocked = false;
            }
        }
    }
}
