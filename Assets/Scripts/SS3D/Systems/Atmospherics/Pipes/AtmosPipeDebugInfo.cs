namespace SS3D.Systems.Atmospherics.Pipes
{
    public struct AtmosPipeDebugInfo
    {
        public bool Exists;
        public GasPipeSegmentKey Segment;
        public GasPipeNetworkId NetworkId;
        public int SegmentCount;
        public float PressureKpa;
        public float Temperature;
        public float Volume;
        public float TotalMoles;
    }
}
