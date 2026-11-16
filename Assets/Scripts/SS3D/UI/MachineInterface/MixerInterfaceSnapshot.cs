namespace SS3D.UI.MachineInterface
{
    public struct MixerInterfaceSnapshot
    {
        public int MachineObjectId;

        public string InterfaceId;

        public string ModelLabel;

        public string DeviceTitle;

        public string Subtitle;

        public bool PowerOk;

        public bool Powered;

        public bool Connected;

        public byte Scenario;

        public bool AccessGranted;

        public bool AccessScanning;

        public bool AccessDenied;

        public int Inlet1Percent;

        public int TargetOutletPressureKpa;

        public bool Inlet1Dry;

        public bool Inlet2Dry;

        public GasPanelRow[] Gases;

        public GasLineReading Inlet1;

        public GasLineReading Inlet2;

        public GasLineReading Outlet;
    }
}
