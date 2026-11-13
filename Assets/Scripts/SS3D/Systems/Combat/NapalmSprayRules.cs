using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    public static class NapalmSprayRules
    {
        public const float Range = 4f;
        public const float HalfAngle = 25f;
        public const float VerticalHalfAngle = 6f;
        public const int RayCount = 9;
        public const float ExposureRadius = NapalmPatchSet.Radius + 0.25f;
        /// <summary>Spraying a surface closer than this splashes burning fuel back onto the shooter.</summary>
        public const float BlowbackRange = 1.1f;

        public static Vector3 Direction(Vector3 forward, int index)
        {
            float yaw = Mathf.Lerp(-HalfAngle, HalfAngle, index / (float)(RayCount - 1));
            // Three low/high/centre bands sample the shallow fan without making a spherical cone.
            float pitch = (index % 3 - 1) * VerticalHalfAngle;
            return Quaternion.LookRotation(forward) * Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
        }

        public static bool Touches(Vector3 point, Vector3 normal, Collider limb)
        {
            if (limb == null || !limb.enabled || !limb.gameObject.activeInHierarchy) return false;
            if (limb.bounds.SqrDistance(point) > 3f) return false;
            Vector3 contact = limb.ClosestPoint(point + normal * 0.35f);
            return Exposes(point, normal, contact);
        }

        public static bool Exposes(Vector3 point, Vector3 normal, Vector3 bodyPoint)
        {
            Vector3 delta = bodyPoint - point;
            float height = Vector3.Dot(delta, normal);
            Vector3 tangent = delta - normal * height;
            if (height < -0.05f || height > 1.4f || tangent.sqrMagnitude > ExposureRadius * ExposureRadius)
                return false;
            // Trigger volumes (doors, interaction zones) are not cover.
            return LineOfSight.HasLineOfSight(point + normal * 0.08f, bodyPoint,
                LayerMask.GetMask("Default", "Walls"), out _, QueryTriggerInteraction.Ignore);
        }
    }
}
