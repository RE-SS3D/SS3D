using System;
using SS3D.Systems.Atmospherics;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Synced turf exposure snapshot for alert / screen mappers on all peers.
    /// Breathability is derived from O₂ partial pressure (1 at ≥18 kPa PO₂; station air ~20).
    /// </summary>
    [Serializable]
    public struct HealthEnvironmentState
    {
        public bool HasSample;
        public bool IsVacuum;
        public float AtmosphereBreathability;
        public float TemperatureKelvin;
        public float PressureKpa;
        public float BurnIntensity;
        public float PlasmaMoleFraction;
        public float OxygenMoleFraction;

        /// <summary>A worn seal holds, so pressure and cold are not reaching the body.</summary>
        public bool IsSealed;

        /// <summary>Share of heat and fire stopped by the seal; 0 when unsealed.</summary>
        public float HeatProtection;

        /// <summary>A worn suit or helmet is punctured, so the seal cannot hold until it is replaced.</summary>
        public bool IsBreached;

        /// <summary>Assumed safe station air when atmos/tile is unavailable (lobby, map load).</summary>
        public static HealthEnvironmentState SafeDefault => new()
        {
            HasSample = false,
            IsVacuum = false,
            AtmosphereBreathability = 1f,
            TemperatureKelvin = AtmosConstants.StandardTemperature,
            PressureKpa = AtmosConstants.StandardPressure,
            BurnIntensity = 0f,
            PlasmaMoleFraction = 0f,
            OxygenMoleFraction = StationOxygenMoleFraction,
        };

        public static float StationOxygenMoleFraction =>
            AtmosConstants.StationOxygenMoles
            / (AtmosConstants.StationOxygenMoles + AtmosConstants.StationNitrogenMoles);
    }
}
