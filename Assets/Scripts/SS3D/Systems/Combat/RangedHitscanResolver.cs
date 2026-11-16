using SS3D.Core;
using SS3D.Systems.Health;
using SS3D.Systems.Tile;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Server hitscan resolve: accuracy-cone sample → living zone (with LOS) or structural turf.
    /// </summary>
    public static class RangedHitscanResolver
    {
        /// <summary>
        /// Cover layers between muzzle and a limb. Must include Walls — many tile walls are not Default.
        /// Do not early-out on a full-range Default cast: Characters are excluded from that mask, so the
        /// ray passes through the target and can "block" on floor/props behind them, skipping the hit.
        /// </summary>
        private static readonly LayerMask CoverOcclusionMask = LayerMask.GetMask("Default", "Walls");

        public static bool TryResolveShot(
            Ray aimRay,
            in RangedWeaponProfile profile,
            float spreadDegrees,
            System.Random rng,
            HumanHealthController excludeHealth,
            out HumanHealthController health,
            out BodyZone zone,
            out TileCoord structuralCoord,
            out bool hitLiving,
            out bool hitStructural,
            out Vector3 impactPoint,
            out Vector3 impactNormal,
            out bool hasImpact,
            out Vector3 shotDirection)
        {
            health = null;
            zone = BodyZone.Chest;
            structuralCoord = default;
            hitLiving = false;
            hitStructural = false;
            impactPoint = default;
            impactNormal = Vector3.up;
            hasImpact = false;
            shotDirection = aimRay.direction.sqrMagnitude > 0.0001f
                ? aimRay.direction.normalized
                : Vector3.forward;

            float maxRange = Mathf.Max(1f, profile.MaxRangeMeters);
            Vector3 shotDir = AccuracyCone.SampleDirection(aimRay.direction, spreadDegrees, rng);
            shotDirection = shotDir;
            Ray shotRay = new(aimRay.origin, shotDir);

            // Living first — cover is checked only along the segment to the limb (not past it).
            if (ZoneTargetResolver.TryResolveHoverZone(
                    shotRay,
                    excludeHealth,
                    maxRange,
                    out zone,
                    out health,
                    out Vector3 hitPoint,
                    out _))
            {
                if (!LineOfSight.HasLineOfSight(shotRay.origin, hitPoint, CoverOcclusionMask, out RaycastHit coverHit))
                {
                    float coverDistance = coverHit.distance > 0f
                        ? coverHit.distance
                        : Vector3.Distance(shotRay.origin, hitPoint);

                    if (RangedStructuralHitResolver.TryResolve(shotRay, coverDistance + 0.05f, out structuralCoord, out float structuralDistance, out Vector3 structuralNormal))
                    {
                        health = null;
                        hitStructural = true;
                        impactPoint = shotRay.GetPoint(structuralDistance);
                        impactNormal = structuralNormal;
                        hasImpact = true;
                        return true;
                    }

                    health = null;
                    if (coverHit.collider != null)
                    {
                        impactPoint = coverHit.point;
                        impactNormal = coverHit.normal;
                    }
                    else
                    {
                        impactPoint = hitPoint;
                        impactNormal = -shotDir;
                    }

                    hasImpact = true;
                    return false;
                }

                hitLiving = true;
                impactPoint = hitPoint;
                impactNormal = -shotDir;
                hasImpact = true;
                return true;
            }

            if (RangedStructuralHitResolver.TryResolve(shotRay, maxRange, out structuralCoord, out float structDist, out Vector3 structNormal))
            {
                hitStructural = true;
                impactPoint = shotRay.GetPoint(structDist);
                impactNormal = structNormal;
                hasImpact = true;
                return true;
            }

            // Soft surface / floor / prop without structural turf — still show where the round went.
            // Prefer cover layers, then any non-trigger collider (CharacterController, props, etc.).
            if (LineOfSight.TryGetFirstHit(shotRay.origin, shotDir, maxRange, CoverOcclusionMask, out RaycastHit coverSurface))
            {
                impactPoint = coverSurface.point;
                impactNormal = coverSurface.normal;
                hasImpact = true;
                return false;
            }

            if (Physics.Raycast(shotRay, out RaycastHit anyHit, maxRange, StationBounds.WithoutCeiling(~0), QueryTriggerInteraction.Ignore))
            {
                impactPoint = anyHit.point;
                impactNormal = anyHit.normal;
                hasImpact = true;
            }

            return false;
        }
    }
}
