using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Resolves gameplay body zones from world interaction points against armature colliders.
    /// Full raycast combat integration ships in Phase 4.
    /// </summary>
    public static class ZoneTargetResolver
    {
        private const float MaxResolveDistanceSqr = 0.08f;

        public static bool TryResolveZone(Vector3 worldPoint, HumanHealthController health, out BodyZone zone)
        {
            zone = BodyZone.Chest;
            if (health == null)
            {
                return false;
            }

            float bestDistanceSqr = MaxResolveDistanceSqr;
            bool found = false;

            ZoneTargetCollider[] colliders = health.GetComponentsInChildren<ZoneTargetCollider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                ZoneTargetCollider zoneCollider = colliders[i];
                Collider physicsCollider = zoneCollider.GetComponent<Collider>();
                if (physicsCollider == null)
                {
                    continue;
                }

                Vector3 closestPoint = physicsCollider.ClosestPoint(worldPoint);
                float distanceSqr = (closestPoint - worldPoint).sqrMagnitude;
                if (distanceSqr >= bestDistanceSqr)
                {
                    continue;
                }

                bestDistanceSqr = distanceSqr;
                zone = zoneCollider.Zone;
                found = true;
            }

            return found;
        }
    }
}
