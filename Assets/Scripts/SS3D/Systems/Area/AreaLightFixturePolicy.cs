using SS3D.Systems.Electricity;

namespace SS3D.Systems.Area
{
    /// <summary>
    /// Pure policy for whether a fixture should emit light in a given area lighting state.
    /// </summary>
    public static class AreaLightFixturePolicy
    {
        public static bool ShouldEmitLight(
            bool hasArea,
            AreaLightingState areaState,
            LightFixtureCapability capability,
            PowerStatus consumerStatus,
            out bool useEmergencyVisuals)
        {
            useEmergencyVisuals = false;

            if (!hasArea)
            {
                return false;
            }

            switch (areaState)
            {
                case AreaLightingState.Normal:
                    return consumerStatus == PowerStatus.Powered;

                case AreaLightingState.Emergency:
                    if (capability != LightFixtureCapability.EmergencyCapable
                        || consumerStatus != PowerStatus.Powered)
                    {
                        return false;
                    }

                    useEmergencyVisuals = true;
                    return true;

                default:
                    return false;
            }
        }
    }
}
