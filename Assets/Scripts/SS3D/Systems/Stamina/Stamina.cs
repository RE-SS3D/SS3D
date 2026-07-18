using UnityEngine;

namespace SS3D.Systems.Stamina
{
    /// <summary>
    /// Fast exertion pool per Documents/design/stamina.md — current/max with consume and recharge.
    /// Does not hard-lock actions at zero; overdraw is tracked for oxy-debt bridging.
    /// </summary>
    public class Stamina : IStamina
    {
        private float _current;
        private float _max;
        private float _baseMax;
        private float _baseRecoveryRate;
        private float _recoveryRate;

        /// <inheritdoc />
        public float LastOverdraw { get; private set; }

        public float CurrentAbsolute => Mathf.Max(_current, 0f);

        public float Max => Mathf.Max(_max, 0.01f);

        /// <inheritdoc />
        public float Current => Mathf.Clamp01(CurrentAbsolute / Max);

        /// <inheritdoc />
        public bool CanCommenceInteraction => true;

        /// <inheritdoc />
        public bool CanContinueInteraction => true;

        /// <inheritdoc />
        public float ExertionPenalty => 1f - Current;

        public Stamina(float max, float recoveryRate)
        {
            _baseMax = Mathf.Max(max, 0.01f);
            _max = _baseMax;
            _current = _max;
            _baseRecoveryRate = Mathf.Max(recoveryRate, 0f);
            _recoveryRate = _baseRecoveryRate;
        }

        /// <inheritdoc />
        public void ApplyModifiers(float maxScale, float regenScale)
        {
            // Preserve absolute current (not ratio) so per-tick modifier refresh does not drain the pool.
            float absolute = CurrentAbsolute;
            _max = Mathf.Max(_baseMax * Mathf.Max(maxScale, 0.05f), 0.01f);
            _recoveryRate = _baseRecoveryRate * Mathf.Max(regenScale, 0f);
            _current = Mathf.Min(absolute, _max);
        }

        /// <inheritdoc />
        public void SetBaseRecoveryRate(float recoveryRate)
        {
            _baseRecoveryRate = Mathf.Max(recoveryRate, 0f);
        }

        /// <inheritdoc />
        public void SetBaseMax(float max)
        {
            float ratio = Current;
            _baseMax = Mathf.Max(max, 0.01f);
            _max = _baseMax;
            _current = ratio * _max;
        }

        public void ConsumeStamina(float amount)
        {
            LastOverdraw = 0f;
            if (amount <= 0f)
            {
                return;
            }

            if (_current >= amount)
            {
                _current -= amount;
                return;
            }

            LastOverdraw = amount - Mathf.Max(_current, 0f);
            _current = 0f;
        }

        public void RechargeStamina(float deltaTime)
        {
            if (deltaTime <= 0f || _recoveryRate <= 0f)
            {
                return;
            }

            _current = Mathf.Min(_current + deltaTime * _max * _recoveryRate, _max);
        }
    }
}
