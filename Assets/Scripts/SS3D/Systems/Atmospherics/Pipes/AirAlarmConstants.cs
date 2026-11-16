namespace SS3D.Systems.Atmospherics.Pipes
{
    public static class AirAlarmConstants
    {
        /// <summary>Alarm when sampled O₂ mole fraction drops below this (normal air ~0.21).</summary>
        public const float LowOxygenMoleFraction = 0.18f;

        /// <summary>Alarm when sampled CO₂ mole fraction exceeds this.</summary>
        public const float HighCarbonDioxideMoleFraction = 0.01f;

        /// <summary>Alarm when sampled pressure exceeds this (kPa).</summary>
        public const float HighPressureKpa = 150f;

        /// <summary>Alarm when sampled pressure drops below this (kPa).</summary>
        public const float LowPressureKpa = 80f;

        /// <summary>Alarm when sampled plasma mole fraction exceeds this.</summary>
        public const float HighPlasmaMoleFraction = 0.001f;

        /// <summary>Alarm when sampled temperature exceeds this (Kelvin).</summary>
        public const float HighTemperatureKelvin = 323.15f;
    }
}
