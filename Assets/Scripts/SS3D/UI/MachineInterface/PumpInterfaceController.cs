using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Electricity;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Machine interface for inline pipe devices: pumps, and digital valves with the set point
    /// hidden. Not area-linked; they work on the two networks at their ports.
    /// </summary>
    public sealed class PumpInterfaceController : AtmosMachineInterfaceBehaviour
    {
        [SerializeField]
        private string _title = "PUMP · ATMOSPHERICS";

        [SerializeField]
        private string _modelLabel = "PMP-22 · pipe pump unit";

        [SerializeField]
        private string _deviceTitle = "PMP-22";

        [SerializeField]
        [Tooltip("Device kind shown after the room name, e.g. \"Medbay — Pipe Pump\".")]
        [FormerlySerializedAs("_subtitle")]
        private string _deviceKind = "Pipe Pump";

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        private AtmosInlineDeviceBase _device;

        private AtmosInlineDeviceBase Device => _device != null ? _device : _device = GetComponent<AtmosInlineDeviceBase>();

        private AtmosPumpController Pump => Device as AtmosPumpController;

        public override string InterfaceId => MachineInterfaceIds.Pump;

        public override void OnStartServer()
        {
            if (_powerConsumer == null)
            {
                TryGetComponent(out _powerConsumer);
            }

            base.OnStartServer();
        }

        protected override bool ApplyControl(byte controlId, bool value)
        {
            if (controlId != MachineInterfaceControlIds.Atmos.Power || !AccessGranted)
            {
                return false;
            }

            if (Device == null || Device.GetState() == value)
            {
                return Device != null;
            }

            Device.Toggle();
            RefreshAllViewers();
            return true;
        }

        protected override bool ApplyNumericControl(byte controlId, float delta)
        {
            if (controlId != MachineInterfaceControlIds.Atmos.TargetPressure || !AccessGranted)
            {
                return false;
            }

            if (Pump == null)
            {
                return false;
            }

            int step = Mathf.RoundToInt(delta * 100f);
            if (Pump.Regulation == PumpRegulation.Volume)
            {
                Pump.ServerSetTargetLitresPerSecond(Pump.TargetLitresPerSecond + step);
            }
            else
            {
                Pump.ServerSetTargetOutletPressureKpa(Pump.TargetOutletPressureKpa + step);
            }

            RefreshAllViewers();
            return true;
        }

        protected override void SendOpenToViewer(NetworkConnection conn)
        {
            TargetOpenInterface(conn, BuildSnapshot());
        }

        protected override void SendRefreshToViewer(NetworkConnection conn)
        {
            TargetRefreshInterface(conn, BuildSnapshot());
        }

        [TargetRpc(RunLocally = true)]
        private void TargetOpenInterface(NetworkConnection conn, PumpInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(conn, snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, PumpInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(conn, snapshot);
        }

        private PumpInterfaceSnapshot BuildSnapshot()
        {
            bool powerOk = PowerGate.IsPowered(_powerConsumer, NullConsumerPolicy.Allow);
            bool powered = Device != null && Device.IsEnabled;
            bool connected = Device != null && Device.TryGetConnectedNetwork(out _);
            bool flowing = Device != null && Device.IsPortFlowing;
            bool stalled = Device != null && Device.LastStalled;
            PumpRegulation regulation = Device != null ? Device.Regulation : PumpRegulation.None;
            float inletPressure = connected ? Device.LastInletPressureKpa : 0f;
            float outletPressure = connected ? Device.LastOutletPressureKpa : 0f;
            int targetPressure = regulation == PumpRegulation.OutletPressure ? Pump.TargetOutletPressureKpa : 0;
            int targetLitres = regulation == PumpRegulation.Volume ? Pump.TargetLitresPerSecond : 0;
            float flowMoles = Device != null ? Device.LastFlowMolesPerSecond : 0f;

            PumpScenario scenario;
            if (!powerOk)
            {
                scenario = PumpScenario.Fault;
            }
            else if (!powered)
            {
                scenario = PumpScenario.Idle;
            }
            else if (targetPressure > 0 && outletPressure > targetPressure)
            {
                scenario = PumpScenario.Fault;
            }
            else if (stalled || (Pump != null && connected && powered && inletPressure < 20f && !flowing))
            {
                scenario = PumpScenario.Starved;
            }
            else if (flowing)
            {
                scenario = PumpScenario.Pumping;
            }
            else
            {
                scenario = PumpScenario.Idle;
            }

            return new PumpInterfaceSnapshot
            {
                MachineObjectId = NetworkObject != null ? NetworkObject.ObjectId : 0,
                InterfaceId = InterfaceId,
                Title = _title,
                ModelLabel = _modelLabel,
                DeviceTitle = _deviceTitle,
                Subtitle = MachineAreaLabel.ComposeForDevice(this, _deviceKind),
                PowerOk = powerOk,
                Powered = powered,
                Connected = connected,
                Scenario = (byte)scenario,
                AccessGranted = AccessGranted,
                AccessScanning = AccessScanning,
                AccessDenied = AccessDenied,
                Regulation = (byte)regulation,
                TargetOutletPressureKpa = targetPressure,
                TargetLitresPerSecond = targetLitres,
                InletPressureKpa = inletPressure,
                OutletPressureKpa = outletPressure,
                FlowMolesPerSecond = flowMoles,
            };
        }
    }
}
