using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Tile
{
    /// <summary>
    /// Server-side read-only facade over <see cref="TileMap"/>.
    /// </summary>
    public sealed class TileQueryService : ITileQueryService
    {
        private readonly TileMap _map;

        public TileQueryService(TileMap map)
        {
            _map = map;
        }

        public bool TryGetOccupant(TileCoord coord, TileLayer layer, Direction dir, out ITileOccupant occupant)
        {
            occupant = null;
            if (_map == null || coord.MapId != _map.MapId)
                return false;

            Vector3 world = TileToWorld(coord);
            if (!_map.TryGetTileLocation(layer, world, out ITileLocation location))
                return false;

            if (!location.TryGetPlacedObject(out PlacedTileObject placed, dir))
                return false;

            occupant = placed;
            return true;
        }

        public bool TryGetOccupancy(TileCoord coord, out TileOccupancy occupancy)
        {
            occupancy = default;
            if (_map == null || coord.MapId != _map.MapId)
                return false;

            Vector3 world = TileToWorld(coord);
            if (!_map.TryGetTileLocations(world, out ITileLocation[] locations))
                return false;

            occupancy.HasPlenum = !locations[(int)TileLayer.Plenum].IsFullyEmpty();
            occupancy.HasTurf = !locations[(int)TileLayer.Turf].IsFullyEmpty();

            TileOccupancyEvaluator.Evaluate(_map, locations, ref occupancy);

            return true;
        }

        public IReadOnlyList<ITileOccupant> GetNeighbours(TileCoord coord, TileLayer layer)
        {
            var neighbours = new List<ITileOccupant>();
            if (_map == null || coord.MapId != _map.MapId)
                return neighbours;

            Vector3 world = TileToWorld(coord);
            foreach (PlacedTileObject neighbour in _map.GetCardinalNeighbourPlacedObjects(layer, world))
            {
                if (neighbour != null)
                    neighbours.Add(neighbour);
            }

            return neighbours;
        }

        public bool TryFindFreeTile(TileCoord near, TileLayer layer, out TileCoord free)
        {
            free = default;
            if (_map == null || near.MapId != _map.MapId)
                return false;

            Vector3 origin = TileToWorld(near);
            for (int radius = 1; radius <= 8; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dz = -radius; dz <= radius; dz++)
                    {
                        if (Mathf.Abs(dx) != radius && Mathf.Abs(dz) != radius)
                            continue;

                        Vector3 candidate = origin + new Vector3(dx, 0, dz);
                        TileCoord candidateCoord = WorldToTile(candidate, near.MapId);

                        if (candidateCoord.MapId == near.MapId && candidateCoord.Grid == near.Grid)
                            continue;

                        if (TryGetOccupant(candidateCoord, layer, Direction.North, out _))
                            continue;

                        if (!TryGetOccupancy(candidateCoord, out TileOccupancy occupancy) || !occupancy.HasPlenum)
                            continue;

                        free = candidateCoord;
                        return true;
                    }
                }
            }

            return false;
        }

        public TileCoord WorldToTile(Vector3 world, int mapId = 0)
        {
            return new TileCoord(mapId, Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));
        }

        public Vector3 TileToWorld(TileCoord coord)
        {
            return new Vector3(coord.Grid.x, 0, coord.Grid.y);
        }
    }
}
