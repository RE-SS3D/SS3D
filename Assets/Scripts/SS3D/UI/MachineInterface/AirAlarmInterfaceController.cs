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

        private readonly List<AtmosAreaPortRecord> _discoveredPorts = new();

        private AirAlarmController _airAlarm;

        private string _selectedDeviceId;

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

            if (controlId == MachineInterfaceControlIds.Atmos.PresetMode)
            {
                if (!AccessGranted)
                {
                    return false;
                }

                if (_airAlarm == null)
                {
                    _airAlarm = GetComponent<AirAlarmController>();
                }

                _airAlarm?.ServerSetPresetMode((AirAlarmPresetMode)value);
                RefreshAllViewers();
                return true;
            }

            if (controlId == MachineInterfaceControlIds.Atmos.DeviceFilter)
            {
                if (!AccessGranted || !int.TryParse(_selectedDeviceId, out int objectId))
                {
                    return false;
                }

                if (_airAlarm == null)
                {
                    _airAlarm = GetComponent<AirAlarmController>();
                }

                _airAlarm?.ServerToggleScrubberFilter(objectId, value);
                RefreshAllViewers();
                return true;
            }

            AirAlarmInterfaceViewModel scratch = BuildScratchModel();
            AirAlarmInterfaceInteractionLogic.ApplyAction(scratch, controlId, value);
            _selectedDeviceId = scratch.SelectedDeviceId;
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

        private AirAlarmInterfaceViewModel BuildScratchModel()
        {
            AirAlarmInterfaceViewModel model = AirAlarmInterfaceViewModel.CreateNormal();
            model.AccessGranted = AccessGranted;
            model.AccessScanning = AccessScanning;
            model.AccessDenied = AccessDenied;
            model.ActiveMode = _airAlarm != null ? _airAlarm.ActiveMode : AirAlarmPresetMode.Filtering;
            model.SelectedDeviceId = _selectedDeviceId;
            PopulateConnectedDevices(model.ConnectedDevices);
            return model;
        }

        private void ApplyScratchModel(AirAlarmInterfaceViewModel model)
        {
            if (_airAlarm == null)
            {
                _airAlarm = GetComponent<AirAlarmController>();
            }

            _selectedDeviceId = model.SelectedDeviceId;

            AirAlarmConnectedDevice selected = GetSelectedDevice(model);
            if (selected == null || !int.TryParse(selected.Id, out int objectId))
            {
                return;
            }

            _airAlarm?.ServerSetPortEnabled(objectId, selected.Powered);

            if (selected.Kind == AirAlarmConnectedDeviceKind.Vent)
            {
                _airAlarm?.ServerSetVentTargetPressure(objectId, selected.TargetKpa);
            }
            else if (selected.Kind == AirAlarmConnectedDeviceKind.Scrubber
                && AtmosAreaDeviceQuery.TryResolvePort(objectId, out AtmosPortControllerBase controller, out _)
                && controller is ScrubberController scrubber
                && selected.Filters != null)
            {
                scrubber.ServerSetFilters(
                    selected.Filters.TryGetValue("O2", out bool o2) && o2,
                    selected.Filters.TryGetValue("N2", out bool n2) && n2,
                    selected.Filters.TryGetValue("CO2", out bool co2) && co2,
                    selected.Filters.TryGetValue("Plasma", out bool plasma) && plasma,
                    selected.Filters.TryGetValue("Toxins", out bool toxins) && toxins);
            }
        }

        private void PopulateConnectedDevices(List<AirAlarmConnectedDevice> target)
        {
            target.Clear();
            if (_airAlarm == null)
            {
                _airAlarm = GetComponent<AirAlarmController>();
            }

            if (_airAlarm == null || !_airAlarm.HasArea)
            {
                return;
            }

            AtmosAreaDeviceQuery.TryCollectAreaPorts(_airAlarm.AreaId, _discoveredPorts);
            foreach (AtmosAreaPortRecord record in _discoveredPorts)
            {
                target.Add(AirAlarmAreaDeviceMapper.ToConnectedDevice(record));
            }
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
            else if ((alarmState & AirAlarmState.HighPlasma) != 0)
            {
                scenario = AirAlarmScenario.Danger;
            }
            else if ((alarmState & AirAlarmState.LowOxygen) != 0
                || (alarmState & AirAlarmState.HighCarbonDioxide) != 0
                || (alarmState & AirAlarmState.HighTemperature) != 0)
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

            List<AirAlarmDeviceSnapshot> connectedDevices = new();
            PopulateConnectedDevicesFromWorld(connectedDevices);

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
                AccessDenied = AccessDenied,
                PressureKpa = _airAlarm != null ? _airAlarm.SamplePressureKpa : 0f,
                OxygenFraction = _airAlarm != null ? _airAlarm.SampleOxygenFraction : 0f,
                CarbonDioxideFraction = _airAlarm != null ? _airAlarm.SampleCarbonDioxideFraction : 0f,
                NitrogenFraction = _airAlarm != null ? _airAlarm.SampleNitrogenFraction : 0f,
                PlasmaFraction = _airAlarm != null ? _airAlarm.SamplePlasmaFraction : 0f,
                TemperatureKelvin = _airAlarm != null ? _airAlarm.SampleTemperatureKelvin : 0f,
                HasSample = _airAlarm != null && _airAlarm.HasSample,
                ActiveMode = (byte)(_airAlarm != null ? _airAlarm.ActiveMode : AirAlarmPresetMode.Filtering),
                SelectedDeviceId = _selectedDeviceId ?? string.Empty,
                ConnectedDevices = connectedDevices,
            };
        }

        private void PopulateConnectedDevicesFromWorld(List<AirAlarmDeviceSnapshot> target)
        {
            target.Clear();
            if (_airAlarm == null || !_airAlarm.HasArea)
            {
                return;
            }

            AtmosAreaDeviceQuery.TryCollectAreaPorts(_airAlarm.AreaId, _discoveredPorts);
            foreach (AtmosAreaPortRecord record in _discoveredPorts)
            {
                target.Add(AirAlarmInterfaceSnapshotSerializer.ToDeviceSnapshot(
                    AirAlarmAreaDeviceMapper.ToConnectedDevice(record)));
            }
        }

        private static AirAlarmConnectedDevice GetSelectedDevice(AirAlarmInterfaceViewModel model)
        {
            if (string.IsNullOrEmpty(model.SelectedDeviceId))
            {
                return null;
            }

            foreach (AirAlarmConnectedDevice device in model.ConnectedDevices)
            {
                if (device.Id == model.SelectedDeviceId)
                {
                    return device;
                }
            }

            return null;
        }
    }
}
