namespace SS3D.UI.MachineInterface
{
    public struct MeterInterfaceSnapshot
    {
        public int MachineObjectId;

        public string InterfaceId;

        public string ModelLabel;

        public string DeviceTitle;

        public string LineLabel;

        public bool Connected;

        public float TemperatureKelvin;

        public float Moles;

        public float VolumeLitres;

        public GasPanelRow[] Gases;

        public GasLineReading Line;

        /// <summary>Pressure each second, oldest first.</summary>
        public float[] History;
    }
}
