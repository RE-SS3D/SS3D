namespace SS3D.Systems.Comms
{
    /// <summary>
    /// A listener's audibility of a given speech line, per comms.md §3.
    /// </summary>
    public enum AudibilityTier
    {
        /// <summary>Beyond audible range, or blocked and beyond the grace distance, or a sealed barrier. Nothing renders.</summary>
        Inaudible,

        /// <summary>Within extended range unobstructed, or blocked but within the grace distance. Garbled/partial text.</summary>
        Muffled,

        /// <summary>Within clear range and unobstructed. Full text, full opacity.</summary>
        Clear,
    }
}
