using System.Electricity;

namespace SS3D.UI.MachineInterface
{
    public struct ApcInterfaceSnapshot
    {
        public const int MaxDiagnostics = 6;

        public int MachineObjectId;

        public string InterfaceId;

        public string Title;

        public ApcControlFlags Channels;

        public byte PowerState;

        public float GridInputKw;

        public float LoadOutputKw;

        public float BatteryCharge;

        public byte BatteryState;

        public float LightingLoadKw;

        public float EquipmentLoadKw;

        public float EnvironmentLoadKw;

        public bool MultipleApcsInArea;

        public int DiagnosticCount;

        public ApcDiagnosticSnapshot Diagnostic0;

        public ApcDiagnosticSnapshot Diagnostic1;

        public ApcDiagnosticSnapshot Diagnostic2;

        public ApcDiagnosticSnapshot Diagnostic3;

        public ApcDiagnosticSnapshot Diagnostic4;

        public ApcDiagnosticSnapshot Diagnostic5;
    }
}
