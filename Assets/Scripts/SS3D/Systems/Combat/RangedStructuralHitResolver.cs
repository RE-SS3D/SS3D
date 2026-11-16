using SS3D.Core;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using System;
using SS3D.Utils;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Resolves a hitscan ray to structural Turf (wall/door/window) with no melee reach gate.
    /// </summary>
    public static class RangedStructuralHitResolver
    {
        private static readonly string[] ExcludeLayerNames =
        {
            "Characters",
            "BodyParts",
            "UI",
            "Ignore Raycast",
            "TransparentFX",
            "Water",
        };

        public static bool TryResolve(Ray shotRay, float maxDistance, out TileCoord coord, out float hitDistance, out Vector3 hitNormal)
        {
            coord = default;
            hitDistance = 0f;
            hitNormal = -shotRay.direction;

            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null || tiles.QueryService == null)
            {
                return false;
            }

            int mask = StationBounds.WithoutCeiling(~LayerMask.GetMask(ExcludeLayerNames));
            RaycastHit[] hits = Physics.RaycastAll(shotRay, maxDistance, mask, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
            {
                return false;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                PlacedTileObject placed = hit.collider.GetComponentInParent<PlacedTileObject>();
                if (placed == null || !StructuralIntegrityRules.IsStructural(placed))
                {
                    continue;
                }

                coord = new TileCoord(tiles.CurrentMap.MapId, placed.WorldOrigin.x, placed.WorldOrigin.y);
                hitDistance = hit.distance;
                hitNormal = hit.normal.sqrMagnitude > 0.0001f ? hit.normal.normalized : -shotRay.direction.normalized;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves structural Turf and returns distance only (normal discarded).
        /// </summary>
        public static bool TryResolve(Ray shotRay, float maxDistance, out TileCoord coord, out float hitDistance)
        {
            return TryResolve(shotRay, maxDistance, out coord, out hitDistance, out _);
        }

        /// <summary>
        /// True when a Default-layer occluder sits closer than <paramref name="maxDistance"/> along the shot.
        /// </summary>
        public static bool IsOccluded(Ray shotRay, float maxDistance, LayerMask occlusionMask, out float blockDistance)
        {
            blockDistance = 0f;
            if (!LineOfSight.TryGetFirstHit(shotRay.origin, shotRay.direction, maxDistance, occlusionMask, out RaycastHit block))
            {
                return false;
            }

            blockDistance = block.distance;
            return true;
        }
    }
}
