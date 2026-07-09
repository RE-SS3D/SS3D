namespace SS3D.UI.MachineInterface
{
    public struct SmesInterfaceSnapshot
    {
        public const int MaxWarnings = 4;

        public int MachineObjectId;

        public string InterfaceId;

        public string Title;

        public byte PowerState;

        public float ChargePct;

        public byte ChargeTrend;

        public float InputCurrentKw;

        public float OutputCurrentKw;

        public float InputMaxKw;

        public float OutputMaxKw;

        public bool InputEnabled;

        public bool OutputEnabled;

        public bool InputActive;

        public bool OutputActive;

        public string ConnectionStateText;

        public string DiagnosisHint;

        public int WarningCount;

        public ApcDiagnosticSnapshot Warning0;

        public ApcDiagnosticSnapshot Warning1;

        public ApcDiagnosticSnapshot Warning2;

        public ApcDiagnosticSnapshot Warning3;
    }
}
