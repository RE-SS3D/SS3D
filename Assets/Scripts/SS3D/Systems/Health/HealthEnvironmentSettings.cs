namespace SS3D.Systems.Health
{
    /// <summary>
    /// Process-wide toggles for atmosphere → health coupling. Default off for the damage disable
    /// flag so live play always takes turf exposure unless a debug console / Health Debug opts out.
    /// </summary>
    public static class HealthEnvironmentSettings
    {
        /// <summary>
        /// When true: skip turf-driven oxy shortfall, toxin intake, breath exchange, env burn damage,
        /// and atmos alert/screen mapping (combat testing without vacuum/heat killing you).
        /// </summary>
        public static bool AtmosphericDamageDisabled { get; set; }
    }
}
