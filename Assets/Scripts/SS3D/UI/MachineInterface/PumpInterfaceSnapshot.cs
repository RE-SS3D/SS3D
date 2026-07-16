namespace SS3D.UI.MachineInterface
{
    public struct PumpInterfaceSnapshot
    {
        public int MachineObjectId;

        public string InterfaceId;

        public string Title;

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

        public int TargetOutletPressureKpa;

        public float InletPressureKpa;

        public float OutletPressureKpa;

        public float FlowMolesPerSecond;
    }
}
