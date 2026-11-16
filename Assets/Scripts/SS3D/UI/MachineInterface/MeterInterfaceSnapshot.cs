namespace SS3D.UI.MachineInterface
{
    public struct MeterInterfaceSnapshot
    {
        public int MachineObjectId;

        public string InterfaceId;

        public string ModelLabel;

        public string DeviceTitle;

        public string Subtitle;

        public string LineLabel;

        public bool Connected;

        /// <summary>Lines running through the meter's tile; above one, the panel offers a switch.</summary>
        public byte LinesOnTile;

        public float TemperatureKelvin;

        public float Moles;

        public float VolumeLitres;

        public GasPanelRow[] Gases;

        public GasLineReading Line;

        /// <summary>Pressure each second, oldest first.</summary>
        public float[] History;
    }
}
