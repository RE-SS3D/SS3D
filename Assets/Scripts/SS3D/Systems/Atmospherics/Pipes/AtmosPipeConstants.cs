namespace SS3D.Systems.Atmospherics.Pipes
{
    public static class AtmosPipeConstants
    {
        /// <summary>Well-mixed volume per pipe segment tile (m³).</summary>
        public const float SegmentVolume = 0.5f;

        /// <summary>Pressure a storage chamber's gas source holds its cell at (kPa).</summary>
        public const float ChamberPressureKpa = 500f;

        /// <summary>Pressure a line fill gives its network at round start (kPa).</summary>
        public const float LineFillPressureKpa = 300f;
    }
}
