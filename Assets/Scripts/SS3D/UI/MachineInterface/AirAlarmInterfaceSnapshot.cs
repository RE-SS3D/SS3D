using System.Collections.Generic;

namespace SS3D.UI.MachineInterface
{
    public struct AirAlarmInterfaceSnapshot
    {
        public int MachineObjectId;

        public string InterfaceId;

        public string Title;

        public string ModelLabel;

        public string DeviceTitle;

        public string Subtitle;

        /// <summary>Display name of the room the alarm watches; empty when unassigned.</summary>
        public string AreaName;

        public bool PowerOk;

        public byte Scenario;

        /// <summary><see cref="SS3D.Systems.Atmospherics.Pipes.AirAlarmState"/> flags, so the panel names what tripped.</summary>
        public byte AlarmState;

        /// <summary><see cref="SS3D.Systems.Atmospherics.Pipes.AirAlarmIndicator"/>: filling, draining or steady.</summary>
        public byte Indicator;

        public bool AccessGranted;

        public bool AccessScanning;

        public bool AccessDenied;

        public float PressureKpa;

        public float OxygenFraction;

        public float CarbonDioxideFraction;

        public float NitrogenFraction;

        public float PlasmaFraction;

        public float TemperatureKelvin;

        public bool HasSample;

        public byte ActiveMode;

        public string SelectedDeviceId;

        public List<AirAlarmDeviceSnapshot> ConnectedDevices;
    }
}
