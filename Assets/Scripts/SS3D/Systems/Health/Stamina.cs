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
        /// Amount of stamina (multiplied by _max) that must be spent to train stamina, leading to a permanent increase in _max.
        /// </summary>
        private const float TRAINING_CONSUMPTION_REQUIREMENT = 2f;

        /// <summary>
        /// How much _max is multiplied by every time it is trained.
        /// in _max.
        /// </summary>
        private const float TRAINING_MULTIPLIER = 1.05f;

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

            TrainStamina(amount);

            // Reduce the stamina
            _current -= amount;
        }

        /// <inheritdoc />
        public void RechargeStamina(float deltaTime)
        {
            // Ensure current stamina remains no larger than max stamina
            _current = Mathf.Min(_current + deltaTime * _max * _recoveryRate, _max);
        }

        private void TrainStamina(float staminaConsumed)
        {
            // Add the consumed stamina to our tally. We only record stamina that we have, not any overdraw.
            _spent += Mathf.Max(Mathf.Min(staminaConsumed, _current), 0f);

            // If we have spent sufficient stamina, increase our max capacity.
            if (_spent > TRAINING_CONSUMPTION_REQUIREMENT * _max)
            {
                // Reset the spend counter
                _spent -= TRAINING_MULTIPLIER * _max;

                // Increase our max stamina capacity
                _max *= TRAINING_MULTIPLIER;

                // Scale up our current stamina. This is done to prevent visible stamina gap when training.
                _current *= TRAINING_MULTIPLIER;
            }
        }
    }
}