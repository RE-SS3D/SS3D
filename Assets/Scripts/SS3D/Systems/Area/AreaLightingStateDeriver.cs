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
    }
}
