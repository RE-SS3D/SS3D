namespace SS3D.Systems.Atmospherics
{
    /// <summary>
    /// Most commonly used gasses. Use a float4 struct for SIMD optimization.
    /// </summary>
    public enum CoreAtmosGasses
    {
        Oxygen = 0,
        Nitrogen = 1,
        CarbonDioxide = 2,
        Plasma = 3,
    }
}
