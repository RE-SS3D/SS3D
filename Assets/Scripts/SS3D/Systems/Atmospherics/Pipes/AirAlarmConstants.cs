namespace SS3D.Systems.Atmospherics.Pipes
{
    public static class AirAlarmConstants
    {
        /// <summary>Alarm when area-average O₂ mole fraction drops below this (normal air ~0.21).</summary>
        public const float LowOxygenMoleFraction = 0.18f;

        /// <summary>Alarm when area-average CO₂ mole fraction exceeds this.</summary>
        public const float HighCarbonDioxideMoleFraction = 0.01f;

        /// <summary>Alarm when area-average pressure exceeds this (kPa).</summary>
        public const float HighPressureKpa = 150f;

        /// <summary>Alarm when area-average pressure drops below this (kPa).</summary>
        public const float LowPressureKpa = 80f;
    }
}
