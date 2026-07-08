namespace SS3D.UI.MachineInterface
{
    public struct SmesInterfaceSnapshot
    {
        public const int MaxWarnings = 4;

        public const int MaxAdvancedMetrics = 6;

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

        public string MaintenanceText;

        public byte MaintenanceTone;

        public int WarningCount;

        public ApcDiagnosticSnapshot Warning0;

        public ApcDiagnosticSnapshot Warning1;

        public ApcDiagnosticSnapshot Warning2;

        public ApcDiagnosticSnapshot Warning3;

        public int AdvancedMetricCount;

        public SmesMetricSnapshot AdvancedMetric0;

        public SmesMetricSnapshot AdvancedMetric1;

        public SmesMetricSnapshot AdvancedMetric2;

        public SmesMetricSnapshot AdvancedMetric3;

        public SmesMetricSnapshot AdvancedMetric4;

        public SmesMetricSnapshot AdvancedMetric5;
    }
}
