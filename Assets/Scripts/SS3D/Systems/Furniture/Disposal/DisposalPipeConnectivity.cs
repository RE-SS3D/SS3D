using SS3D.Systems.Furniture;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using UnityEngine;

namespace SS3D.Systems.Furniture.Disposal
{
    /// <summary>
    /// Discovers connected disposal pipe network components via BFS over adjacency-respecting pipe
    /// segments, same shape as <c>AtmosPipeConnectivity</c> for gas pipes.
    /// </summary>
    public static class DisposalPipeConnectivity
    {
        public static bool ParticipatesInDisposalNetwork(PlacedTileObject segment) =>
            segment != null
            && segment.Layer == TileLayer.Disposal
            && segment.GenericType == TileObjectGenericType.Disposal
            && segment.Connector is DisposalPipeAdjacencyConnector;

        /// <summary>
        /// Unlike atmos pipes, a disposal pipe's connection rule is stateful (it reads the pipe's own
        /// vertical/facing state), so the rule must be re-resolved from each segment visited during the
        /// walk rather than reused from the seed.
        /// </summary>
        public static bool IsPipeSegmentConnected(PlacedTileObject self, PlacedTileObject neighbour)
        {
            if (!ParticipatesInDisposalNetwork(self) || !ParticipatesInDisposalNetwork(neighbour))
                return false;

            if (self.Layer != neighbour.Layer)
                return false;

            if (self.Connector is not IEngineDrivenAdjacency engineDriven)
                return false;

            IConnectionRule selfRule = engineDriven.ConnectionRule;
            return selfRule != null && selfRule.IsConnected(self, neighbour);
        }

        /// <summary>
        /// Walks every pipe segment reachable from <paramref name="seed"/>, collecting both the pipe
        /// segments themselves and the terminal disposal elements (bins/outlets) sitting above any of them.
        /// </summary>
        public static DisposalNetworkWalkResult CollectNetwork(TileMap map, PlacedTileObject seed)
        {
            HashSet<DisposalSegmentKey> segments = new();
            List<IDisposalElement> terminals = new();

            if (map == null || !ParticipatesInDisposalNetwork(seed))
                return new DisposalNetworkWalkResult(segments, terminals);

            HashSet<GameObject> visitedTerminals = new();
            HashSet<DisposalSegmentKey> visited = new();
            Queue<PlacedTileObject> queue = new();
            queue.Enqueue(seed);

            while (queue.Count > 0)
            {
                PlacedTileObject current = queue.Dequeue();
                DisposalSegmentKey key = DisposalSegmentKey.From(current);
                if (!visited.Add(key))
                    continue;

                segments.Add(key);

                if (DisposalPipeConnectionRule.TryGetDisposalElementAbovePipe(map, current, out IDisposalElement element)
                    && visitedTerminals.Add(element.GameObject))
                {
                    terminals.Add(element);
                }

                if (current.Connector is not IEngineDrivenAdjacency currentEngineDriven)
                    continue;

                IConnectionRule rule = currentEngineDriven.ConnectionRule;
                if (rule == null)
                    continue;

                AdjacencyMap adjacencyMap = AdjacencyEngine.ComputeAdjacencyMap(current, rule, map);
                PlacedTileObject[] neighbours = map.GetNeighbourPlacedObjects(current.Layer, current.transform.position);

                foreach (Direction direction in TileHelper.CardinalDirections())
                {
                    if (!adjacencyMap.HasConnection(direction))
                        continue;

                    PlacedTileObject neighbour = neighbours[(int)direction];
                    if (neighbour == null || !IsPipeSegmentConnected(current, neighbour))
                        continue;

                    queue.Enqueue(neighbour);
                }
            }

            return new DisposalNetworkWalkResult(segments, terminals);
        }

        /// <summary>
        /// BFS from <paramref name="entrySegment"/> to the pipe segment sitting under
        /// <paramref name="destination"/>, returning the ordered chain of segments walked.
        /// Used to build a capsule's travel route (design doc §5).
        /// </summary>
        public static bool TryFindRoute(
            TileMap map,
            PlacedTileObject entrySegment,
            IDisposalElement destination,
            out List<PlacedTileObject> path)
        {
            path = null;
            if (map == null || destination == null || !ParticipatesInDisposalNetwork(entrySegment))
                return false;

            Dictionary<DisposalSegmentKey, PlacedTileObject> visited = new();
            Dictionary<DisposalSegmentKey, PlacedTileObject> parent = new();
            Queue<PlacedTileObject> queue = new();
            queue.Enqueue(entrySegment);
            visited[DisposalSegmentKey.From(entrySegment)] = entrySegment;

            PlacedTileObject found = null;

            while (queue.Count > 0 && found == null)
            {
                PlacedTileObject current = queue.Dequeue();

                if (DisposalPipeConnectionRule.TryGetDisposalElementAbovePipe(map, current, out IDisposalElement element)
                    && element.GameObject == destination.GameObject)
                {
                    found = current;
                    break;
                }

                if (current.Connector is not IEngineDrivenAdjacency currentEngineDriven)
                    continue;

                IConnectionRule rule = currentEngineDriven.ConnectionRule;
                if (rule == null)
                    continue;

                AdjacencyMap adjacencyMap = AdjacencyEngine.ComputeAdjacencyMap(current, rule, map);
                PlacedTileObject[] neighbours = map.GetNeighbourPlacedObjects(current.Layer, current.transform.position);

                foreach (Direction direction in TileHelper.CardinalDirections())
                {
                    if (!adjacencyMap.HasConnection(direction))
                        continue;

                    PlacedTileObject neighbour = neighbours[(int)direction];
                    if (neighbour == null || !IsPipeSegmentConnected(current, neighbour))
                        continue;

                    DisposalSegmentKey neighbourKey = DisposalSegmentKey.From(neighbour);
                    if (visited.ContainsKey(neighbourKey))
                        continue;

                    visited[neighbourKey] = neighbour;
                    parent[neighbourKey] = current;
                    queue.Enqueue(neighbour);
                }
            }

            if (found == null)
                return false;

            List<PlacedTileObject> reversed = new() { found };
            DisposalSegmentKey walkKey = DisposalSegmentKey.From(found);
            while (parent.TryGetValue(walkKey, out PlacedTileObject previous))
            {
                reversed.Add(previous);
                walkKey = DisposalSegmentKey.From(previous);
            }

            reversed.Reverse();
            path = reversed;
            return true;
        }

        public static bool TryGetSegment(TileMap map, TileCoord coord, out PlacedTileObject segment)
        {
            segment = null;
            if (map == null)
                return false;

            Vector3 world = new Vector3(coord.Grid.x, 0, coord.Grid.y);
            if (!map.TryGetTileLocation(TileLayer.Disposal, world, out ITileLocation location))
                return false;

            foreach (PlacedTileObject placedObject in location.GetAllPlacedObject())
            {
                if (placedObject != null && ParticipatesInDisposalNetwork(placedObject))
                {
                    segment = placedObject;
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<PlacedTileObject> EnumerateAllDisposalPipeSegments(TileMap map)
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
                        if (!map.TryGetTileLocation(TileLayer.Disposal, world, out ITileLocation location))
                            continue;

                        foreach (PlacedTileObject placedObject in location.GetAllPlacedObject())
                        {
                            if (placedObject != null && ParticipatesInDisposalNetwork(placedObject))
                                yield return placedObject;
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

    /// <summary>
    /// Result of a disposal pipe network BFS: the segments visited and the terminal disposal
    /// elements (bins/outlets) reachable through them.
    /// </summary>
    public readonly struct DisposalNetworkWalkResult
    {
        public readonly HashSet<DisposalSegmentKey> Segments;
        public readonly List<IDisposalElement> Terminals;

        public DisposalNetworkWalkResult(HashSet<DisposalSegmentKey> segments, List<IDisposalElement> terminals)
        {
            Segments = segments;
            Terminals = terminals;
        }
    }
}
