using SS3D.Systems.Tile;
using System.Collections.Generic;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// The room around a tile as gas sees it: the same edges the turf simulation flows through, except that
    /// a door always bounds the room, open or not. Map Check and room air read the same answer.
    /// </summary>
    public static class AtmosRoomFlood
    {
        /// <summary>Past this a "room" is the outside or the whole station.</summary>
        public const int MaxCells = 4096;

        private static readonly Direction[] Cardinals = { Direction.North, Direction.East, Direction.South, Direction.West };

        /// <summary>Collects the room's cells into <paramref name="cells"/>.</summary>
        /// <returns>False if the room is open to space, or too large to be a room.</returns>
        public static bool Collect(ITileQueryService query, TileCoord start, List<TileCoord> cells)
        {
            cells.Clear();
            if (!query.TryGetOccupancy(start, out TileOccupancy first) || !first.HasPlenum || !IsRoomCell(first))
                return false;

            bool isSealed = true;
            var visited = new HashSet<TileCoord> { start };
            var queue = new Queue<TileCoord>();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                TileCoord current = queue.Dequeue();
                cells.Add(current);
                if (cells.Count >= MaxCells)
                    return false;

                query.TryGetOccupancy(current, out TileOccupancy from);
                foreach (Direction direction in Cardinals)
                {
                    TileCoord next = AtmosNeighbourBuilder.GetNeighbourCoord(current, direction);
                    bool known = query.TryGetOccupancy(next, out TileOccupancy to);
                    if (known && !IsRoomCell(to))
                        continue;

                    if (!known || !to.HasPlenum)
                        isSealed = false;
                    else if (AtmosNeighbourBuilder.CanFlow(from, to, direction) && visited.Add(next))
                        queue.Enqueue(next);
                }
            }

            return isSealed;
        }

        private static bool IsRoomCell(TileOccupancy occupancy) =>
            !occupancy.IsDoor && !(occupancy.HasWall && occupancy.IsAirtight);
    }
}
