namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// What an inline device's panel sets. A valve only opens and closes, so it has no set point.
    /// </summary>
    public enum PumpRegulation : byte
    {
        OutletPressure,
        Volume,
        None,
    }
}
