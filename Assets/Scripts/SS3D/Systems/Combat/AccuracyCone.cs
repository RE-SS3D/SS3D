using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Samples a direction inside a weapon accuracy cone (physical spread, not a hidden miss roll).
    /// </summary>
    public static class AccuracyCone
    {
        /// <summary>
        /// Uniform-ish sample within a cone of half-angle <paramref name="spreadDegrees"/> around
        /// <paramref name="aimDirection"/>.
        /// </summary>
        public static Vector3 SampleDirection(Vector3 aimDirection, float spreadDegrees, System.Random rng)
        {
            if (aimDirection.sqrMagnitude < 0.0001f)
            {
                aimDirection = Vector3.forward;
            }
            else
            {
                aimDirection.Normalize();
            }

            float spread = Mathf.Max(0f, spreadDegrees);
            if (spread <= 0.0001f || rng == null)
            {
                return aimDirection;
            }

            float maxRad = spread * Mathf.Deg2Rad;
            float cosMax = Mathf.Cos(maxRad);
            float u = (float)rng.NextDouble();
            float z = Mathf.Lerp(cosMax, 1f, u);
            float phi = (float)(rng.NextDouble() * Mathf.PI * 2f);
            float sinTheta = Mathf.Sqrt(Mathf.Max(0f, 1f - (z * z)));
            Vector3 local = new(sinTheta * Mathf.Cos(phi), sinTheta * Mathf.Sin(phi), z);
            return Quaternion.LookRotation(aimDirection) * local;
        }

        /// <summary>
        /// Builds current half-angle spread from profile + recoil + movement + range estimate.
        /// </summary>
        public static float ComputeSpreadDegrees(
            in RangedWeaponProfile profile,
            float recoilStacks,
            float horizontalSpeed,
            float aimDistanceMeters,
            float exertionPenalty = 0f)
        {
            float spread = Mathf.Max(0f, profile.BaseSpreadDegrees);
            spread += Mathf.Max(0f, recoilStacks) * Mathf.Max(0f, profile.RecoilClimbDegrees);
            spread += Mathf.Max(0f, horizontalSpeed) * Mathf.Max(0f, profile.MovementBloomPerSpeed);

            float start = Mathf.Max(0f, profile.FalloffStartMeters);
            float end = Mathf.Max(start + 0.01f, profile.FalloffEndMeters);
            if (aimDistanceMeters > start)
            {
                float t = Mathf.InverseLerp(start, end, aimDistanceMeters);
                spread += t * Mathf.Max(0f, profile.FalloffExtraSpreadDegrees);
            }

            spread += Mathf.Clamp01(exertionPenalty) * Mathf.Max(0f, profile.ExhaustionSpreadDegrees);

            return spread;
        }
    }
}
