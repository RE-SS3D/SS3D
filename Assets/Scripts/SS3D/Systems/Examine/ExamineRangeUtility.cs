using SS3D.Interactions;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    public static class ExamineRangeUtility
    {
        public static bool IsWithinRange(IExaminable examinable, Vector3 origin, RangeLimit range)
        {
            GameObject target = GetGameObject(examinable);
            if (target == null)
            {
                return false;
            }

            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Vector3 closestPoint = GetClosestPoint(collider, origin);
                return range.IsInRange(origin, closestPoint);
            }

            return range.IsInRange(origin, target.transform.position);
        }

        /// <summary>The world object behind an examinable, or null.</summary>
        public static GameObject GetGameObject(IExaminable examinable) => examinable switch
        {
            ItemFallbackExaminable fallback => fallback.Item != null ? fallback.Item.gameObject : null,
            Component component => component != null ? component.gameObject : null,
            _ => null,
        };

        private static Vector3 GetClosestPoint(Collider collider, Vector3 position)
        {
            if (collider is MeshCollider { convex: false })
            {
                return collider.ClosestPointOnBounds(position);
            }

            if (collider is BoxCollider or SphereCollider or CapsuleCollider or MeshCollider)
            {
                return collider.ClosestPoint(position);
            }

            return collider.ClosestPointOnBounds(position);
        }
    }
}
