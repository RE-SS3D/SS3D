using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Atmospherics.Pipes;
using System.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Networked vent machine interface.
    /// </summary>
    [RequireComponent(typeof(VentController))]
    public sealed class VentInterfaceController : AtmosMachineInterfaceBehaviour
    {
        [SerializeField]
        private string _title = "VENT · ATMOSPHERICS";

        [SerializeField]
        private string _modelLabel = "VT-14 · atmospheric vent unit";

        [SerializeField]
        private string _deviceTitle = "VT-14";

        [SerializeField]
        private string _subtitle = "Engineering Bay 3 — Atmospheric Vent";

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        private VentController _vent;

        public int TargetPressureKpa => _vent != null ? _vent.TargetPressureKpa : 101;

        public override string InterfaceId => MachineInterfaceIds.Vent;

        public override void OnStartServer()
        {
            _vent = GetComponent<VentController>();
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

            if (_vent == null)
            {
                _vent = GetComponent<VentController>();
            }

            if (_vent == null || _vent.GetState() == value)
            {
                return _vent != null;
            }

            _vent.Toggle();
            RefreshAllViewers();
            return true;
        }

        protected override bool ApplyNumericControl(byte controlId, float delta)
        {
            if (controlId != MachineInterfaceControlIds.Atmos.TargetPressure || !AccessGranted)
            {
                return false;
            }

            if (_vent == null)
            {
                _vent = GetComponent<VentController>();
            }

            int next = Mathf.Clamp(TargetPressureKpa + Mathf.RoundToInt(delta), 0, 200);
            _vent?.ServerSetTargetPressureKpa(next);
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
        private void TargetOpenInterface(NetworkConnection conn, VentInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, VentInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(snapshot);
        }

        private VentInterfaceSnapshot BuildSnapshot()
        {
            if (_vent == null)
            {
                _vent = GetComponent<VentController>();
            }

            bool powerOk = _powerConsumer == null || _powerConsumer.PowerStatus == PowerStatus.Powered;
            bool powered = _vent != null && _vent.IsEnabled;
            bool connected = _vent != null && _vent.TryGetConnectedNetwork(out _);
            bool flowing = _vent != null && _vent.IsPortFlowing;

            VentScenario scenario;
            if (!powerOk)
            {
                scenario = VentScenario.Fault;
            }
            else if (!powered)
            {
                scenario = VentScenario.Idle;
            }
            else if (flowing)
            {
                scenario = VentScenario.Pressurizing;
            }
            else
            {
                scenario = VentScenario.Idle;
            }

            return new VentInterfaceSnapshot
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
                AccessDenied = AccessDenied,
                TargetPressureKpa = TargetPressureKpa,
            };
        }
    }
}
