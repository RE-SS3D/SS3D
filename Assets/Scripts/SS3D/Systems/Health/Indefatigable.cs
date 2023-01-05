using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Represents an entity that does not need stamina (robots etc). It will always have
    /// sufficient stamina to perform any task where stamina is required.
    /// </summary>
    /// <inheritdoc cref="SS3D.Systems.Health.IStamina" />
    public class Indefatigable : IStamina
    {

        /// <inheritdoc />
        public float Current
        {
            get => 1f;
        }

        /// <inheritdoc />
        public bool CanCommenceInteraction
        {
            get => true;
        }

        /// <inheritdoc />
        public bool CanContinueInteraction
        {
            get => true;
        }

        /// <inheritdoc />
        public void ConsumeStamina(float amount)
        {
            // No implementation required for Indefatigable.
        }

        /// <inheritdoc />
        public void RechargeStamina(float deltaTime)
        {
            // No implementation required for Indefatigable.
        }
    }
}