using System.Electricity;

namespace SS3D.Systems.Area
{
    public static class AreaLightingStateDeriver
    {
        public static AreaLightingState Derive(CircuitStats stats, ApcControlFlags channels)
        {
            if (stats.GridMeetsLoad)
            {
                return AreaLightingState.Normal;
            }

            bool lightingEnabled = (channels & ApcControlFlags.Lighting) != 0;
            if (lightingEnabled && stats.TotalSupplyKw >= stats.LightingLoadKw)
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
