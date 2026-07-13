using FishNet.Connection;
using FishNet.Object;
using SS3D.Systems.Atmospherics.Pipes;
using System.Collections.Generic;
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

        private readonly List<AirAlarmConnectedDevice> _connectedDevices = new();

        private AirAlarmController _airAlarm;

        private byte _activeMode = (byte)AirAlarmPresetMode.Filtering;

        private string _selectedDeviceId;

        public override string InterfaceId => MachineInterfaceIds.AirAlarm;

        public override void OnStartServer()
        {
            _airAlarm = GetComponent<AirAlarmController>();
            if (_powerConsumer == null)
            {
                TryGetComponent(out _powerConsumer);
            }

            EnsureDefaultDevices();
            base.OnStartServer();
        }

        protected override bool ApplyControl(byte controlId, bool value)
        {
            if (!AccessGranted)
            {
                return false;
            }

            AirAlarmInterfaceViewModel scratch = BuildScratchModel();
            AirAlarmInterfaceInteractionLogic.ApplyBool(scratch, controlId, value);
            ApplyScratchModel(scratch);
            RefreshAllViewers();
            return true;
        }

        protected override bool ApplyNumericControl(byte controlId, float delta)
        {
            if (!AccessGranted)
            {
                return false;
            }

            AirAlarmInterfaceViewModel scratch = BuildScratchModel();
            AirAlarmInterfaceInteractionLogic.ApplyNumeric(scratch, controlId, delta);
            ApplyScratchModel(scratch);
            RefreshAllViewers();
            return true;
        }

        protected override bool ApplyActionControl(byte controlId, int value)
        {
            if (controlId == MachineInterfaceControlIds.Atmos.ReadId)
            {
                if (AccessGranted)
                {
                    _selectedDeviceId = null;
                }

                return base.ApplyActionControl(controlId, value);
            }

            AirAlarmInterfaceViewModel scratch = BuildScratchModel();
            AirAlarmInterfaceInteractionLogic.ApplyAction(scratch, controlId, value);
            ApplyScratchModel(scratch);
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
        private void TargetOpenInterface(NetworkConnection conn, AirAlarmInterfaceSnapshot snapshot)
        {
            DispatchClientOpen(snapshot);
        }

        [TargetRpc(RunLocally = true)]
        private void TargetRefreshInterface(NetworkConnection conn, AirAlarmInterfaceSnapshot snapshot)
        {
            DispatchClientRefresh(snapshot);
        }

        private void EnsureDefaultDevices()
        {
            if (_connectedDevices.Count > 0)
            {
                return;
            }

            foreach (AirAlarmConnectedDevice device in AirAlarmInterfaceViewModel.CreateNormal().ConnectedDevices)
            {
                _connectedDevices.Add(AirAlarmInterfaceInteractionLogic.CloneDevice(device));
            }
        }

        private AirAlarmInterfaceViewModel BuildScratchModel()
        {
            AirAlarmInterfaceViewModel model = AirAlarmInterfaceViewModel.CreateNormal();
            model.ActiveMode = (AirAlarmPresetMode)_activeMode;
            model.SelectedDeviceId = _selectedDeviceId;
            AirAlarmInterfaceInteractionLogic.CopyDevicesToModel(model, _connectedDevices);
            return model;
        }

        private void ApplyScratchModel(AirAlarmInterfaceViewModel model)
        {
            _activeMode = (byte)model.ActiveMode;
            _selectedDeviceId = model.SelectedDeviceId;
            _connectedDevices.Clear();

            foreach (AirAlarmConnectedDevice device in model.ConnectedDevices)
            {
                _connectedDevices.Add(AirAlarmInterfaceInteractionLogic.CloneDevice(device));
            }
        }

        private AirAlarmInterfaceSnapshot BuildSnapshot()
        {
            if (_airAlarm == null)
            {
                _airAlarm = GetComponent<AirAlarmController>();
            }

            EnsureDefaultDevices();

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

            AirAlarmInterfaceSnapshot snapshot = new()
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
                ActiveMode = _activeMode,
                SelectedDeviceId = _selectedDeviceId ?? string.Empty,
                ConnectedDeviceCount = (byte)Mathf.Min(_connectedDevices.Count, AirAlarmInterfaceSnapshot.MaxConnectedDevices),
            };

            for (int i = 0; i < snapshot.ConnectedDeviceCount; i++)
            {
                AirAlarmInterfaceSnapshotSerializer.SetDevice(
                    ref snapshot,
                    i,
                    AirAlarmInterfaceSnapshotSerializer.ToDeviceSnapshot(_connectedDevices[i]));
            }

            return snapshot;
        }
    }
}
