using SS3D.Systems.Tile;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.StructuralDamage
{
    /// <summary>
    /// Blast spread, strongest force first: open hops via BlockedEdges (same rules as atmos CanFlow) through plenum
    /// volume only, structural force on blocking turf, cascade when Destroyed clears an edge. Diagonal hops cost √2 of
    /// a straight one, so open-space force contours are near-circular octagons rather than the diamonds a four-way
    /// spread draws; a diagonal needs both straight routes round its corner open, so it never slips between walls.
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
        private readonly float _roughness;

        /// <summary>Force a diagonal hop costs, in straight hops.</summary>
        public const float DiagonalCost = 1.41421356f;

        /// <summary>
        /// Default per-hop jitter for game blasts (<see cref="StructuralDamageSubSystem"/>): each hop's falloff varies by up
        /// to this share, fixed per tile and epicenter, so the edge of a big blast comes out ragged rather than a
        /// clean polygon. Tests leave it at 0 for exact forces.
        /// </summary>
        public const float GameRoughness = 0.12f;

        /// <summary>
        /// Hops into tiles this close to the epicenter (straight-line tiles) are never jittered: what a charge does at
        /// point blank, such as force 90 knocking someone down, stays exact. Only the edge frays.
        /// </summary>
        public const float SmoothRadius = 2f;

        /// <param name="applyCrewBrute">
        /// Optional; invoked once per tile per detonation as (tile, final force), after every pass, so a body takes one hit
        /// from one blast (tech-debt P2.5). It used to run on every BFS visit, so a shaped charge's two passes hit twice.
        /// </param>
        /// <param name="applyCrewPush">Optional; invoked once per detonation as (epicenter, force per tile, summed over passes).</param>
        /// <param name="roughness">Per-hop falloff jitter, 0 for none; see <see cref="GameRoughness"/>.</param>
        public BlastResolutionService(
            ITileQueryService query,
            IStructuralDamageService structural,
            Action<TileCoord, float> applyCrewBrute = null,
            Action<TileCoord, float> applyFloorDamage = null,
            Action<TileCoord, IReadOnlyDictionary<TileCoord, float>> applyCrewPush = null,
            float roughness = 0f)
        {
            _roughness = Mathf.Clamp(roughness, 0f, 0.5f);
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
            if (_applyCrewBrute != null)
                foreach (KeyValuePair<TileCoord, float> entry in forces) _applyCrewBrute(entry.Key, entry.Value);
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
            var queue = new ForceHeap();
            queue.Push(epicenter, yield);

            while (queue.Count > 0)
            {
                (TileCoord coord, float force) = queue.Pop();
                if (force < MinUsefulForce)
                {
                    continue;
                }

                // Strongest first, so the first visit is the best one: each tile is hit once, at its full force.
                if (bestForce.ContainsKey(coord))
                {
                    continue;
                }

                bestForce[coord] = force;

                _structural.TryApplyStructuralDamage(coord, force, StructuralDamageSource.Blast);

                if (!_query.TryGetOccupancy(coord, out TileOccupancy fromOccupancy))
                {
                    continue;
                }

                for (int i = 0; i < Cardinals.Length; i++)
                {
                    Direction direction = Cardinals[i];
                    TileCoord neighbor = GetNeighbourCoord(coord, direction);
                    float nextForce = force - falloff * HopCost(epicenter, neighbor, 1f);
                    if (nextForce < MinUsefulForce || bestForce.ContainsKey(neighbor))
                    {
                        continue;
                    }

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
                            queue.Push(neighbor, nextForce);
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
                        queue.Push(neighbor, nextForce);
                    }
                }

                // Diagonals only travel through open space; they never strike walls, which straight hops do.
                for (int i = 0; i < Diagonals.Length; i++)
                {
                    (Direction dx, Direction dy) = Diagonals[i];
                    TileCoord across = GetNeighbourCoord(GetNeighbourCoord(coord, dx), dy);
                    float nextForce = force - falloff * HopCost(epicenter, across, DiagonalCost);
                    if (nextForce < MinUsefulForce || bestForce.ContainsKey(across))
                    {
                        continue;
                    }

                    if (forward.sqrMagnitude > 0f && !IsInCone((Vector2)(across.Grid - epicenter.Grid), forward))
                        continue;
                    if (CanCrossDiagonally(coord, fromOccupancy, dx, dy))
                    {
                        queue.Push(across, nextForce);
                    }
                }
            }
            return bestForce;
        }

        private static readonly (Direction, Direction)[] Diagonals =
        {
            (Direction.East, Direction.North), (Direction.East, Direction.South),
            (Direction.West, Direction.North), (Direction.West, Direction.South),
        };

        /// <summary>
        /// A diagonal hop is open when both straight routes round the corner are: from → side → across, for each side,
        /// with plenum on all three tiles. So two walls meeting at a corner still seal it.
        /// </summary>
        private bool CanCrossDiagonally(TileCoord from, TileOccupancy fromOccupancy, Direction dx, Direction dy)
        {
            TileCoord sideX = GetNeighbourCoord(from, dx);
            TileCoord sideY = GetNeighbourCoord(from, dy);
            TileCoord across = GetNeighbourCoord(sideX, dy);
            return _query.TryGetOccupancy(sideX, out TileOccupancy x) && x.HasPlenum
                && _query.TryGetOccupancy(sideY, out TileOccupancy y) && y.HasPlenum
                && _query.TryGetOccupancy(across, out TileOccupancy a) && a.HasPlenum
                && CanHop(fromOccupancy, x, dx) && CanHop(x, a, dy)
                && CanHop(fromOccupancy, y, dy) && CanHop(y, a, dx);
        }

        /// <summary>
        /// Falloff multiplier for the hop into <paramref name="to"/>: its length, jittered by up to the roughness, fixed
        /// per tile and epicenter so a blast is deterministic but no two have the same edge.
        /// </summary>
        private float HopCost(TileCoord epicenter, TileCoord to, float length)
        {
            if (_roughness <= 0f || ((Vector2)(to.Grid - epicenter.Grid)).sqrMagnitude <= SmoothRadius * SmoothRadius)
            {
                return length;
            }

            unchecked
            {
                uint h = (uint)(to.Grid.x * 73856093) ^ (uint)(to.Grid.y * 19349663)
                    ^ (uint)(epicenter.Grid.x * 83492791) ^ (uint)(epicenter.Grid.y * 2654435761u);
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                float jitter = (h & 0xFFFF) / 65535f * 2f - 1f;
                return length * (1f + _roughness * jitter);
            }
        }

        /// <summary>Max-heap on force: the spread always takes the strongest open tile next.</summary>
        private sealed class ForceHeap
        {
            private readonly List<(TileCoord Coord, float Force)> _items = new();

            public int Count => _items.Count;

            public void Push(TileCoord coord, float force)
            {
                _items.Add((coord, force));
                int i = _items.Count - 1;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (_items[parent].Force >= _items[i].Force)
                        break;
                    (_items[parent], _items[i]) = (_items[i], _items[parent]);
                    i = parent;
                }
            }

            public (TileCoord Coord, float Force) Pop()
            {
                (TileCoord, float) top = _items[0];
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _items.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int left = i * 2 + 1, right = left + 1, largest = i;
                    if (left < _items.Count && _items[left].Force > _items[largest].Force) largest = left;
                    if (right < _items.Count && _items[right].Force > _items[largest].Force) largest = right;
                    if (largest == i)
                        break;
                    (_items[largest], _items[i]) = (_items[i], _items[largest]);
                    i = largest;
                }

                return top;
            }
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
