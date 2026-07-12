using System.Collections.Generic;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Area
{
    public sealed class AreaFloodFillService
    {
        private static readonly Direction[] CardinalDirections =
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West,
        };

        private readonly TileMap _map;
        private readonly ITileQueryService _query;

        public AreaFloodFillService(TileMap map, ITileQueryService query)
        {
            _map = map;
            _query = query;
        }

        public void FloodFromApc(IAreaApcOrigin apc, AreaId areaId, HashSet<TileCoord> claimedTiles)
        {
            if (apc == null || areaId.IsNone)
                return;

            TileCoord origin = apc.OriginTile;
            IReadOnlyList<TileCoord> seeds = CollectFloodSeeds(apc);
            if (seeds.Count == 0)
                return;

            var queue = new Queue<TileCoord>();
            foreach (TileCoord seed in seeds)
                queue.Enqueue(seed);

            while (queue.Count > 0)
            {
                TileCoord current = queue.Dequeue();

                if (claimedTiles.Contains(current))
                    continue;

                _map.TryGetAreaId(current, out ushort existingAreaId);

                if (existingAreaId != AreaId.None && existingAreaId != areaId.Value)
                    continue;

                if (!AreaBoundaryEvaluator.IsWalkable(_query, current))
                    continue;

                _map.TrySetAreaId(current, areaId.Value);
                claimedTiles.Add(current);

                foreach (Direction direction in CardinalDirections)
                {
                    TileCoord neighbour = OffsetCardinal(current, direction);
                    if (claimedTiles.Contains(neighbour))
                        continue;

                    _map.TryGetAreaId(neighbour, out ushort neighbourAreaId);
                    if (AreaBoundaryEvaluator.BlocksAreaExpansion(current, neighbour, _query, areaId, neighbourAreaId))
                        continue;

                    queue.Enqueue(neighbour);
                }
            }

            AssignApcOriginTile(origin, areaId);
        }

        public void AssignDoorTileAreas()
        {
            foreach (TileChunk chunk in _map.GetAllChunks())
            {
                for (int x = 0; x < TileChunk.ChunkSize; x++)
                {
                    for (int y = 0; y < TileChunk.ChunkSize; y++)
                    {
                        Vector3 world = chunk.GetWorldPosition(x, y);
                        TileCoord coord = _query.WorldToTile(world, _map.MapId);

                        if (!AreaBoundaryEvaluator.IsTurfDoor(_query, coord))
                            continue;

                        if (!TryGetFirstNeighbourAreaId(coord, out ushort neighbourAreaId))
                            continue;

                        _map.TrySetAreaId(coord, neighbourAreaId);
                    }
                }
            }
        }

        public void ClearAreaTiles(AreaId areaId)
        {
            if (areaId.IsNone)
                return;

            foreach (TileChunk chunk in _map.GetAllChunks())
            {
                for (int x = 0; x < TileChunk.ChunkSize; x++)
                {
                    for (int y = 0; y < TileChunk.ChunkSize; y++)
                    {
                        if (chunk.GetAreaId(x, y) == areaId.Value)
                            chunk.SetAreaId(x, y, AreaId.None);
                    }
                }
            }
        }

        private IReadOnlyList<TileCoord> CollectFloodSeeds(IAreaApcOrigin apc)
        {
            var seeds = new List<TileCoord>();
            TileCoord origin = apc.OriginTile;

            if (AreaBoundaryEvaluator.IsWalkable(_query, origin))
            {
                seeds.Add(origin);
                return seeds;
            }

            Vector2Int offset = TileHelper.CoordinateDifferenceInFrontFacingDirection(apc.FacingDirection);
            TileCoord inFront = new TileCoord(origin.MapId, origin.Grid.x + offset.x, origin.Grid.y + offset.y);
            if (AreaBoundaryEvaluator.IsWalkable(_query, inFront))
                seeds.Add(inFront);

            return seeds;
        }

        private void AssignApcOriginTile(TileCoord origin, AreaId areaId)
        {
            if (!_map.TryGetAreaId(origin, out ushort originAreaId))
                return;

            if (originAreaId != AreaId.None && originAreaId != areaId.Value)
                return;

            _map.TrySetAreaId(origin, areaId.Value);
        }

        private bool TryGetFirstNeighbourAreaId(TileCoord coord, out ushort areaId)
        {
            areaId = AreaId.None;

            foreach (Direction direction in CardinalDirections)
            {
                TileCoord neighbour = OffsetCardinal(coord, direction);
                if (!_map.TryGetAreaId(neighbour, out ushort neighbourAreaId) || neighbourAreaId == AreaId.None)
                    continue;

                areaId = neighbourAreaId;
                return true;
            }

            return false;
        }

        private static TileCoord OffsetCardinal(TileCoord coord, Direction direction)
        {
            (int dx, int dz) = direction switch
            {
                Direction.North => (0, 1),
                Direction.East => (1, 0),
                Direction.South => (0, -1),
                Direction.West => (-1, 0),
                _ => (0, 0),
            };

            return new TileCoord(coord.MapId, coord.Grid.x + dx, coord.Grid.y + dz);
        }
    }
}
