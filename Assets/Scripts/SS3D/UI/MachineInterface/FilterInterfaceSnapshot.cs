namespace SS3D.UI.MachineInterface
{
    public struct FilterInterfaceSnapshot
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

        public int FlowLitresPerSecond;

        public float ThroughputLitresPerSecond;

        /// <summary>Bit n selects gas id n.</summary>
        public ulong SelectedGases;

        public GasPanelRow[] Gases;

        public GasLineReading Inlet;

        public float OutletPressureKpa;

        public float FilterOutletPressureKpa;
    }
}
