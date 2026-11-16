using UnityEngine;

namespace SS3D.Systems.Entities.Humanoid.Body
{
    /// <summary>
    /// Weight of the additive (injury / Flinch) animator layer: eases towards its target, and a flinch without stagger
    /// (combat.md §5) lifts it briefly so the Flinch clip shows on a body that keeps doing what it was doing.
    /// </summary>
    public struct AdditiveLayerWeight
    {
        public const float FlinchSeconds = 0.45f;

        /// <summary>Flinch weight at each strength level (the trigger's variant, 0–3).</summary>
        private static readonly float[] FlinchWeights = { 0.3f, 0.45f, 0.6f, 0.75f };

        public float Current;
        public float Target;
        private float _flinchUntil;
        private float _flinchWeight;

        public void Flinch(int strength, float now)
        {
            _flinchWeight = FlinchWeights[strength & 0x3];
            _flinchUntil = now + FlinchSeconds;
        }

        /// <summary>The target with any running flinch applied.</summary>
        public readonly float EffectiveTarget(float now) => now < _flinchUntil ? Mathf.Max(Target, _flinchWeight) : Target;

        public float Tick(float deltaTime, float lerpPerSecond, float now)
        {
            Current = Mathf.MoveTowards(Current, EffectiveTarget(now), deltaTime * lerpPerSecond);
            return Current;
        }
    }
}
