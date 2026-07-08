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

        private ApcInterfaceViewModel _currentModel;
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
            _currentModel = viewModel;
            SetGameplayInputBlocked(true);
        }

        public void Refresh(ApcInterfaceViewModel viewModel)
        {
            _currentModel = viewModel;

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Refresh(viewModel);
            }
        }

        public void Close()
        {
            _openInterfaceId = null;
            _currentModel = null;
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

        public void RefreshFromNetwork(ApcInterfaceSnapshot snapshot)
        {
            if (!IsOpen)
            {
                return;
            }

            Refresh(ApcInterfaceSnapshotMapper.ToViewModel(snapshot));
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
            _currentModel = null;
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
            if (_currentModel == null)
            {
                return;
            }

            switch (channelId)
            {
                case "lighting":
                {
                    _currentModel.LightingOn = isOn;
                    _clientBridge?.SetControl(0, isOn);
                    break;
                }

                case "equipment":
                {
                    _currentModel.EquipmentOn = isOn;
                    _clientBridge?.SetControl(1, isOn);
                    break;
                }

                case "environment":
                {
                    _currentModel.EnvironmentOn = isOn;
                    _clientBridge?.SetControl(2, isOn);
                    break;
                }
            }

            ChannelToggled?.Invoke(channelId, isOn);
        }

        public void SimulateState(ApcPowerState state)
        {
            ApcInterfaceViewModel model = state switch
            {
                ApcPowerState.Overload => ApcInterfaceViewModel.CreateOverload(),
                ApcPowerState.Critical => ApcInterfaceViewModel.CreateCritical(),
                _ => ApcInterfaceViewModel.CreateNominal(),
            };

            if (IsOpen)
            {
                Refresh(model);
            }
            else
            {
                Open(MachineInterfaceHost.ApcInterfaceId, model);
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
