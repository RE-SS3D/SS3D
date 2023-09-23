using UnityEngine;

namespace SS3D.Systems.Health.Stamina
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
        private const float AllowableOverdraw = 0.1f;

        /// <summary>
        /// Amount of stamina (multiplied by _max) that must be spent to train stamina, leading to a permanent increase in _max.
        /// </summary>
        private const float TrainingConsumptionRequirement = 2f;

        /// <summary>
        /// How much _max is multiplied by every time it is trained.
        /// in _max.
        /// </summary>
        private const float TrainingMultiplier = 1.05f;

        /// <summary>
        /// Recovery rate of stamina, measured as proportion per second.
        /// </summary>
        private readonly float _recoveryRate;

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
        /// Record of the total amount of stamina spent by the player.
        /// </summary>
        private float _spent;

        /// <summary>
        /// Constructor. Should only be created through the StaminaHelper Create() method.
        /// </summary>
        /// <param name="max"></param>
        /// <param name="recoveryRate"></param>
        public Stamina(float max, float recoveryRate)
        {
            _max = max;
            _current = max;
            _recoveryRate = recoveryRate;
            _spent = 0;
        }

        /// <inheritdoc />
        public float Current => Mathf.Max(_current / _max, 0f);

        /// <inheritdoc />
        public bool CanCommenceInteraction => _current > 0f;

        /// <inheritdoc />
        public bool CanContinueInteraction => _current > -1f * AllowableOverdraw * _max;

        /// <inheritdoc />
        public void ConsumeStamina(float amount)
        {
            // This method cannot be used to restore stamina.
            if (amount < 0f)
            {
                return;
            }

            TrainStamina(amount);

            // Reduce the stamina
            _current -= amount;
        }

        /// <inheritdoc />
        public void RechargeStamina(float deltaTime)
        {
            // Ensure current stamina remains no larger than max stamina
            _current = Mathf.Min(_current + (deltaTime * _max * _recoveryRate), _max);
        }

        /// <summary>
        /// Part of the design was that by using stamina, your max stamina can increase i.e. be trained.
        /// So it records all the stamina used and "levels up" the player whenever they use enough.
        /// TODO: Improve: It's a pretty basic and inflexible implementation, just had to put something in there to meet the PR requirements.
        /// </summary>
        /// <param name="staminaConsumed"></param>
        private void TrainStamina(float staminaConsumed)
        {
            // Add the consumed stamina to our tally. We only record stamina that we have, not any overdraw.
            _spent += Mathf.Max(Mathf.Min(staminaConsumed, _current), 0f);

            bool hasSpentSufficientStamina = _spent > TrainingConsumptionRequirement * _max;

            // If we have spent sufficient stamina, increase our max capacity.
            if (!hasSpentSufficientStamina)
            {
                return;
            }

            // Reset the spend counter
            _spent -= TrainingMultiplier * _max;

            // Increase our max stamina capacity
            _max *= TrainingMultiplier;

            // Scale up our current stamina. This is done to prevent visible stamina gap when training.
            _current *= TrainingMultiplier;
        }
    }
}