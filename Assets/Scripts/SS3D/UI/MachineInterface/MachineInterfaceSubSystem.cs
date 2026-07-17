using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Inputs;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using SS3D.Systems.Electricity;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Client-side coordinator for machine interface panels.
    /// </summary>
    public class MachineInterfaceSubSystem : SubSystem
    {
        public event Action<string, bool> ChannelToggled;

        /// <summary>Fired after a panel successfully opens (Main HUD listens to suppress chrome).</summary>
        public event Action InterfaceOpened;

        /// <summary>Fired after close animation + teardown complete.</summary>
        public event Action InterfaceClosed;

        private IMachineInterfaceViewModel _openModel;
        private string _openInterfaceId;
        private IMachineInterfaceClientBridge _clientBridge;
        private InputSubSystem _inputSystem;
        private IInputHandle _machineHandle;
        private bool _isClosing;

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
            // Host.Open tears down any in-flight close without invoking its callback.
            _isClosing = false;

            MachineInterfaceHost host = GetHost();
            if (host == null || !host.Open(interfaceId, viewModel))
            {
                return;
            }

            _openInterfaceId = interfaceId;
            _openModel = viewModel;
            SetGameplayInputBlocked(true);
            InterfaceOpened?.Invoke();
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
            if (_isClosing)
            {
                return;
            }

            MachineInterfaceHost host = GetHost();
            if (host == null || !host.IsOpen)
            {
                FinishClose();
                return;
            }

            _isClosing = true;
            // Keep _openInterfaceId until teardown so IsOpen stays true (HUD suppress, cancel).
            host.Close(FinishClose);
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
            Close();
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

        protected override void OnDestroyed()
        {
            MachineInterfaceHost host = GetHost();
            host?.CloseImmediate();
            FinishClose();
            base.OnDestroyed();
        }

        protected override void OnDisabled()
        {
            SetGameplayInputBlocked(false);
            base.OnDisabled();
        }

        private void FinishClose()
        {
            _isClosing = false;
            _openInterfaceId = null;
            _openModel = null;
            _clientBridge = null;
            SetGameplayInputBlocked(false);
            InterfaceClosed?.Invoke();
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

            if (blocked && _machineHandle == null)
            {
                _machineHandle = _inputSystem.PushContext(InputContext.MachineUI);
                _inputSystem.UiCancel.performed += HandleUiCancel;
            }
            else if (!blocked && _machineHandle != null)
            {
                _inputSystem.UiCancel.performed -= HandleUiCancel;
                _machineHandle.Dispose();
                _machineHandle = null;
            }
        }

        private void HandleUiCancel(InputAction.CallbackContext context)
        {
            if (IsOpen && !_isClosing)
            {
                RequestCloseFromUi(_openInterfaceId);
            }
        }
    }
}
