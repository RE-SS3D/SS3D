using SS3D.Systems.Atmospherics.ECS;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace SS3D.Systems.Atmospherics
{
    internal static class AtmosNeighbourBuilder
    {
        private static readonly Direction[] CardinalDirections =
        {
            Direction.North, Direction.East, Direction.South, Direction.West,
        };

        public static bool CanFlow(TileOccupancy from, TileOccupancy to, Direction direction)
        {
            int edgeIndex = DirectionToEdgeIndex(direction);
            int oppositeEdge = (edgeIndex + 2) % 4;

            if ((from.BlockedEdges & (1 << edgeIndex)) != 0)
                return false;

            if ((to.BlockedEdges & (1 << oppositeEdge)) != 0)
                return false;

            return true;
        }

        public static TileCoord GetNeighbourCoord(TileCoord coord, Direction direction)
        {
            System.Tuple<int, int> vector = TileHelper.ToCardinalVector(direction);
            return new TileCoord(coord.MapId, coord.Grid.x + vector.Item1, coord.Grid.y + vector.Item2);
        }

        public static int DirectionToEdgeIndex(Direction direction)
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

        public static void RebuildNeighbours(
            int cellIndex,
            TileCoord coord,
            IReadOnlyDictionary<TileCoord, int> coordToIndex,
            ITileQueryService query,
            NativeArray<AtmosNeighbours> neighbours)
        {
            if (!query.TryGetOccupancy(coord, out TileOccupancy selfOccupancy))
                return;

            AtmosNeighbours links = default;

            for (int directionIndex = 0; directionIndex < CardinalDirections.Length; directionIndex++)
            {
                Direction direction = CardinalDirections[directionIndex];
                TileCoord neighbourCoord = GetNeighbourCoord(coord, direction);

                if (!coordToIndex.TryGetValue(neighbourCoord, out int neighbourIndex) ||
                    !query.TryGetOccupancy(neighbourCoord, out TileOccupancy neighbourOccupancy) ||
                    !CanFlow(selfOccupancy, neighbourOccupancy, direction))
                {
                    links.Set(directionIndex, -1);
                    continue;
                }

                links.Set(directionIndex, neighbourIndex);
            }

            neighbours[cellIndex] = links;
        }
    }
}
