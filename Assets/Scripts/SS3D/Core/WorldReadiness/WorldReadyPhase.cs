namespace SS3D.Core.WorldReadiness
{
    /// <summary>
    /// Ordered world-simulation readiness phases. Domain gates are independent bits;
    /// <see cref="WorldReady"/> means all five domain gates are set.
    /// </summary>
    public enum WorldReadyPhase
    {
        None = 0,
        TileMapLoaded = 1,
        AreasFlooded = 2,
        ElectricityReady = 3,
        AtmosReady = 4,
        DisposalReady = 5,
        WorldReady = 6,
    }
}
