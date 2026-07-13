namespace SS3D.UI.MachineInterface
{
    public struct AirAlarmInterfaceSnapshot
    {
        public const int MaxConnectedDevices = AirAlarmInterfaceInteractionLogic.MaxConnectedDevices;

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

        public float PressureKpa;

        public float OxygenFraction;

        public float CarbonDioxideFraction;

        public byte ActiveMode;

        public string SelectedDeviceId;

        public byte ConnectedDeviceCount;

        public AirAlarmDeviceSnapshot Device0;

        public AirAlarmDeviceSnapshot Device1;

        public AirAlarmDeviceSnapshot Device2;

        public AirAlarmDeviceSnapshot Device3;
    }
}
