namespace SS3D.Systems.Health
{
    public static class HealthConstants
    {
        /// <summary>
        /// Max amount of damages taken upon each attempt at consuming oxygen if none is present in reserve.
        /// </summary>
        public const float DamageWithNoOxygen = 5f;

        /// <summary>
        /// All in the name. Average quantity of oxygen needed for one millileters of human cells.
        /// </summary>
        public const double MilliMolesPerCentilitersOfOxygen = 1.15e-6;

        /// <summary>
        /// max amount in millimole of blood lost at each heart beat
        /// </summary>
        public const float MaxBloodLost = 2000f;

        /// <summary>
        /// Multiply it with the needed amount of oxygen to know which quantity of oxygen is safe to have.
        /// </summary>
        public const float SafeOxygenFactor = 1.2f;

        /// <summary>
        /// Average ratio between the volume of blood and the volume of body parts in a human.
        /// </summary>
        public const float BloodVolumeToHumanVolumeRatio = 0.08f;
    }
}
