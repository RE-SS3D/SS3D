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

        /// <summary>Scales presentation from yield relative to a reference grenade-sized blast.</summary>
        public static float YieldScale(float yield, float referenceYield = ReferenceYield)
        {
            if (referenceYield <= 0f)
                return 1f;

            return Mathf.Clamp(yield / referenceYield, 0.35f, 2.5f);
        }
    }
}
