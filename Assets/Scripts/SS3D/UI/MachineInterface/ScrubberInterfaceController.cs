using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Atmospherics.Pipes;
using System.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Networked scrubber machine interface. UI-only controls until atmos plumbing is wired.
    /// </summary>
    [RequireComponent(typeof(ScrubberController))]
    public sealed class ScrubberInterfaceController : AtmosMachineInterfaceBehaviour
    {
        [SerializeField]
        private string _title = "SCRUBBER · ATMOSPHERICS";

        [SerializeField]
        private string _modelLabel = "SCB-09 · atmospheric scrubber unit";

        [SerializeField]
        private string _deviceTitle = "SCB-09";

        [SerializeField]
        private string _subtitle = "Engineering Bay 3 — Atmospheric Scrubber";

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        [SyncVar(OnChange = nameof(OnFlowRateChanged))]
        private int _flowRate = 5;

        private ScrubberController _scrubber;

        public override string InterfaceId => MachineInterfaceIds.Scrubber;

        public override void OnStartServer()
        {
            _scrubber = GetComponent<ScrubberController>();
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

            if (_scrubber == null)
            {
                _scrubber = GetComponent<ScrubberController>();
            }

            if (_scrubber == null || _scrubber.GetState() == value)
            {
                return _scrubber != null;
            }

            _scrubber.Toggle();
            RefreshAllViewers();
            return true;
        }

        protected override bool ApplyNumericControl(byte controlId, float delta)
        {
            if (controlId != MachineInterfaceControlIds.Atmos.FlowRate || !AccessGranted)
            {
                return false;
            }

            _flowRate = Mathf.Clamp(_flowRate + Mathf.RoundToInt(delta), 1, 10);
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
        private void TargetOpenInterface(NetworkConnection conn, ScrubberInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, ScrubberInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(snapshot);
        }

        private ScrubberInterfaceSnapshot BuildSnapshot()
        {
            if (_scrubber == null)
            {
                _scrubber = GetComponent<ScrubberController>();
            }

            bool powerOk = _powerConsumer == null || _powerConsumer.PowerStatus == PowerStatus.Powered;
            bool powered = _scrubber != null && _scrubber.IsEnabled;
            bool connected = _scrubber != null && _scrubber.TryGetConnectedNetwork(out _);
            bool flowing = _scrubber != null && _scrubber.IsPortFlowing;

            ScrubberScenario scenario;
            if (!powerOk)
            {
                scenario = ScrubberScenario.Fault;
            }
            else if (!powered)
            {
                scenario = ScrubberScenario.Idle;
            }
            else if (flowing)
            {
                scenario = ScrubberScenario.Filtering;
            }
            else
            {
                scenario = ScrubberScenario.Idle;
            }

            return new ScrubberInterfaceSnapshot
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
                FlowRate = _flowRate,
            };
        }

        private void OnFlowRateChanged(int _, int __, bool asServer)
        {
            if (asServer)
            {
                RefreshAllViewers();
            }
        }
    }
}
