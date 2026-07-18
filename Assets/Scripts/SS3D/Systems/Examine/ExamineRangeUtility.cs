using SS3D.Interactions;
using UnityEngine;

namespace SS3D.Systems.Examine
{
    public static class ExamineRangeUtility
    {
        public static bool IsWithinRange(IExaminable examinable, Vector3 origin, RangeLimit range)
        {
            if (examinable is not ExaminableBase examinableObject)
            {
                return false;
            }

            GameObject target = examinableObject.GameObject;
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Vector3 closestPoint = GetClosestPoint(collider, origin);
                return range.IsInRange(origin, closestPoint);
            }

            return range.IsInRange(origin, target.transform.position);
        }

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
