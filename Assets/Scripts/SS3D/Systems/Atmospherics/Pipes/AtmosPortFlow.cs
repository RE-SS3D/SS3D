using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Pressure-differential flow helpers for atmos port devices (design §7).
    /// </summary>
    public static class AtmosPortFlow
    {
        private const float LitresPerCubicMetre = 1000f;

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

        /// <summary>
        /// Pump flow from backpressure (design §7): rated flow while the inlet is at or above the
        /// outlet, tapering to zero at <paramref name="maxDifferentialKpa"/>, where the motor stalls.
        /// </summary>
        public static float ComputePumpFlowMoles(
            float sourcePressureKpa,
            float destinationPressureKpa,
            float ratedMaxFlowMolesPerSecond,
            float maxDifferentialKpa,
            float deltaTime,
            out float differentialKpa,
            out bool stalled)
        {
            differentialKpa = sourcePressureKpa - destinationPressureKpa;
            stalled = false;

            if (deltaTime <= 0f || ratedMaxFlowMolesPerSecond <= 0f || maxDifferentialKpa <= 0f)
            {
                differentialKpa = 0f;
                return 0f;
            }

            float backpressure = Mathf.Max(0f, -differentialKpa);
            if (backpressure >= maxDifferentialKpa)
            {
                stalled = true;
                return 0f;
            }

            return ratedMaxFlowMolesPerSecond * deltaTime * (1f - backpressure / maxDifferentialKpa);
        }

        /// <summary>
        /// Moles that take a pump's outlet to its target and no further, so rated flow can't
        /// overshoot it by a whole tick's worth on a short run.
        /// </summary>
        public static float ComputeMolesToTarget(
            float outletPressureKpa,
            float targetPressureKpa,
            float outletTemperature,
            float outletVolume) =>
            AtmosPipeThermo.GetMolesForPressure(targetPressureKpa - outletPressureKpa, outletTemperature, outletVolume);

        /// <summary>
        /// Moles in the share of an inlet a volume pump draws this tick, whatever the outlet holds.
        /// </summary>
        public static float ComputeVolumeMoles(
            float inletMoles,
            float inletVolume,
            float litresPerSecond,
            float deltaTime)
        {
            if (inletVolume <= 0f || litresPerSecond <= 0f || deltaTime <= 0f)
                return 0f;

            float drawn = litresPerSecond / LitresPerCubicMetre * deltaTime;
            return inletMoles * Mathf.Min(1f, drawn / inletVolume);
        }

        /// <summary>
        /// Moles an open valve moves this tick from the higher-pressure side, closing
        /// <see cref="AtmosPortConstants.ValveEqualiseFraction"/> of the gap to where the two
        /// networks would level out. Stepping only part of the way stops them trading places.
        /// </summary>
        public static float ComputeEqualiseMoles(
            float highPressureKpa,
            float highTemperature,
            float highVolume,
            float lowPressureKpa,
            float lowTemperature,
            float lowVolume)
        {
            float molesPerKpaHigh = AtmosPipeThermo.GetMolesForPressure(1f, highTemperature, highVolume);
            float molesPerKpaLow = AtmosPipeThermo.GetMolesForPressure(1f, lowTemperature, lowVolume);
            if (molesPerKpaHigh <= 0f || molesPerKpaLow <= 0f)
                return 0f;

            float molesToLevel = (highPressureKpa - lowPressureKpa)
                * molesPerKpaHigh * molesPerKpaLow / (molesPerKpaHigh + molesPerKpaLow);
            return Mathf.Max(0f, molesToLevel) * AtmosPortConstants.ValveEqualiseFraction;
        }
    }
}
