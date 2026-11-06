using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Electricity;
using UnityEngine;
using UnityEngine.Serialization;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Filter panel: power, flow rate and a toggle per gas in the registry.
    /// </summary>
    [RequireComponent(typeof(AtmosFilterController))]
    public sealed class FilterInterfaceController : AtmosMachineInterfaceBehaviour
    {
        private const int FlowStepLitres = 5;

        [SerializeField]
        private string _modelLabel = "FLT-04 · gas filter unit";

        [SerializeField]
        private string _deviceTitle = "FLT-04";

        [SerializeField]
        [Tooltip("Device kind shown after the room name, e.g. \"Medbay — Gas Filter\".")]
        [FormerlySerializedAs("_subtitle")]
        private string _deviceKind = "Gas Filter";

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        private AtmosFilterController _filter;

        private AtmosFilterController Filter => _filter != null ? _filter : _filter = GetComponent<AtmosFilterController>();

        public override string InterfaceId => MachineInterfaceIds.Filter;

        protected override bool ApplyControl(byte controlId, bool value)
        {
            if (controlId != MachineInterfaceControlIds.Atmos.Power || !AccessGranted || Filter == null)
            {
                return false;
            }

            if (Filter.GetState() != value)
            {
                Filter.Toggle();
                RefreshAllViewers();
            }

            return true;
        }

        protected override bool ApplyNumericControl(byte controlId, float delta)
        {
            if (controlId != MachineInterfaceControlIds.Atmos.FlowRate || !AccessGranted || Filter == null)
            {
                return false;
            }

            Filter.ServerSetFlowLitresPerSecond(Filter.FlowLitresPerSecond + Mathf.RoundToInt(delta) * FlowStepLitres);
            RefreshAllViewers();
            return true;
        }

        protected override bool ApplyActionControl(byte controlId, int value)
        {
            if (controlId == MachineInterfaceControlIds.Atmos.ReadId)
            {
                return base.ApplyActionControl(controlId, value);
            }

            if (controlId != MachineInterfaceControlIds.Atmos.DeviceFilter
                || !AccessGranted
                || Filter == null
                || value < 0
                || value >= AtmosConstants.MaxGasTypes)
            {
                return false;
            }

            Filter.ServerToggleGas(new GasId((ushort)value));
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
        private void TargetOpenInterface(NetworkConnection conn, FilterInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(conn, snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, FilterInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(conn, snapshot);
        }

        private FilterInterfaceSnapshot BuildSnapshot()
        {
            bool powerOk = PowerGate.IsPowered(_powerConsumer, NullConsumerPolicy.Allow);
            bool powered = Filter != null && Filter.IsEnabled;
            bool connected = Filter != null && Filter.TryGetConnectedNetwork(out _);
            bool flowing = Filter != null && Filter.IsPortFlowing;
            GasPanelRow[] gases = GasPanelReadings.BuildRows();

            GasPipeNetworkRecord inlet = null;
            GasPipeNetworkRecord outlet = null;
            GasPipeNetworkRecord side = null;
            if (Filter != null)
            {
                Filter.TryGetPorts(out inlet, out outlet, out side);
            }

            FilterScenario scenario = FilterScenario.Idle;
            if (powerOk && powered && connected)
            {
                scenario = Filter.LastStalled ? FilterScenario.Stalled
                    : flowing ? FilterScenario.Filtering
                    : FilterScenario.Idle;
            }

            return new FilterInterfaceSnapshot
            {
                MachineObjectId = NetworkObject != null ? NetworkObject.ObjectId : 0,
                InterfaceId = InterfaceId,
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
                FlowLitresPerSecond = Filter != null ? Filter.FlowLitresPerSecond : 0,
                ThroughputLitresPerSecond = flowing ? Filter.FlowLitresPerSecond : 0f,
                SelectedGases = Filter != null ? Filter.SelectedGases : 0UL,
                Gases = gases,
                Inlet = GasPanelReadings.Read(inlet, gases),
                OutletPressureKpa = outlet != null ? outlet.GetPressure(AtmosConstants.DefaultGasCount) : 0f,
                FilterOutletPressureKpa = side != null ? side.GetPressure(AtmosConstants.DefaultGasCount) : 0f,
            };
        }
    }
}
