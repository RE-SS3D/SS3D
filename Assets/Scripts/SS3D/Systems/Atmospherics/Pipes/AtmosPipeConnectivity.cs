using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Atmospherics.Pipes
{
    /// <summary>
    /// Discovers connected gas pipe network components via BFS over adjacency-respecting pipe segments.
    /// </summary>
    public static class AtmosPipeConnectivity
    {
        public static readonly TileLayer[] GasPipeLayers =
        {
            TileLayer.PipeLeft,
            TileLayer.PipeMiddle,
            TileLayer.PipeRight,
            TileLayer.PipeSurface,
        };

        public static bool IsGasPipeLayer(TileLayer layer)
        {
            for (int i = 0; i < GasPipeLayers.Length; i++)
            {
                if (GasPipeLayers[i] == layer)
                    return true;
            }

            return false;
        }

        public static HashSet<GasPipeSegmentKey> CollectNetworkSegments(TileMap map, PlacedTileObject seed)
        {
            var segments = new HashSet<GasPipeSegmentKey>();
            if (map == null || !PipeConnectionRule.ParticipatesInGasNetwork(seed))
                return segments;

            if (seed.Connector is not IEngineDrivenAdjacency engineDriven)
                return segments;

            IConnectionRule rule = engineDriven.ConnectionRule;
            if (rule == null)
                return segments;

            var visited = new HashSet<GasPipeSegmentKey>();
            var queue = new Queue<PlacedTileObject>();
            queue.Enqueue(seed);

            while (queue.Count > 0)
            {
                PlacedTileObject current = queue.Dequeue();
                GasPipeSegmentKey key = GasPipeSegmentKey.From(current);
                if (!visited.Add(key))
                    continue;

                segments.Add(key);

                AdjacencyMap adjacencyMap = AdjacencyEngine.ComputeAdjacencyMap(current, rule, map);
                PlacedTileObject[] neighbours = map.GetNeighbourPlacedObjects(current.Layer, current.transform.position);

                foreach (Direction direction in TileHelper.CardinalDirections())
                {
                    if (!adjacencyMap.HasConnection(direction))
                        continue;

                    PlacedTileObject neighbour = neighbours[(int)direction];
                    if (neighbour == null || !PipeConnectionRule.IsPipeSegmentConnected(current, neighbour))
                        continue;

                    queue.Enqueue(neighbour);
                }
            }

            return segments;
        }

        public static bool TryGetSegment(
            TileMap map,
            TileCoord coord,
            TileLayer layer,
            out PlacedTileObject segment)
        {
            segment = null;
            if (map == null || !IsGasPipeLayer(layer))
                return false;

            Vector3 world = new Vector3(coord.Grid.x, 0, coord.Grid.y);
            if (!map.TryGetTileLocation(layer, world, out ITileLocation location))
                return false;

            foreach (PlacedTileObject placedObject in location.GetAllPlacedObject())
            {
                if (placedObject != null && PipeConnectionRule.ParticipatesInGasNetwork(placedObject))
                {
                    segment = placedObject;
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<PlacedTileObject> EnumerateAllGasPipeSegments(TileMap map)
        {
            if (map == null)
                yield break;

            foreach (TileChunk chunk in map.GetAllChunks())
            {
                for (int localX = 0; localX < TileChunk.ChunkSize; localX++)
                {
                    for (int localY = 0; localY < TileChunk.ChunkSize; localY++)
                    {
                        Vector3 world = chunk.GetWorldPosition(localX, localY);
                        foreach (TileLayer layer in GasPipeLayers)
                        {
                            if (!map.TryGetTileLocation(layer, world, out ITileLocation location))
                                continue;

                            foreach (PlacedTileObject placedObject in location.GetAllPlacedObject())
                            {
                                if (placedObject != null && PipeConnectionRule.ParticipatesInGasNetwork(placedObject))
                                    yield return placedObject;
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<TileCoord> EnumerateCoordsAround(TileMap map, TileCoord coord)
        {
            yield return coord;

            foreach (Direction direction in TileHelper.CardinalDirections())
            {
                System.Tuple<int, int> offset = TileHelper.ToCardinalVector(direction);
                yield return new TileCoord(coord.MapId, coord.Grid.x + offset.Item1, coord.Grid.y + offset.Item2);
            }
        }
    }
}
