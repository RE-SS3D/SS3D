using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Pressure-differential flow helpers for atmos port devices (design §7).
    /// </summary>
    public static class AtmosPortFlow
    {
        public static float ComputeFlowMoles(
            float sourcePressureKpa,
            float destinationPressureKpa,
            float ratedMaxFlowMolesPerSecond,
            float maxDifferentialKpa,
            float deltaTime)
        {
            if (deltaTime <= 0f || ratedMaxFlowMolesPerSecond <= 0f || maxDifferentialKpa <= 0f)
                return 0f;

            float differential = sourcePressureKpa - destinationPressureKpa;
            if (differential <= 0f)
                return 0f;

            float factor = Mathf.Clamp01(differential / maxDifferentialKpa);
            return ratedMaxFlowMolesPerSecond * deltaTime * factor;
        }
    }
}
