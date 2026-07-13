namespace SS3D.UI.MachineInterface
{
    public struct ScrubberInterfaceSnapshot
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

        public int FlowRate;

        public bool FilterO2;

        public bool FilterN2;

        public bool FilterCo2;

        public bool FilterPlasma;

        public bool FilterToxins;
    }
}
