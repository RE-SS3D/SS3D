using System.Collections.Generic;
using SS3D.Systems.Tile;
using UnityEngine;

namespace SS3D.Systems.Vision
{
    /// <summary>
    /// Chunk-aligned occlusion cache invalidated by tile mutations.
    /// </summary>
    public sealed class VisionOcclusionProvider : ITileMutationObserver
    {
        private readonly TileMap _map;
        private readonly ITileQueryService _query;
        private readonly Dictionary<Vector2Int, VisionChunkCache> _chunks = new();

        public VisionOcclusionProvider(TileMap map, ITileQueryService query)
        {
            _map = map;
            _query = query;
        }

        public bool IsBlocked(TileCoord coord)
        {
            if (!TryGetCachedCell(coord, out VisionCellCache cell))
                return false;

            return cell.BlocksVision;
        }

        public bool BlocksEdge(TileCoord from, Direction stepDirection)
        {
            if (!TileHelper.IsCardinal(stepDirection))
                return true;

            if (!TryGetCachedCell(from, out VisionCellCache fromCell))
                fromCell = default;

            TileCoord to = Neighbour(from, stepDirection);
            if (!TryGetCachedCell(to, out VisionCellCache toCell))
                toCell = default;

            int exitEdge = VisionEdgeMask.EdgeIndexForDirection(stepDirection);
            int enterEdge = VisionEdgeMask.EdgeIndexForDirection(TileHelper.GetOpposite(stepDirection));

            bool fromBlocksExit = exitEdge >= 0 && (fromCell.BlockedEdges & VisionEdgeMask.ForCardinal(exitEdge)) != 0;
            bool toBlocksEnter = enterEdge >= 0 && (toCell.BlockedEdges & VisionEdgeMask.ForCardinal(enterEdge)) != 0;

            return fromBlocksExit || toBlocksEnter;
        }

        public void OnTilePlaced(ITileOccupant occupant, TileCoord coord)
        {
            InvalidateAround(coord);
        }

        public void OnTileCleared(ITileOccupant occupant, TileCoord coord, TileLayer layer)
        {
            InvalidateAround(coord);
        }

        public void OnChunkCreated(TileChunkRef chunk)
        {
            _chunks.Remove(chunk.ChunkKey);
        }

        public void OnTileStateChanged(TileCoord coord)
        {
            InvalidateCell(coord);
        }

        private void InvalidateAround(TileCoord coord)
        {
            InvalidateCell(coord);

            foreach (Direction direction in TileHelper.AllDirections())
            {
                Vector2Int delta = TileHelper.CoordinateDifferenceInFrontFacingDirection(direction);
                InvalidateCell(new TileCoord(coord.MapId, coord.Grid.x + delta.x, coord.Grid.y + delta.y));
            }
        }

        private void InvalidateCell(TileCoord coord)
        {
            Vector2Int chunkKey = GetChunkKey(coord);
            if (!_chunks.TryGetValue(chunkKey, out VisionChunkCache chunk))
                return;

            Vector2Int local = GetLocalCoord(coord, chunkKey);
            chunk.Cells[local.x, local.y] = default;
            _chunks[chunkKey] = chunk;
        }

        private bool TryGetCachedCell(TileCoord coord, out VisionCellCache cell)
        {
            if (_map == null || coord.MapId != _map.MapId)
            {
                cell = default;
                return false;
            }

            Vector2Int chunkKey = GetChunkKey(coord);
            if (!_chunks.TryGetValue(chunkKey, out VisionChunkCache chunk))
            {
                chunk = new VisionChunkCache();
                _chunks[chunkKey] = chunk;
            }

            Vector2Int local = GetLocalCoord(coord, chunkKey);
            cell = chunk.Cells[local.x, local.y];

            if (cell.IsValid)
                return true;

            if (!_query.TryGetOccupancy(coord, out TileOccupancy occupancy))
            {
                cell = new VisionCellCache
                {
                    IsValid = true,
                    BlocksVision = false,
                    BlockedEdges = 0,
                };
            }
            else
            {
                cell = new VisionCellCache
                {
                    IsValid = true,
                    BlocksVision = occupancy.BlocksVision,
                    BlockedEdges = occupancy.BlockedEdges,
                };
            }

            chunk.Cells[local.x, local.y] = cell;
            return true;
        }

        private static TileCoord Neighbour(TileCoord coord, Direction direction)
        {
            Vector2Int delta = TileHelper.CoordinateDifferenceInFrontFacingDirection(direction);
            return new TileCoord(coord.MapId, coord.Grid.x + delta.x, coord.Grid.y + delta.y);
        }

        private static Vector2Int GetChunkKey(TileCoord coord)
        {
            int chunkSize = TileConstants.ChunkSize;
            return new Vector2Int(
                FloorDiv(coord.Grid.x, chunkSize),
                FloorDiv(coord.Grid.y, chunkSize));
        }

        private static Vector2Int GetLocalCoord(TileCoord coord, Vector2Int chunkKey)
        {
            int chunkSize = TileConstants.ChunkSize;
            return new Vector2Int(
                Mod(coord.Grid.x, chunkSize),
                Mod(coord.Grid.y, chunkSize));
        }

        private static int FloorDiv(int value, int divisor)
        {
            return value >= 0 ? value / divisor : (value - divisor + 1) / divisor;
        }

        private static int Mod(int value, int modulus)
        {
            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }

        private struct VisionCellCache
        {
            public bool IsValid;
            public bool BlocksVision;
            public byte BlockedEdges;
        }

        private sealed class VisionChunkCache
        {
            public readonly VisionCellCache[,] Cells =
                new VisionCellCache[TileConstants.ChunkSize, TileConstants.ChunkSize];
        }
    }
}
