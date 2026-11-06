using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Breadth-first blast spread: open hops via BlockedEdges (same rules as atmos CanFlow)
    /// through plenum volume only, structural force on blocking turf, cascade when Destroyed clears an edge.
    /// </summary>
    public sealed class BlastResolutionService : IBlastResolutionService
    {
        public const float MinUsefulForce = 1f;

        private static readonly Direction[] Cardinals =
        {
            Direction.North, Direction.East, Direction.South, Direction.West,
        };

        private readonly ITileQueryService _query;
        private readonly IStructuralDamageService _structural;
        private readonly Action<TileCoord, float> _applyCrewBrute;
        private readonly Action<TileCoord, float> _applyFloorDamage;
        private readonly Action<TileCoord, IReadOnlyDictionary<TileCoord, float>> _applyCrewPush;

        /// <param name="applyCrewBrute">Optional; invoked as (tile, force) for each BFS visit.</param>
        /// <param name="applyCrewPush">Optional; invoked once per detonation as (epicenter, force per tile, summed over passes).</param>
        public BlastResolutionService(
            ITileQueryService query,
            IStructuralDamageService structural,
            Action<TileCoord, float> applyCrewBrute = null,
            Action<TileCoord, float> applyFloorDamage = null,
            Action<TileCoord, IReadOnlyDictionary<TileCoord, float>> applyCrewPush = null)
        {
            _query = query;
            _structural = structural;
            _applyCrewBrute = applyCrewBrute;
            _applyFloorDamage = applyFloorDamage;
            _applyCrewPush = applyCrewPush;
        }

        public void Resolve(TileCoord epicenter, float yield, float falloff)
            => FinishBlast(epicenter, ResolveInternal(epicenter, yield, falloff, Vector2.zero));

        /// <summary>Additional shaped-charge force in a 90-degree cone. Still respects barriers and plenum.</summary>
        public void ResolveCone(TileCoord epicenter, float yield, float falloff, Vector2 forward)
        {
            if (forward.sqrMagnitude < 0.01f) return;
            FinishBlast(epicenter, ResolveInternal(epicenter, yield, falloff, forward.normalized));
        }

        public void ResolveShaped(TileCoord epicenter, float radialYield, float radialFalloff,
            float coneYield, float coneFalloff, Vector2 forward)
        {
            Dictionary<TileCoord, float> forces = ResolveInternal(epicenter, radialYield, radialFalloff, Vector2.zero);
            if (forward.sqrMagnitude > 0.01f)
                foreach (KeyValuePair<TileCoord, float> entry in ResolveInternal(epicenter, coneYield, coneFalloff, forward.normalized))
                {
                    forces.TryGetValue(entry.Key, out float existing);
                    forces[entry.Key] = existing + entry.Value;
                }
            FinishBlast(epicenter, forces);
        }

        private void FinishBlast(TileCoord epicenter, Dictionary<TileCoord, float> forces)
        {
            if (_applyFloorDamage != null)
                foreach (KeyValuePair<TileCoord, float> entry in forces) _applyFloorDamage(entry.Key, entry.Value);
            if (forces.Count > 0) _applyCrewPush?.Invoke(epicenter, forces);
        }

        public static bool IsInCone(Vector2 offset, Vector2 forward) => offset.sqrMagnitude < 0.001f
            || Vector2.Dot(offset.normalized, forward.normalized) >= 0.707f;

        private Dictionary<TileCoord, float> ResolveInternal(TileCoord epicenter, float yield, float falloff, Vector2 forward)
        {
            if (_query == null || _structural == null || !float.IsFinite(yield) || !float.IsFinite(falloff)
                || yield < MinUsefulForce || falloff < 0f)
            {
                return new Dictionary<TileCoord, float>();
            }

            var bestForce = new Dictionary<TileCoord, float>();
            var queue = new Queue<(TileCoord Coord, float Force)>();
            queue.Enqueue((epicenter, yield));

            while (queue.Count > 0)
            {
                (TileCoord coord, float force) = queue.Dequeue();
                if (force < MinUsefulForce)
                {
                    continue;
                }

                if (bestForce.TryGetValue(coord, out float seen) && force <= seen)
                {
                    continue;
                }

                bestForce[coord] = force;

                _structural.TryApplyStructuralDamage(coord, force, StructuralDamageSource.Blast);
                _applyCrewBrute?.Invoke(coord, force);

                if (!_query.TryGetOccupancy(coord, out TileOccupancy fromOccupancy))
                {
                    continue;
                }

                for (int i = 0; i < Cardinals.Length; i++)
                {
                    Direction direction = Cardinals[i];
                    float nextForce = force - falloff;
                    if (nextForce < MinUsefulForce)
                    {
                        continue;
                    }

                    TileCoord neighbor = GetNeighbourCoord(coord, direction);
                    if (forward.sqrMagnitude > 0f && !IsInCone((Vector2)(neighbor.Grid - epicenter.Grid), forward))
                        continue;
                    if (!_query.TryGetOccupancy(neighbor, out TileOccupancy toOccupancy))
                    {
                        continue;
                    }

                    // Open hops only through station volume (plenum). Empty chunk cells report
                    // BlockedEdges=0 and would otherwise flank barriers through the void.
                    if (CanHop(fromOccupancy, toOccupancy, direction))
                    {
                        if (toOccupancy.HasPlenum)
                        {
                            queue.Enqueue((neighbor, nextForce));
                        }

                        continue;
                    }

                    // Blocked edge: spend force on the neighbor structural turf; cascade if it clears.
                    if (!_structural.TryApplyStructuralDamage(neighbor, nextForce, StructuralDamageSource.Blast))
                    {
                        continue;
                    }

                    if (!_query.TryGetOccupancy(coord, out fromOccupancy)
                        || !_query.TryGetOccupancy(neighbor, out toOccupancy))
                    {
                        continue;
                    }

                    if (CanHop(fromOccupancy, toOccupancy, direction) && toOccupancy.HasPlenum)
                    {
                        queue.Enqueue((neighbor, nextForce));
                    }
                }
            }
            return bestForce;
        }

        /// <summary>Same edge rules as atmos <c>CanFlow</c> — kept local to avoid Atmospherics.internal coupling.</summary>
        internal static bool CanHop(TileOccupancy from, TileOccupancy to, Direction direction)
        {
            int edgeIndex = DirectionToEdgeIndex(direction);
            if (edgeIndex < 0)
            {
                return false;
            }

            int oppositeEdge = (edgeIndex + 2) % 4;
            if ((from.BlockedEdges & (1 << edgeIndex)) != 0)
            {
                return false;
            }

            if ((to.BlockedEdges & (1 << oppositeEdge)) != 0)
            {
                return false;
            }

            return true;
        }

        internal static TileCoord GetNeighbourCoord(TileCoord coord, Direction direction)
        {
            Tuple<int, int> vector = TileHelper.ToCardinalVector(direction);
            return new TileCoord(coord.MapId, coord.Grid.x + vector.Item1, coord.Grid.y + vector.Item2);
        }

        private static int DirectionToEdgeIndex(Direction direction)
        {
            return direction switch
            {
                Direction.North => 0,
                Direction.East => 1,
                Direction.South => 2,
                Direction.West => 3,
                _ => -1,
            };
        }
    }
}
