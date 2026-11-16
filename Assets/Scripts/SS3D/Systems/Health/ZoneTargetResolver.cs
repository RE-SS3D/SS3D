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

        /// <summary>Default hover/melee camera ray length. Ranged passes an explicit max.</summary>
        public const float DefaultMaxRayDistance = 8f;

        /// <summary>
        /// Player-facing zone label for the Main HUD reticle chip (main-hud §6).
        /// </summary>
        public static string GetReticleLabel(BodyZone zone)
        {
            return zone switch
            {
                BodyZone.Head => "Head",
                BodyZone.Chest => "Chest",
                BodyZone.LeftArm => "Left Arm",
                BodyZone.RightArm => "Right Arm",
                BodyZone.LeftLeg => "Left Leg",
                BodyZone.RightLeg => "Right Leg",
                BodyZone.Groin => "Groin",
                _ => "Chest",
            };
        }

        /// <summary>
        /// Camera-aim hover resolve for the zone reticle: finds a health root under the ray,
        /// then resolves the zone via the same collider raycasts combat uses (armature bones may
        /// be on Characters and/or triggers — do not rely on BodyParts Physics.RaycastAll alone).
        /// </summary>
        public static bool TryResolveHoverZone(Ray ray, out BodyZone zone, out HumanHealthController health)
        {
            return TryResolveHoverZone(ray, excludeHealth: null, out zone, out health, out _, out _);
        }

        /// <inheritdoc cref="TryResolveHoverZone(Ray, out BodyZone, out HumanHealthController)"/>
        public static bool TryResolveHoverZone(
            Ray ray,
            out BodyZone zone,
            out HumanHealthController health,
            out Vector3 hitPoint)
        {
            return TryResolveHoverZone(ray, excludeHealth: null, out zone, out health, out hitPoint, out _);
        }

        /// <inheritdoc cref="TryResolveHoverZone(Ray, out BodyZone, out HumanHealthController)"/>
        public static bool TryResolveHoverZone(
            Ray ray,
            HumanHealthController excludeHealth,
            out BodyZone zone,
            out HumanHealthController health,
            out Vector3 hitPoint,
            out Collider zoneCollider)
        {
            return TryResolveHoverZone(
                ray,
                excludeHealth,
                DefaultMaxRayDistance,
                out zone,
                out health,
                out hitPoint,
                out zoneCollider);
        }

        /// <param name="maxRayDistance">Camera/hitscan cast length (melee hover uses <see cref="DefaultMaxRayDistance"/>).</param>
        public static bool TryResolveHoverZone(
            Ray ray,
            HumanHealthController excludeHealth,
            float maxRayDistance,
            out BodyZone zone,
            out HumanHealthController health,
            out Vector3 hitPoint,
            out Collider zoneCollider)
        {
            zone = BodyZone.Chest;
            health = null;
            hitPoint = default;
            zoneCollider = null;

            float castDistance = Mathf.Max(0.01f, maxRayDistance);

            int mask = LayerMask.GetMask("Characters", HealthLayers.BodyPartsLayerName);
            if (mask == 0)
            {
                mask = ~0;
            }

            RaycastHit[] hits = Physics.RaycastAll(ray, castDistance, mask, QueryTriggerInteraction.Collide);
            if (hits.Length == 0)
            {
                return false;
            }

            float closestDistance = float.PositiveInfinity;
            HumanHealthController closestHealth = null;

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit candidateHit = hits[i];
                if (candidateHit.distance >= closestDistance)
                {
                    continue;
                }

                HumanHealthController candidate = candidateHit.collider.GetComponentInParent<HumanHealthController>();
                if (candidate == null || candidate == excludeHealth)
                {
                    continue;
                }

                closestDistance = candidateHit.distance;
                closestHealth = candidate;
            }

            if (closestHealth == null)
            {
                return false;
            }

            if (!TryResolveZoneFromRay(ray, closestHealth, castDistance, out zone, out RaycastHit zoneHit))
            {
                return false;
            }

            health = closestHealth;
            hitPoint = zoneHit.point;
            zoneCollider = zoneHit.collider;
            zone = ApplyGroinBanding(zone, zoneHit.point, closestHealth);
            return true;
        }

        /// <summary>
        /// Melee reach uses the closest point on the resolved zone collider — not the ray impact.
        /// Extended limbs (forearm, hand) can be past hand range at the hit point while still reachable.
        /// </summary>
        public static bool IsMeleeZoneReachInRange(Vector3 handOrigin, RangeLimit range, Collider zoneCollider)
        {
            if (zoneCollider == null)
            {
                return false;
            }

            Vector3 reachPoint = zoneCollider.ClosestPoint(handOrigin);
            return range.IsInRange(handOrigin, reachPoint);
        }

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
            return TryResolveZoneFromRay(ray, health, DefaultMaxRayDistance, out zone, out hit);
        }

        public static bool TryResolveZoneFromRay(
            Ray ray,
            HumanHealthController health,
            float maxRayDistance,
            out BodyZone zone,
            out RaycastHit hit)
        {
            zone = BodyZone.Chest;
            hit = default;

            if (health == null)
            {
                return false;
            }

            float castDistance = Mathf.Max(0.01f, maxRayDistance);
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

                if (!TryPickClosestZoneHit(
                        ray,
                        physicsCollider,
                        zoneCollider.Zone,
                        castDistance,
                        ref closestDistance,
                        ref hit,
                        ref zone))
                {
                    continue;
                }

                found = true;
            }

            // Limb mesh colliders (HumanArmLeft/Right etc.) follow AnatomyNode and may catch rays
            // the smaller armature ZoneTargetCollider triggers miss — especially while animating.
            AnatomyNode[] anatomyNodes = health.GetComponentsInChildren<AnatomyNode>(true);
            for (int i = 0; i < anatomyNodes.Length; i++)
            {
                AnatomyNode anatomyNode = anatomyNodes[i];
                if (!anatomyNode.IsDetachable || anatomyNode.IsSevered)
                {
                    continue;
                }

                Collider[] anatomyColliders = anatomyNode.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < anatomyColliders.Length; c++)
                {
                    Collider physicsCollider = anatomyColliders[c];
                    if (physicsCollider == null || !physicsCollider.enabled)
                    {
                        continue;
                    }

                    if (physicsCollider.GetComponent<ZoneTargetCollider>() != null)
                    {
                        continue;
                    }

                    if (!TryPickClosestZoneHit(
                            ray,
                            physicsCollider,
                            anatomyNode.PrimaryZone,
                            castDistance,
                            ref closestDistance,
                            ref hit,
                            ref zone))
                    {
                        continue;
                    }

                    found = true;
                }
            }

            return found;
        }

        private static bool TryPickClosestZoneHit(
            Ray ray,
            Collider physicsCollider,
            BodyZone candidateZone,
            float maxRayDistance,
            ref float closestDistance,
            ref RaycastHit hit,
            ref BodyZone zone)
        {
            if (!physicsCollider.Raycast(ray, out RaycastHit candidate, maxRayDistance))
            {
                return false;
            }

            if (candidate.distance >= closestDistance)
            {
                return false;
            }

            closestDistance = candidate.distance;
            hit = candidate;
            zone = candidateZone;
            return true;
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
            if (!interactionEvent.HasPoint)
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
            // ZoneTargetCollider is the contract. Armature zone bones still sit on Characters
            // (often as triggers); do not require the BodyParts layer name alone.
            return collider != null && collider.GetComponent<ZoneTargetCollider>() != null;
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
