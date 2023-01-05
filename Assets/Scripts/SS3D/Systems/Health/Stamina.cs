using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Represents stamina of an individual entity.
    /// </summary>
    /// <inheritdoc cref="SS3D.Systems.Health.IStamina" />
    public class Stamina : IStamina
    {
        /// <summary>
        /// Allows stamina to be overdrawn for existing interactions; however, new interactions requiring stamina cannot
        /// be started while overdrawn. This prevents rapid transition between walk and run animations (for example).
        /// </summary>
        private const float ALLOWABLE_OVERDRAW = 0.1f;

        /// <summary>
        /// The current stamina of the entity. This can vary between a lower limit of negative ALLOWABLE_OVERDRAW times _max,
        /// and an upper limit of _max.
        /// </summary>
        private float _current;

        /// <summary>
        /// The maximum stamina of the entity. 
        /// </summary>
        private float _max;

        /// <summary>
        /// Recovery rate of stamina, measured as proportion per second.
        /// </summary>
        private float _recoveryRate;

        /// <summary>
        /// Record of the total amount of stamina spent by the player.
        /// </summary>
        private float _spent;

        /// <summary>
        /// Constructor. Should only be created through the StaminaHelper Create() method.
        /// </summary>
        /// <param name="max"></param>
        /// <param name="recoveryRate"></param>
        public Stamina (float max, float recoveryRate)
        {
            _max = max;
            _current = max;
            _recoveryRate = recoveryRate;
            _spent = 0;
        }

        /// <inheritdoc />
        public float Current
        {
            get => Mathf.Max(_current / _max, 0f);
        }

        /// <inheritdoc />
        public bool CanCommenceInteraction
        {
            get => _current > 0f;
        }

        /// <inheritdoc />
        public bool CanContinueInteraction
        {
            get => _current > -1f * ALLOWABLE_OVERDRAW * _max;
        }

        /// <inheritdoc />
        public void ConsumeStamina(float amount)
        {
            // This method cannot be used to restore stamina.
            if (amount < 0f) return;

            // Reduce the stamina
            _current -= amount;

            // Add the consumed stamina to our tally
            _spent += amount;
        }

        /// <inheritdoc />
        public void RechargeStamina(float deltaTime)
        {
            // Ensure current stamina remains no larger than max stamina
            _current = Mathf.Min(_current + deltaTime * _max * _recoveryRate, _max);
        }
    }
}