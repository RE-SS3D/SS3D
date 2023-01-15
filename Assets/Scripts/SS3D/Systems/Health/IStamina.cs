namespace SS3D.Systems.Health
{
    public interface IStamina
    {
        /// <summary>
        /// Gets the current stamina as a proportion of max stamina. Will return value between 0f and 1f.
        /// </summary>
        public float Current { get; }

        /// <summary>
        /// Whether the entity has stamina available to commence an new interaction.
        /// </summary>
        public bool CanCommenceInteraction { get; }

        /// <summary>
        /// Whether the entity has stamina available to continue an interaction they have already started.
        /// </summary>
        public bool CanContinueInteraction { get; }

        /// <summary>
        /// Reduces the current stamina by a set amount.
        /// </summary>
        /// <param name="amount">The amount of stamina to reduce by. Must be a positive number.</param>
        public void ConsumeStamina(float amount);

        /// <summary>
        /// Recharges stamina over time.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void RechargeStamina(float deltaTime);
    }
}