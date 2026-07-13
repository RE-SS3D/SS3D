using FishNet.Connection;
using SS3D.Systems.Atmospherics.Pipes;
using System.Electricity;
using UnityEngine;

namespace SS3D.UI.MachineInterface
{
    /// <summary>
    /// Networked air alarm machine interface. Uses live alarm samples when available.
    /// </summary>
    [RequireComponent(typeof(AirAlarmController))]
    public sealed class AirAlarmInterfaceController : AtmosMachineInterfaceBehaviour
    {
        [SerializeField]
        private string _title = "AIR ALARM · ATMOSPHERICS";

        [SerializeField]
        private string _modelLabel = "AA-07 · atmospheric alarm control unit";

        [SerializeField]
        private string _deviceTitle = "AA-07";

        [SerializeField]
        private string _subtitle = "Engineering Bay 3 — Air Alarm";

        [SerializeField]
        private BasicPowerConsumer _powerConsumer;

        private AirAlarmController _airAlarm;

        public override string InterfaceId => MachineInterfaceIds.AirAlarm;

        public override void OnStartServer()
        {
            _airAlarm = GetComponent<AirAlarmController>();
            if (_powerConsumer == null)
            {
                TryGetComponent(out _powerConsumer);
            }

            base.OnStartServer();
        }

        protected override void SendOpenToViewer(NetworkConnection conn)
        {
            TargetOpenInterface(conn, BuildSnapshot());
        }

        protected override void SendRefreshToViewer(NetworkConnection conn)
        {
            TargetRefreshInterface(conn, BuildSnapshot());
        }

        protected override bool ApplyControl(byte controlId, bool value) => false;

        [TargetRpc(RunLocally = true)]
        private void TargetOpenInterface(NetworkConnection conn, AirAlarmInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, AirAlarmInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(snapshot);
        }

        private AirAlarmInterfaceSnapshot BuildSnapshot()
        {
            if (_airAlarm == null)
            {
                _airAlarm = GetComponent<AirAlarmController>();
            }

            bool powerOk = _powerConsumer == null || _powerConsumer.PowerStatus == PowerStatus.Powered;
            AirAlarmState alarmState = _airAlarm != null ? _airAlarm.AlarmState : AirAlarmState.None;

            AirAlarmScenario scenario;
            if (!powerOk)
            {
                scenario = AirAlarmScenario.Danger;
            }
            else if ((alarmState & AirAlarmState.LowOxygen) != 0
                || (alarmState & AirAlarmState.HighCarbonDioxide) != 0)
            {
                scenario = AirAlarmScenario.Warning;
            }
            else if ((alarmState & AirAlarmState.HighPressure) != 0
                || (alarmState & AirAlarmState.LowPressure) != 0)
            {
                scenario = AirAlarmScenario.Warning;
            }
            else
            {
                scenario = AirAlarmScenario.Normal;
            }

            return new AirAlarmInterfaceSnapshot
            {
                MachineObjectId = NetworkObject != null ? NetworkObject.ObjectId : 0,
                InterfaceId = InterfaceId,
                Title = _title,
                ModelLabel = _modelLabel,
                DeviceTitle = _deviceTitle,
                Subtitle = _subtitle,
                PowerOk = powerOk,
                Scenario = (byte)scenario,
                AccessGranted = AccessGranted,
                AccessScanning = AccessScanning,
                PressureKpa = _airAlarm != null ? _airAlarm.SamplePressureKpa : 0f,
                OxygenFraction = _airAlarm != null ? _airAlarm.SampleOxygenFraction : 0f,
                CarbonDioxideFraction = _airAlarm != null ? _airAlarm.SampleCarbonDioxideFraction : 0f,
            };
        }
    }
}
