using UnityEngine;

namespace SS3D.Systems.Stamina
{
    /// <summary>
    /// Represents stamina of an individual entity.
    /// </summary>
    /// <inheritdoc cref="SS3D.Systems.Stamina.IStamina" />
    public class Stamina : IStamina
    {
        private float _current;
        private float _max;
        private readonly float _recoveryRate;
        private float _spent;

        private const float AllowableOverdraw = 0.1f;
        private const float TrainingConsumptionRequirement = 2f;
        private const float TrainingMultiplier = 1.05f;

        public float Current => Mathf.Max(_current / _max, 0f);
        public bool CanCommenceInteraction => _current > 0f;
        public bool CanContinueInteraction => _current > -1f * AllowableOverdraw * _max;

        public Stamina(float max, float recoveryRate)
        {
            _max = max;
            _current = max;
            _recoveryRate = recoveryRate;
            _spent = 0;
        }

        public void ConsumeStamina(float amount)
        {
            if (amount < 0f) return;

            TrainStamina(amount);
            _current -= amount;
        }

        public void RechargeStamina(float deltaTime)
        {
            _current = Mathf.Min(_current + deltaTime * _max * _recoveryRate, _max);
        }

        private void TrainStamina(float staminaConsumed)
        {
            _spent += Mathf.Max(Mathf.Min(staminaConsumed, _current), 0f);

            bool hasSpentSufficientStamina = _spent > TrainingConsumptionRequirement * _max;
            if (!hasSpentSufficientStamina)
            {
                return;
            }

            _spent -= TrainingMultiplier * _max;
            _max *= TrainingMultiplier;
            _current *= TrainingMultiplier;
        }
    }
}
