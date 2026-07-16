namespace SS3D.UI.MachineInterface
{
    public struct AirAlarmDeviceSnapshot
    {
        public string Id;

        public string Name;

        public byte Kind;

        public bool Powered;

        public float TargetKpa;

        public bool FilterO2;

        public bool FilterN2;

        public bool FilterCo2;

        public bool FilterPlasma;

        public bool FilterToxins;
    }
}
