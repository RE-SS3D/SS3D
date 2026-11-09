using SS3D.Core;
using SS3D.Logging;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Server: puts back anything found outside <see cref="StationBounds"/> (above the ceiling or below the deck).
    /// The ceiling should make this rare, so every rescue logs a warning: it marks a way out that needs fixing.
    /// </summary>
    public static class StationBoundsRescue
    {
        /// <summary>How many rings of tiles around the spot are searched for floor to stand on.</summary>
        private const int SearchRadius = 8;

        public static bool IsOutOfBounds(Vector3 position) =>
            position.y > StationBounds.EscapedAbove || position.y < StationBounds.FloorLimit;

        /// <summary>
        /// Where something found at <paramref name="position"/> goes: the deck at the middle of the nearest walkable
        /// floor tile (floor, no wall or door), or, over open space, the same spot at deck height. False without a map.
        /// </summary>
        public static bool TryGetRescuePosition(Vector3 position, out Vector3 rescue)
        {
            rescue = default;
            if (!SubSystems.TryGet(out TileSubSystem tiles)
                || tiles.CurrentMap == null
                || tiles.QueryService == null)
            {
                return false;
            }

            ITileQueryService query = tiles.QueryService;
            TileCoord near = query.WorldToTile(position, tiles.CurrentMap.MapId);
            float deck = query.TileToWorld(near).y;
            rescue = new Vector3(position.x, deck, position.z);
            if (!query.TryGetOccupancy(near, out TileOccupancy here) || !here.HasPlenum)
            {
                return true;
            }

            for (int radius = 0; radius <= SearchRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dz) != radius)
                        {
                            continue;
                        }

                        TileCoord candidate = new(near.MapId, near.Grid.x + dx, near.Grid.y + dz);
                        if (query.TryGetOccupancy(candidate, out TileOccupancy occupancy)
                            && occupancy.HasPlenum && !occupancy.HasWall && !occupancy.IsDoor)
                        {
                            rescue = query.TileToWorld(candidate);
                            return true;
                        }
                    }
                }
            }

            return true;
        }

        public static void LogRescue(Object target, Vector3 from, Vector3 velocity, Vector3 to) =>
            Log.Warning(typeof(StationBoundsRescue),
                "Moved {name} back into the station: found at {from} moving {velocity}, put at {to}",
                Logs.Important, target.name, from, velocity, to);
    }
}
