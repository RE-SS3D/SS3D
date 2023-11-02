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
        public const double MilliMolesOfOxygenPerMillilitersOfBody = 2.77e-6;

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
        public const float BloodVolumeToHumanVolumeRatio = 0.077f;

        /// <summary>
        /// Healthy ratio of blood volume to blood container volume in the blood container before
        /// the circulatory system struggles sending oxygen to body parts. No units.
        /// Close to one, oxygen will become fastly unavailable. Close to 0, body can function even with little blood.
        /// </summary>
        public const float HealthyBloodVolumeRatio = 0.8f;

        /// <summary>
        /// The maximum volume of oxygen which the blood can carry when fully saturated
        /// is termed the oxygen carrying capacity, which,
        /// with a normal haemoglobin concentration, is approximately 20 mL oxygen per 100 mL blood.
        /// in SS3D, "real blood" is composed of "blood" and oxygen, in the amount of 2 part oxygen, 8 part blood.
        /// So oxygen volume is at a maximum one fourth of blood volume.
        /// </summary>
        public const float MaxOxygenToBloodVolumeRatio = 0.25f;

        /// <summary>
        /// This factor gives a rough idea of how much more blood a human can have before it starts having trouble
        /// due to the excess of blood.
        /// </summary>
        public const float HighBloodVolumeToleranceFactor = 1.15f;
    }
}
