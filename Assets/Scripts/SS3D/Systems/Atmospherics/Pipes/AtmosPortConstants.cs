namespace SS3D.Systems.Atmospherics.Pipes
{
    public static class AtmosPortConstants
    {
        /// <summary>Rated vent throughput under ideal differential (moles/s total mixture).</summary>
        public const float VentRatedFlowMolesPerSecond = 50f;

        /// <summary>Rated scrubber throughput for filtered gases (moles/s).</summary>
        public const float ScrubberRatedFlowMolesPerSecond = 40f;

        /// <summary>Backpressure differential at which port flow tapers to zero (kPa).</summary>
        public const float PortMaxDifferentialKpa = 101.325f;

        /// <summary>Rated gas pump throughput under ideal differential (moles/s total mixture).</summary>
        public const float PumpRatedFlowMolesPerSecond = 100f;

        /// <summary>Motor stall threshold — no flow above this differential (kPa).</summary>
        public const float PumpMaxDifferentialKpa = 450f;
    }
}
