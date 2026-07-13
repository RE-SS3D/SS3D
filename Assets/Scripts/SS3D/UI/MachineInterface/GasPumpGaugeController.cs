using FishNet.Connection;
using FishNet.Object;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Atmospherics.Pipes;
using System.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Diegetic flow gauge for <see cref="AtmosPumpController"/> (lives in this assembly to avoid Systems↔UI circular refs).
    /// </summary>
    [RequireComponent(typeof(AtmosPumpController))]
    public sealed class GasPumpGaugeController : MachineInterfaceBehaviour
    {
        [SerializeField]
        private string _title = "GAS PUMP · FLOW MONITOR";

        [SerializeField]
        private string _modelLabel = "ATP-1 · differential transfer pump";

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        private AtmosPumpController _pump;

        public override string InterfaceId => MachineInterfaceIds.GasPump;

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            return new IInteraction[] { new OpenMachineInterfaceInteraction() };
        }

        public override void OnStartServer()
        {
            _pump = GetComponent<AtmosPumpController>();
            if (_powerConsumer == null)
            {
                TryGetComponent(out _powerConsumer);
            }

            base.OnStartServer();
        }

        protected override bool ApplyControl(byte controlId, bool value) => false;

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

            bool powered = _powerConsumer == null || _powerConsumer.PowerStatus == PowerStatus.Powered;
            bool connected = _pump != null && _pump.TryGetConnectedNetwork(out _);
            bool flowing = _pump != null && _pump.LastFlowMolesPerSecond > 0f;
            bool stalled = _pump != null && _pump.LastStalled;

            byte health = 0;
            if (!powered)
            {
                health = 3;
            }
            else if (stalled)
            {
                health = 2;
            }
            else if (flowing)
            {
                health = 1;
            }

            return new GasPumpInterfaceSnapshot
            {
                MachineObjectId = NetworkObject != null ? NetworkObject.ObjectId : 0,
                InterfaceId = InterfaceId,
                Title = _title,
                ModelLabel = _modelLabel,
                PowerOk = powered,
                Enabled = _pump == null || _pump.IsEnabled,
                Connected = connected,
                RatedMaxFlowMolesPerSecond = _pump != null ? _pump.RatedMaxFlowMolesPerSecond : 0f,
                CurrentFlowMolesPerSecond = _pump != null ? _pump.LastFlowMolesPerSecond : 0f,
                DifferentialKpa = _pump != null ? _pump.LastDifferentialKpa : 0f,
                MaxDifferentialKpa = _pump != null ? _pump.MaxDifferentialKpa : 0f,
                Stalled = stalled,
                HealthState = health,
            };
        }
    }
}
