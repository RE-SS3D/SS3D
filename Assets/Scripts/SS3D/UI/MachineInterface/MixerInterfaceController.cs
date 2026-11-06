using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Mixer panel: power, target outlet pressure and the inlet ratio.
    /// </summary>
    [RequireComponent(typeof(AtmosMixerController))]
    public sealed class MixerInterfaceController : AtmosMachineInterfaceBehaviour
    {
        private const int RatioStepPercent = 5;
        private const int TargetStepKpa = 100;

        [SerializeField]
        private string _modelLabel = "MIX-11 · gas mixer unit";

        [SerializeField]
        private string _deviceTitle = "MIX-11";

        [SerializeField]
        private string _subtitle = "Atmospherics Distro — Gas Mixer";

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        private AtmosMixerController _mixer;

        private AtmosMixerController Mixer => _mixer != null ? _mixer : _mixer = GetComponent<AtmosMixerController>();

        public override string InterfaceId => MachineInterfaceIds.Mixer;

        protected override bool ApplyControl(byte controlId, bool value)
        {
            if (controlId != MachineInterfaceControlIds.Atmos.Power || !AccessGranted || Mixer == null)
            {
                return false;
            }

            if (Mixer.GetState() != value)
            {
                Mixer.Toggle();
                RefreshAllViewers();
            }

            return true;
        }

        protected override bool ApplyNumericControl(byte controlId, float delta)
        {
            if (!AccessGranted || Mixer == null)
            {
                return false;
            }

            int steps = Mathf.RoundToInt(delta);
            if (controlId == MachineInterfaceControlIds.Atmos.TargetPressure)
            {
                Mixer.ServerSetTargetOutletPressureKpa(Mixer.TargetOutletPressureKpa + steps * TargetStepKpa);
            }
            else if (controlId == MachineInterfaceControlIds.Atmos.InletRatio)
            {
                Mixer.ServerSetInlet1Percent(Mixer.Inlet1Percent + steps * RatioStepPercent);
            }
            else
            {
                return false;
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
        private void TargetOpenInterface(NetworkConnection conn, MixerInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(conn, snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, MixerInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(conn, snapshot);
        }

        private MixerInterfaceSnapshot BuildSnapshot()
        {
            bool powerOk = PowerGate.IsPowered(_powerConsumer, NullConsumerPolicy.Allow);
            bool powered = Mixer != null && Mixer.IsEnabled;
            bool connected = Mixer != null && Mixer.TryGetConnectedNetwork(out _);
            GasPanelRow[] gases = GasPanelReadings.BuildRows();

            GasPipeNetworkRecord inlet1 = null;
            GasPipeNetworkRecord outlet = null;
            GasPipeNetworkRecord inlet2 = null;
            if (Mixer != null)
            {
                Mixer.TryGetPorts(out inlet1, out outlet, out inlet2);
            }

            MixerScenario scenario = MixerScenario.Idle;
            if (powerOk && powered && connected)
            {
                scenario = Mixer.LastStalled ? MixerScenario.Stopped
                    : Mixer.IsPortFlowing ? MixerScenario.Mixing
                    : MixerScenario.Idle;
            }

            return new MixerInterfaceSnapshot
            {
                MachineObjectId = NetworkObject != null ? NetworkObject.ObjectId : 0,
                InterfaceId = InterfaceId,
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
                Inlet1Percent = Mixer != null ? Mixer.Inlet1Percent : 0,
                TargetOutletPressureKpa = Mixer != null ? Mixer.TargetOutletPressureKpa : 0,
                Inlet1Dry = Mixer != null && Mixer.LastInlet1Dry,
                Inlet2Dry = Mixer != null && Mixer.LastInlet2Dry,
                Gases = gases,
                Inlet1 = GasPanelReadings.Read(inlet1, gases),
                Inlet2 = GasPanelReadings.Read(inlet2, gases),
                Outlet = GasPanelReadings.Read(outlet, gases),
            };
        }
    }
}
