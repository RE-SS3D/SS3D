using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// What a blast does to a body's motion, from the force the BFS left on its tile
    /// (explosives-destruction.md §5). Force is the same figure crew brute uses.
    /// </summary>
    public static class BlastPush
    {
        /// <summary>
        /// Force that floors a character. It is reached within two hops of a C4 and only on a minibomb's
        /// own tile, which is what "close to the epicenter" comes to with current yields.
        /// </summary>
        public const float KnockdownForce = 90f;

        public const float SpeedPerForce = 0.05f;

        /// <summary>Bones collide discretely, so this keeps a launch from stepping through a wall.</summary>
        public const float MaxSpeed = 10f;

        public static bool KnocksDown(float force) => force >= KnockdownForce;

        /// <summary>Planar velocity straight away from the epicenter.</summary>
        /// <param name="fallback">Direction for a body exactly on the epicenter.</param>
        public static Vector3 Velocity(Vector3 epicenter, Vector3 body, Vector3 fallback, float force)
        {
            Vector3 away = body - epicenter;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
            {
                away = fallback;
                away.y = 0f;
            }

            return away.normalized * Mathf.Min(force * SpeedPerForce, MaxSpeed);
        }
    }
}
