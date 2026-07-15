using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Networked scrubber machine interface.
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

            if (_scrubber == null)
            {
                _scrubber = GetComponent<ScrubberController>();
            }

            int current = _scrubber != null ? _scrubber.FlowRate : 5;
            int next = Mathf.Clamp(current + Mathf.RoundToInt(delta), 1, 10);
            _scrubber?.ServerSetFlowRate(next);
            RefreshAllViewers();
            return true;
        }

        protected override bool ApplyActionControl(byte controlId, int value)
        {
            if (controlId == MachineInterfaceControlIds.Atmos.ReadId)
            {
                return base.ApplyActionControl(controlId, value);
            }

            if (controlId != MachineInterfaceControlIds.Atmos.DeviceFilter || !AccessGranted)
            {
                return false;
            }

            if (_scrubber == null)
            {
                _scrubber = GetComponent<ScrubberController>();
            }

            _scrubber?.ServerToggleFilter(value);
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

            bool powerOk = PowerGate.IsPowered(_powerConsumer, NullConsumerPolicy.Allow);
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

            bool filterO2 = true;
            bool filterN2 = true;
            bool filterCo2 = true;
            bool filterPlasma = false;
            bool filterToxins = true;
            if (_scrubber != null)
            {
                _scrubber.GetFilterStates(
                    out filterO2,
                    out filterN2,
                    out filterCo2,
                    out filterPlasma,
                    out filterToxins);
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
                AccessDenied = AccessDenied,
                FlowRate = _scrubber != null ? _scrubber.FlowRate : 5,
                FilterO2 = filterO2,
                FilterN2 = filterN2,
                FilterCo2 = filterCo2,
                FilterPlasma = filterPlasma,
                FilterToxins = filterToxins,
            };
        }
    }
}
