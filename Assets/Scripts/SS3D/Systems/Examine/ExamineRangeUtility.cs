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
                Vector3 closestPoint = collider.ClosestPoint(origin);
                return range.IsInRange(origin, closestPoint);
            }

            return range.IsInRange(origin, target.transform.position);
        }
    }
}
