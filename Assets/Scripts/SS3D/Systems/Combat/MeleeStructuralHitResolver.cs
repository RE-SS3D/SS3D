using SS3D.Core;
using SS3D.Interactions;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using System;
using UnityEngine;

namespace SS3D.Systems.Combat
{
    /// <summary>
    /// Resolves a melee aim ray to structural Turf (wall/door/window) within reach of the attacker.
    /// Reach uses the entity root (not the swinging hand bone) — hand IK during windup often sits
    /// past <see cref="SS3D.Interactions.RangeLimit"/> even when the player is adjacent to the wall.
    /// </summary>
    public static class MeleeStructuralHitResolver
    {
        /// <summary>Same order of magnitude as <c>ZoneTargetResolver</c> camera hover rays.</summary>
        private const float MaxAimRayDistance = 8f;
        private const float AimSampleStep = 0.25f;
        /// <summary>Chest height above the entity root; melee reach is measured from here.</summary>
        public const float ReachHeight = 1.2f;

        /// <summary>Tiles are 1 m, centred on <c>TileToWorld</c>; structural turf fills its tile.</summary>
        private const float TileHalfExtent = 0.5f;

        private static readonly string[] ExcludeLayerNames =
        {
            "Characters",
            "BodyParts",
            "UI",
            "Ignore Raycast",
            "TransparentFX",
            "Water",
        };

        /// <param name="aimRay">Synced camera mouse ray (not hand→aim).</param>
        /// <param name="attackerPosition">Entity root world position (stable reach origin).</param>
        /// <param name="range">Hand interaction range; applied from entity torso, not the hand bone.</param>
        public static bool TryResolve(Ray aimRay, Vector3 attackerPosition, RangeLimit range, out TileCoord coord)
        {
            return TryResolve(aimRay, attackerPosition, range, out coord, out _);
        }

        public static bool TryResolve(Ray aimRay, Vector3 attackerPosition, RangeLimit range,
            out TileCoord coord, out RaycastHit? contact)
        {
            coord = default;
            contact = null;

            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null || tiles.QueryService == null)
            {
                return false;
            }

            Vector3 reachOrigin = attackerPosition + Vector3.up * ReachHeight;

            if (TryResolveFromRaycast(aimRay, reachOrigin, range, tiles, out coord, out contact))
            {
                return true;
            }

            if (TryResolveBySamplingAimRay(aimRay, reachOrigin, range, tiles, out coord))
            {
                return true;
            }

            // Same idea as hurtstructure: standing tile + cardinal step along aim. Catches adjacent
            // walls when the camera ray grazes past thin colliders or range samples miss.
            if (TryResolveCardinalAhead(attackerPosition, aimRay.direction, range, tiles, out coord))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Standing tile + one cardinal step along <paramref name="facingDirection"/> — same targeting
        /// as admin <c>hurtstructure</c>. Use when the attacker is adjacent and swinging into a wall.
        /// </summary>
        public static bool TryResolveAdjacent(Vector3 attackerPosition, Vector3 facingDirection, RangeLimit range, out TileCoord coord)
        {
            coord = default;

            if (!SubSystems.TryGet(out TileSubSystem tiles) || tiles.CurrentMap == null || tiles.QueryService == null)
            {
                return false;
            }

            return TryResolveCardinalAhead(attackerPosition, facingDirection, range, tiles, out coord);
        }

        private static bool TryResolveFromRaycast(
            Ray aimRay,
            Vector3 reachOrigin,
            RangeLimit range,
            TileSubSystem tiles,
            out TileCoord coord,
            out RaycastHit? contact)
        {
            coord = default;
            contact = null;
            int mask = ~LayerMask.GetMask(ExcludeLayerNames);
            RaycastHit[] hits = Physics.RaycastAll(aimRay, MaxAimRayDistance, mask, QueryTriggerInteraction.Ignore);
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

                Vector3 reachPoint = hit.collider.ClosestPoint(reachOrigin);
                if (!range.IsInRange(reachOrigin, reachPoint))
                {
                    continue;
                }

                coord = new TileCoord(tiles.CurrentMap.MapId, placed.WorldOrigin.x, placed.WorldOrigin.y);
                contact = hit;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Fallback when wall colliders miss the ray: walk the aim ray and pick the first structural
        /// tile whose cell is still within reach of the attacker.
        /// </summary>
        private static bool TryResolveBySamplingAimRay(
            Ray aimRay,
            Vector3 reachOrigin,
            RangeLimit range,
            TileSubSystem tiles,
            out TileCoord coord)
        {
            coord = default;
            int mapId = tiles.CurrentMap.MapId;

            for (float distance = AimSampleStep; distance <= MaxAimRayDistance; distance += AimSampleStep)
            {
                Vector3 point = aimRay.GetPoint(distance);
                TileCoord candidate = tiles.QueryService.WorldToTile(point, mapId);
                if (!TryGetStructuralAt(tiles, candidate, out _))
                {
                    continue;
                }

                if (!IsTileFaceInRange(tiles, candidate, reachOrigin, range))
                {
                    continue;
                }

                coord = candidate;
                return true;
            }

            return false;
        }

        private static bool TryResolveCardinalAhead(
            Vector3 attackerPosition,
            Vector3 aimDirection,
            RangeLimit range,
            TileSubSystem tiles,
            out TileCoord coord)
        {
            coord = default;
            int mapId = tiles.CurrentMap.MapId;
            TileCoord standing = tiles.QueryService.WorldToTile(attackerPosition, mapId);

            Vector3 flat = aimDirection;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            Direction facing = CardinalFromForward(flat);
            TileCoord ahead = OffsetCardinal(standing, facing);
            if (!TryGetStructuralAt(tiles, ahead, out _))
            {
                return false;
            }

            // The tile ahead can be up to a full tile away from the far side of the standing tile.
            if (!IsTileFaceInRange(tiles, ahead, attackerPosition + Vector3.up * ReachHeight, range))
            {
                return false;
            }

            coord = ahead;
            return true;
        }

        /// <summary>Reach to the nearest face of a structural tile (not its centre, which is half a tile deeper).</summary>
        private static bool IsTileFaceInRange(TileSubSystem tiles, TileCoord tile, Vector3 reachOrigin, RangeLimit range)
        {
            Vector3 centre = tiles.QueryService.TileToWorld(tile);
            Vector3 nearestFace = new(
                Mathf.Clamp(reachOrigin.x, centre.x - TileHalfExtent, centre.x + TileHalfExtent),
                reachOrigin.y,
                Mathf.Clamp(reachOrigin.z, centre.z - TileHalfExtent, centre.z + TileHalfExtent));
            return range.IsInRange(reachOrigin, nearestFace);
        }

        private static bool TryGetStructuralAt(TileSubSystem tiles, TileCoord candidate, out PlacedTileObject placed)
        {
            placed = null;
            if (!tiles.QueryService.TryGetOccupant(candidate, TileLayer.Turf, Direction.North, out ITileOccupant occupant))
            {
                return false;
            }

            if (occupant is not PlacedTileObject tileObject || !StructuralIntegrityRules.IsStructural(tileObject))
            {
                return false;
            }

            placed = tileObject;
            return true;
        }

        private static Direction CardinalFromForward(Vector3 forward)
        {
            forward.Normalize();
            if (Mathf.Abs(forward.z) >= Mathf.Abs(forward.x))
            {
                return forward.z >= 0f ? Direction.North : Direction.South;
            }

            return forward.x >= 0f ? Direction.East : Direction.West;
        }

        private static TileCoord OffsetCardinal(TileCoord coord, Direction direction)
        {
            Tuple<int, int> offset = TileHelper.ToCardinalVector(direction);
            return new TileCoord(coord.MapId, coord.Grid.x + offset.Item1, coord.Grid.y + offset.Item2);
        }
    }
}
