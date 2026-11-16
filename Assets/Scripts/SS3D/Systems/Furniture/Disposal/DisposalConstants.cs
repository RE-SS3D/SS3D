namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Placeholder tuning values. Exact transit speed/throughput is explicitly a balancing pass, not a
    /// design decision (design doc §12) — these just need to be non-zero and roughly plausible for v1.
    /// </summary>
    public static class DisposalConstants
    {
        /// <summary>World units per second a capsule travels along the pipe network.</summary>
        public const float TransitSpeed = 3f;
    }
}
