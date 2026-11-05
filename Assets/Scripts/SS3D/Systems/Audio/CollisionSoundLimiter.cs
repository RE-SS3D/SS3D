namespace SS3D.Systems.Audio
{
    /// <summary>
    /// Caps collision sounds, which are server messages, so a breach that throws every loose item
    /// around doesn't become constant clatter or traffic. Each source has a cooldown, which stops a
    /// bouncing item spending the budget, and all sources share one budget that refills over time.
    /// </summary>
    public sealed class CollisionSoundLimiter
    {
        /// <summary>A source stays quiet this long after it sounds.</summary>
        public const float SourceCooldown = 0.25f;

        /// <summary>The most sounds that can play at once.</summary>
        public const int Burst = 5;

        /// <summary>The most sounds per second over any longer stretch.</summary>
        public const float RefillPerSecond = 5f;

        private float _budget = Burst;
        private float _lastRefillTime;

        /// <summary>Whether a source may sound now. On true, spends budget and starts its cooldown.</summary>
        /// <param name="now">Current time in seconds.</param>
        /// <param name="sourceReadyTime">Kept by the source, which starts it at zero.</param>
        public bool TryPlay(float now, ref float sourceReadyTime)
        {
            if (now < sourceReadyTime)
            {
                return false;
            }

            _budget = System.Math.Min(Burst, _budget + (now - _lastRefillTime) * RefillPerSecond);
            _lastRefillTime = now;
            if (_budget < 1f)
            {
                return false;
            }

            _budget -= 1f;
            sourceReadyTime = now + SourceCooldown;
            return true;
        }
    }
}
