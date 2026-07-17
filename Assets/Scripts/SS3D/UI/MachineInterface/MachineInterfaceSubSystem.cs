using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using System;
using System.Collections.Generic;
using UnityEngine;
using SS3D.Systems.Electricity;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Client-side coordinator for machine interface panels.
    /// </summary>
    public class MachineInterfaceSubSystem : SubSystem
    {
        public event Action<string, bool> ChannelToggled;

        public event Action InterfaceClosed;

        private IMachineInterfaceViewModel _openModel;
        private string _openInterfaceId;
        private IMachineInterfaceClientBridge _clientBridge;
        private InputSubSystem _inputSystem;
        private bool _inputBlocked;

        public bool IsOpen => !string.IsNullOrEmpty(_openInterfaceId);

        public void Open(string interfaceId, IMachineInterfaceViewModel viewModel)
        {
#if UNITY_SERVER
            // Machine controllers send their "open" RPC as TargetRpc(RunLocally = true), which also runs
            // this on the server that sent it (needed for host mode, where server and client are the same
            // process). A dedicated server has no local player to show UI to, so skip it entirely - it
            // would otherwise try to lay out UI Toolkit text with no shaders available and crash.
            return;
#endif
            MachineInterfaceHost host = GetHost();
            if (host == null || !host.Open(interfaceId, viewModel))
            {
                return;
            }

            _openInterfaceId = interfaceId;
            _openModel = viewModel;
            SetGameplayInputBlocked(true);
        }

        public void Refresh(IMachineInterfaceViewModel viewModel)
        {
            _openModel = viewModel;

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Refresh(viewModel);
            }
        }

        public void Close()
        {
            _openInterfaceId = null;
            _openModel = null;
            _clientBridge = null;

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Close();
            }

            SetGameplayInputBlocked(false);
            InterfaceClosed?.Invoke();
        }

        public void OpenFromNetwork(
            string interfaceId,
            IMachineInterfaceViewModel viewModel,
            IMachineInterfaceClientBridge bridge)
        {
            _clientBridge = bridge;
            Open(interfaceId, viewModel);

            if (!IsOpen)
            {
                _clientBridge = null;
            }
        }

        public void RefreshFromNetwork(string interfaceId, IMachineInterfaceViewModel viewModel)
        {
            if (!IsOpen || _openInterfaceId != interfaceId)
            {
                return;
            }

            Refresh(viewModel);
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
            _openModel = null;
            _clientBridge = null;

            MachineInterfaceHost host = GetHost();
            if (host != null)
            {
                host.Close();
            }

            SetGameplayInputBlocked(false);
            InterfaceClosed?.Invoke();
        }

        public void NotifyBoolControl(byte controlId, bool isOn)
        {
            if (_openModel == null)
            {
                return;
            }

            MachineOptimisticControlRegistry.EnsureRegistered();
            if (MachineOptimisticControlRegistry.TryGet(_openModel.GetType(), out IMachineOptimisticControlHandler handler))
            {
                handler.ApplyBool(_openModel, controlId, isOn, CreateOptimisticCallbacks());
            }

            _clientBridge?.SetControl(controlId, isOn);
            Refresh(_openModel);
        }

        public void NotifyNumericControl(byte controlId, float delta)
        {
            if (_openModel == null)
            {
                return;
            }

            MachineOptimisticControlRegistry.EnsureRegistered();
            if (!MachineOptimisticControlRegistry.TryGet(_openModel.GetType(), out IMachineOptimisticControlHandler handler))
            {
                return;
            }

            handler.ApplyNumeric(_openModel, controlId, delta, CreateOptimisticCallbacks());
            _clientBridge?.SetNumericControl(controlId, delta);
            Refresh(_openModel);
        }

        public void NotifyActionControl(byte controlId, int value)
        {
            if (_openModel == null)
            {
                return;
            }

            MachineOptimisticControlRegistry.EnsureRegistered();
            if (MachineOptimisticControlRegistry.TryGet(_openModel.GetType(), out IMachineOptimisticControlHandler handler))
            {
                handler.ApplyAction(_openModel, controlId, value, CreateOptimisticCallbacks());
            }

            _clientBridge?.SetActionControl(controlId, value);

            if (_openModel is VendingInterfaceViewModel
                or ScrubberInterfaceViewModel
                or VentInterfaceViewModel
                or PumpInterfaceViewModel
                or AirAlarmInterfaceViewModel
                or ApcInterfaceViewModel
                or SmesInterfaceViewModel)
            {
                Refresh(_openModel);
            }
        }

        public void SimulateApcState(ApcPowerState state)
        {
            ApcInterfaceViewModel model = state switch
            {
                ApcPowerState.Overload => ApcInterfaceViewModel.CreateOverload(),
                ApcPowerState.Critical => ApcInterfaceViewModel.CreateCritical(),
                _ => ApcInterfaceViewModel.CreateNominal(),
            };

            if (IsOpen && _openInterfaceId == MachineInterfaceIds.Apc)
            {
                Refresh(model);
            }
            else
            {
                Open(MachineInterfaceIds.Apc, model);
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

            if (IsOpen && _openInterfaceId == MachineInterfaceIds.Smes)
            {
                Refresh(model);
            }
            else
            {
                Open(MachineInterfaceIds.Smes, model);
            }
        }

        public void SimulateVendingState()
        {
            VendingInterfaceViewModel model = VendingInterfaceViewModel.CreateSample();

            if (IsOpen && _openInterfaceId == MachineInterfaceIds.Vending)
            {
                Refresh(model);
            }
            else
            {
                Open(MachineInterfaceIds.Vending, model);
            }
        }

        protected void Update()
        {
            if (!IsOpen || !Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

            RequestCloseFromUi(_openInterfaceId);
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

        private MachineOptimisticControlCallbacks CreateOptimisticCallbacks() =>
            new MachineOptimisticControlCallbacks((channelId, isOn) => ChannelToggled?.Invoke(channelId, isOn));

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
