namespace SS3D.Systems.Stamina
{
    public interface IStamina
    {
        /// <summary>
        /// Current stamina as a proportion of max (0..1).
        /// </summary>
        float Current { get; }

        /// <summary>
        /// Whether a new interaction may start. Design allows acting at zero stamina.
        /// </summary>
        bool CanCommenceInteraction { get; }

        /// <summary>
        /// Whether an in-progress interaction may continue. Design allows acting at zero stamina.
        /// </summary>
        bool CanContinueInteraction { get; }

        /// <summary>0 when rested, 1 when fully exhausted — movement/performance degradation.</summary>
        float ExertionPenalty { get; }

        /// <summary>Absolute stamina remaining (not normalized).</summary>
        float CurrentAbsolute { get; }

        float Max { get; }

        /// <summary>Overdraw amount from the last <see cref="ConsumeStamina"/> call.</summary>
        float LastOverdraw { get; }

        void ConsumeStamina(float amount);

        void RechargeStamina(float deltaTime);

        void ApplyModifiers(float maxScale, float regenScale);

        void SetBaseRecoveryRate(float recoveryRate);

        void SetBaseMax(float max);
    }
}
