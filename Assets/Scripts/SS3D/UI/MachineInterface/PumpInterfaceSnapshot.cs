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

        /// <summary>A <see cref="SS3D.Systems.Atmospherics.Pipes.PumpRegulation"/>: which set point applies, if any.</summary>
        public byte Regulation;

        public int TargetOutletPressureKpa;

        public int TargetLitresPerSecond;

        public float InletPressureKpa;

        public float OutletPressureKpa;

        public float FlowMolesPerSecond;
    }
}
