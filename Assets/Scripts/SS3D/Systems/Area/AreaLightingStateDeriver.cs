using SS3D.Systems.Electricity;

namespace SS3D.Systems.Area
{
    public static class AreaLightingStateDeriver
    {
        public static AreaLightingState Derive(CircuitStats stats, ApcControlFlags channels, bool lightingSwitchOn = true)
        {
            if (!lightingSwitchOn)
            {
                return AreaLightingState.Dark;
            }

            bool lightingEnabled = (channels & ApcControlFlags.Lighting) != 0;
            if (!lightingEnabled)
            {
                return AreaLightingState.Dark;
            }

            if (stats.GridMeetsLoad)
            {
                return AreaLightingState.Normal;
            }

            if (stats.TotalSupplyKw >= stats.LightingLoadKw)
            {
                return AreaLightingState.Normal;
            }

            if (stats.ApcBatteryCharge > 0f)
            {
                return AreaLightingState.Emergency;
            }

            return AreaLightingState.Dark;
        }

        /// <summary>
        /// Whether the area's machines are running: the Equipment channel is on and the grid covers
        /// the load. A battery only carries the lights, so this goes first when the grid fails.
        /// </summary>
        public static bool DeriveEquipmentPowered(CircuitStats stats, ApcControlFlags channels) =>
            (channels & ApcControlFlags.Equipment) != 0 && stats.TotalSupplyKw > 0f && stats.GridMeetsLoad;
    }
}
