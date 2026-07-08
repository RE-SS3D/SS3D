using SS3D.Core;
using SS3D.Core.Behaviours;
using System;
using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Client-side coordinator for machine interface panels. Phase 1 is local-only;
    /// Phase 2 will extend this to NetworkSubSystem with TargetRpc snapshot sync.
    /// </summary>
    public class MachineInterfaceSubSystem : SubSystem
    {
        public event Action<string, bool> ChannelToggled;

        public event Action InterfaceClosed;

        private ApcInterfaceViewModel _currentModel;
        private string _openInterfaceId;

        public bool IsOpen => !string.IsNullOrEmpty(_openInterfaceId);

        public void Open(string interfaceId, ApcInterfaceViewModel viewModel)
        {
            _openInterfaceId = interfaceId;
            _currentModel = viewModel;

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Open(interfaceId, viewModel);
            }
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

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Close();
            }

            InterfaceClosed?.Invoke();
        }

        public void NotifyClosed(string interfaceId)
        {
            if (_openInterfaceId == interfaceId)
            {
                _openInterfaceId = null;
                _currentModel = null;
                InterfaceClosed?.Invoke();
            }
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
                    break;
                }

                case "equipment":
                {
                    _currentModel.EquipmentOn = isOn;
                    break;
                }

                case "environment":
                {
                    _currentModel.EnvironmentOn = isOn;
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

        private static MachineInterfaceHost GetHost()
        {
            List<MachineInterfaceHost> hosts = ViewLocator.Get<MachineInterfaceHost>();
            return hosts is { Count: > 0 } ? hosts[0] : null;
        }
    }
}
