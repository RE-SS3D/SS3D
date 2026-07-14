using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Atmospherics.Pipes;
using System.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Networked pump unit machine interface. Pumps are not area-linked — they operate on their own tile and pipe network.
    /// </summary>
    [RequireComponent(typeof(AtmosPumpController))]
    public sealed class GasPumpGaugeController : AtmosMachineInterfaceBehaviour
    {
        [SerializeField]
        private string _title = "PUMP · ATMOSPHERICS";

        [SerializeField]
        private string _modelLabel = "PMP-22 · pipe pump unit";

        [SerializeField]
        private string _deviceTitle = "PMP-22";

        [SerializeField]
        private string _subtitle = "Engineering Bay 3 — Pipe Pump";

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        private AtmosPumpController _pump;

        public override string InterfaceId => MachineInterfaceIds.GasPump;

        public override void OnStartServer()
        {
            _pump = GetComponent<AtmosPumpController>();
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

            if (_pump == null)
            {
                _pump = GetComponent<AtmosPumpController>();
            }

            if (_pump == null || _pump.GetState() == value)
            {
                return _pump != null;
            }

            _pump.Toggle();
            RefreshAllViewers();
            return true;
        }

        protected override bool ApplyNumericControl(byte controlId, float delta)
        {
            if (controlId != MachineInterfaceControlIds.Atmos.TargetPressure || !AccessGranted)
            {
                return false;
            }

            if (_pump == null)
            {
                _pump = GetComponent<AtmosPumpController>();
            }

            int next = Mathf.Clamp(
                (_pump != null ? _pump.TargetOutletPressureKpa : 0) + Mathf.RoundToInt(delta * 100f),
                0,
                9000);
            _pump?.ServerSetTargetOutletPressureKpa(next);
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
        private void TargetOpenInterface(NetworkConnection conn, GasPumpInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, GasPumpInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(snapshot);
        }

        private GasPumpInterfaceSnapshot BuildSnapshot()
        {
            if (_pump == null)
            {
                _pump = GetComponent<AtmosPumpController>();
            }

            bool powerOk = _powerConsumer == null || _powerConsumer.PowerStatus == PowerStatus.Powered;
            bool powered = _pump != null && _pump.IsEnabled;
            bool connected = _pump != null && _pump.TryGetConnectedNetwork(out _);
            bool flowing = _pump != null && _pump.IsPortFlowing;
            bool stalled = _pump != null && _pump.LastStalled;
            float inletPressure = _pump != null ? _pump.LastInletPressureKpa : 0f;
            float outletPressure = _pump != null ? _pump.LastOutletPressureKpa : 0f;
            int targetPressure = _pump != null ? _pump.TargetOutletPressureKpa : 0;
            float flowMoles = _pump != null ? _pump.LastFlowMolesPerSecond : 0f;

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
            else if (stalled || (powered && inletPressure < 20f && !flowing))
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

            return new GasPumpInterfaceSnapshot
            {
                MachineObjectId = NetworkObject != null ? NetworkObject.ObjectId : 0,
                InterfaceId = InterfaceId,
                Title = _title,
                ModelLabel = _modelLabel,
                DeviceTitle = _deviceTitle,
                Subtitle = _subtitle,
                PowerOk = powerOk,
                Powered = powered,
                Connected = connected,
                Scenario = (byte)scenario,
                AccessGranted = AccessGranted,
                AccessScanning = AccessScanning,
                TargetOutletPressureKpa = targetPressure,
                InletPressureKpa = inletPressure,
                OutletPressureKpa = outletPressure,
                FlowMolesPerSecond = flowMoles,
            };
        }
    }
}
