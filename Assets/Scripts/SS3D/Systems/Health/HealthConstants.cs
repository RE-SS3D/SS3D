namespace SS3D.Systems.Health
{
    public static class HealthConstants
    {
        /// <summary>
        /// Max amount of damages taken upon each attempt at consuming oxygen if none is present in reserve. No units.
        /// </summary>
        public const float DamageWithNoOxygen = 25f;

        /// <summary>
        /// Average quantity of oxygen needed for one milliliters of human cells for one second. mmol / ml / s.
        /// </summary>
        public const double MilliMolesOfOxygenPerMillilitersOfBody = 1.15e-6;

        /// <summary>
        /// max amount in millimole of blood lost at each heart beat. mmol
        /// </summary>
        public const float MaxBloodLost = 2000f;

        /// <summary>
        /// Multiply it with the needed amount of oxygen to know which quantity of oxygen is safe to have in the body. No units.
        /// Below, player start breathing with difficulty, or even suffocate.
        /// </summary>
        public const float SafeOxygenFactor = 1.2f;

        /// <summary>
        /// Average ratio between the volume of blood and the volume of body parts in a human. No units.
        /// Determines the volume of blood in the body.
        /// </summary>
        public const float BloodVolumeToHumanVolumeRatio = 0.08f;

        /// <summary>
        /// Healthy ratio of blood volume to blood container volume in the blood container before
        /// the circulatory system struggles sending oxygen to body parts. No units.
        /// Close to one, oxygen will become fastly unavailable. Close to 0, body can function even with little blood.
        /// </summary>
        public const float HealthyBloodVolumeRatio = 0.8f;

        /// <summary>
        /// Change that to change the approximate time it'll take for body parts to take oxy damages
        /// When bodyparts relies uniquely on their reserves. Time might be affected by other factors.
        /// </summary>
        public const float OxygenSecondOfReserveInNormalConditions = 10f;
    }
}
