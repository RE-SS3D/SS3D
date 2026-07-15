using SS3D.Interactions;
using SS3D.Interactions.Extensions;
using SS3D.Interactions.Interfaces;
using UnityEngine;

namespace SS3D.Systems.Health
{
    /// <summary>
    /// Resolves gameplay body zones from aim rays and interaction points against armature colliders.
    /// </summary>
    public static class ZoneTargetResolver
    {
        private const float MaxPointResolveDistanceSqr = 0.08f;
        private const float MaxRayDistance = 8f;

        public static bool TryResolveZone(Vector3 worldPoint, HumanHealthController health, out BodyZone zone)
        {
            return TryResolveZoneFromPoint(worldPoint, health, out zone);
        }

        public static bool TryResolveCombatZone(InteractionEvent interactionEvent, HumanHealthController health, out BodyZone zone)
        {
            zone = BodyZone.Chest;
            if (health == null || interactionEvent == null)
            {
                return false;
            }

            if (TryBuildAimRay(interactionEvent, out Ray aimRay)
                && TryResolveZoneFromRay(aimRay, health, out zone, out RaycastHit hit))
            {
                zone = ApplyGroinBanding(zone, hit.point, health);
                return true;
            }

            if (!TryResolveZoneFromPoint(interactionEvent.Point, health, out zone))
            {
                return false;
            }

            zone = ApplyGroinBanding(zone, interactionEvent.Point, health);
            return true;
        }

        public static bool TryResolveZoneFromRay(Ray ray, HumanHealthController health, out BodyZone zone, out RaycastHit hit)
        {
            zone = BodyZone.Chest;
            hit = default;

            if (health == null)
            {
                return false;
            }

            float closestDistance = float.PositiveInfinity;
            bool found = false;

            ZoneTargetCollider[] colliders = health.GetComponentsInChildren<ZoneTargetCollider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                ZoneTargetCollider zoneCollider = colliders[i];
                Collider physicsCollider = zoneCollider.GetComponent<Collider>();
                if (physicsCollider == null || !physicsCollider.enabled)
                {
                    continue;
                }

                if (!IsBodyPartCollider(physicsCollider))
                {
                    continue;
                }

                if (!physicsCollider.Raycast(ray, out RaycastHit candidate, MaxRayDistance))
                {
                    continue;
                }

                if (candidate.distance >= closestDistance)
                {
                    continue;
                }

                closestDistance = candidate.distance;
                hit = candidate;
                zone = zoneCollider.Zone;
                found = true;
            }

            return found;
        }

        public static BodyZone ApplyGroinBanding(BodyZone zone, Vector3 worldHit, HumanHealthController health)
        {
            if (zone != BodyZone.Chest || health == null)
            {
                return zone;
            }

            if (!TryGetTorsoLocalHeight01(health, worldHit, out float localHeight01))
            {
                return zone;
            }

            return ResolveGroinBand(zone, localHeight01);
        }

        public static BodyZone ResolveGroinBand(BodyZone zone, float torsoLocalHeight01)
        {
            if (zone == BodyZone.Chest && torsoLocalHeight01 < HealthConstants.GroinTorsoBandFraction)
            {
                return BodyZone.Groin;
            }

            return zone;
        }

        private static bool TryResolveZoneFromPoint(Vector3 worldPoint, HumanHealthController health, out BodyZone zone)
        {
            zone = BodyZone.Chest;
            if (health == null)
            {
                return false;
            }

            float bestDistanceSqr = MaxPointResolveDistanceSqr;
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

        private static bool TryBuildAimRay(InteractionEvent interactionEvent, out Ray ray)
        {
            ray = default;
            if (interactionEvent.Point.sqrMagnitude < 0.001f)
            {
                return false;
            }

            IInteractionOriginProvider originProvider = interactionEvent.Source.GetComponentInTree<IInteractionOriginProvider>(out IGameObjectProvider provider);
            Vector3 origin = originProvider != null
                ? originProvider.InteractionOrigin
                : provider?.GameObject.transform.position ?? interactionEvent.Point + Vector3.back;

            Vector3 direction = interactionEvent.Point - origin;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            ray = new Ray(origin, direction.normalized);
            return true;
        }

        private static bool IsBodyPartCollider(Collider collider)
        {
            int bodyPartsLayer = HealthLayers.BodyPartsLayer;
            if (bodyPartsLayer < 0)
            {
                return collider.GetComponent<ZoneTargetCollider>() != null;
            }

            return collider.gameObject.layer == bodyPartsLayer;
        }

        private static bool TryGetTorsoLocalHeight01(HumanHealthController health, Vector3 worldHit, out float localHeight01)
        {
            localHeight01 = 0f;
            Transform root = health.transform;
            float minLocalY = float.PositiveInfinity;
            float maxLocalY = float.NegativeInfinity;
            bool found = false;

            ZoneTargetCollider[] colliders = health.GetComponentsInChildren<ZoneTargetCollider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].Zone != BodyZone.Chest)
                {
                    continue;
                }

                Collider physicsCollider = colliders[i].GetComponent<Collider>();
                if (physicsCollider == null)
                {
                    continue;
                }

                Bounds bounds = physicsCollider.bounds;
                Vector3 localMin = root.InverseTransformPoint(bounds.min);
                Vector3 localMax = root.InverseTransformPoint(bounds.max);
                minLocalY = Mathf.Min(minLocalY, localMin.y, localMax.y);
                maxLocalY = Mathf.Max(maxLocalY, localMin.y, localMax.y);
                found = true;
            }

            if (!found || Mathf.Approximately(maxLocalY, minLocalY))
            {
                return false;
            }

            Vector3 localHit = root.InverseTransformPoint(worldHit);
            localHeight01 = Mathf.Clamp01((localHit.y - minLocalY) / (maxLocalY - minLocalY));
            return true;
        }
    }
}
