using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Pure helpers for blast presentation intensity (EditMode-tested).
    /// </summary>
    public static class BlastVfxFalloff
    {
        public const float ReferenceYield = 120f;

        /// <summary>
        /// 1 at distance ≤ <paramref name="fullRange"/>, 0 at ≥ <paramref name="maxRange"/>, linear between.
        /// </summary>
        public static float DistanceAttenuation(float distance, float fullRange, float maxRange)
        {
            if (maxRange <= fullRange)
                return distance <= fullRange ? 1f : 0f;

            if (distance <= fullRange)
                return 1f;

            if (distance >= maxRange)
                return 0f;

            return 1f - (distance - fullRange) / (maxRange - fullRange);
        }

        /// <summary>Yield at and below which <see cref="Violence"/> is 1: X4's single detonation event.</summary>
        public const float ViolentYield = 300f;

        /// <summary>Cap on <see cref="Violence"/>; the Syndicate bomb (600) sits at it.</summary>
        public const float MaxViolence = 2f;

        /// <summary>
        /// Extra punch for blasts bigger than any charge, past where <see cref="YieldScale"/> stops growing: 1 up to
        /// <see cref="ViolentYield"/>, then in proportion, capped at <see cref="MaxViolence"/>. It stretches the shake,
        /// the flash and their reach, and swells the fireball.
        /// </summary>
        public static float Violence(float yield) => Mathf.Clamp(yield / ViolentYield, 1f, MaxViolence);

        /// <summary>Scales presentation from yield relative to a reference grenade-sized blast.</summary>
        public static float YieldScale(float yield, float referenceYield = ReferenceYield)
        {
            if (referenceYield <= 0f)
                return 1f;

            return Mathf.Clamp(yield / referenceYield, 0.35f, 2.5f);
        }
    }
}
