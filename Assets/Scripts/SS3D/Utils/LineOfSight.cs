using UnityEngine;

namespace SS3D.Utils
{
    /// <summary>
    /// Shared occlusion raycast for Drop, local speech, combat cover, and other consumers.
    /// One raycast system — do not fork parallel Physics.Raycast LOS checks.
    /// </summary>
    public static class LineOfSight
    {
        /// <summary>
        /// True when nothing on <paramref name="mask"/> intersects the segment from
        /// <paramref name="origin"/> to <paramref name="end"/> (exclusive of zero-length).
        /// </summary>
        public static bool HasLineOfSight(Vector3 origin, Vector3 end, LayerMask mask, out RaycastHit blockHit,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal)
        {
            Vector3 delta = end - origin;
            float distance = delta.magnitude;
            if (distance <= 0f)
            {
                blockHit = default;
                return true;
            }

            Vector3 direction = delta / distance;
            if (Physics.Raycast(origin, direction, out blockHit, distance, mask, triggers))
            {
                return false;
            }

            blockHit = default;
            return true;
        }

        /// <summary>
        /// True when nothing on <paramref name="mask"/> intersects within
        /// <paramref name="distance"/> along <paramref name="direction"/>.
        /// </summary>
        public static bool HasLineOfSight(
            Vector3 origin,
            Vector3 direction,
            float distance,
            LayerMask mask,
            out RaycastHit blockHit)
        {
            if (distance <= 0f)
            {
                blockHit = default;
                return true;
            }

            Vector3 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
            if (Physics.Raycast(origin, dir, out blockHit, distance, mask))
            {
                return false;
            }

            blockHit = default;
            return true;
        }

        /// <summary>
        /// First hit along a ray, or false if nothing is hit within max distance.
        /// </summary>
        public static bool TryGetFirstHit(
            Vector3 origin,
            Vector3 direction,
            float maxDistance,
            LayerMask mask,
            out RaycastHit hit,
            QueryTriggerInteraction triggers = QueryTriggerInteraction.UseGlobal)
        {
            Vector3 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
            return Physics.Raycast(origin, dir, out hit, maxDistance, mask, triggers);
        }
    }
}
