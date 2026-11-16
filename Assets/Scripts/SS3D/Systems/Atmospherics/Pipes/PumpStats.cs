namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Pump device ratings per design §7. Actual flow is computed from differential at runtime.
    /// </summary>
    public readonly struct PumpStats
    {
        public readonly float RatedMaxFlowMoles;
        public readonly float MaxDifferentialKpa;

        public PumpStats(float ratedMaxFlowMoles, float maxDifferentialKpa)
        {
            RatedMaxFlowMoles = ratedMaxFlowMoles;
            MaxDifferentialKpa = maxDifferentialKpa;
        }

        public static PumpStats Default => new(100f, 450f);
    }
}
