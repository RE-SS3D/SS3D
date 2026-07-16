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

        public bool PowerOk;

        public byte Scenario;

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
