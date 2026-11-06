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
            TileLayer.PipeManifold,
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

                if (current.TryGetComponent(out GasPipeManifold manifold))
                {
                    EnqueueAtManifoldPorts(map, manifold, key.Coord, queue);
                    continue;
                }

                EnqueueManifoldsBesidePipe(map, current, key.Coord, queue);

                if (current.Connector is not IEngineDrivenAdjacency engineDriven
                    || engineDriven.ConnectionRule is not { } rule)
                {
                    continue;
                }

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

        /// <summary>
        /// The manifold on a tile, if any.
        /// </summary>
        public static bool TryGetManifold(TileMap map, TileCoord coord, out GasPipeManifold manifold)
        {
            manifold = null;
            Vector3 world = new Vector3(coord.Grid.x, 0, coord.Grid.y);
            return map.TryGetTileLocation(TileLayer.PipeManifold, world, out ITileLocation location)
                && location.TryGetPlacedObject(out PlacedTileObject placedObject)
                && placedObject != null
                && placedObject.TryGetComponent(out manifold);
        }

        /// <summary>
        /// A manifold on this tile spanning <paramref name="line"/>, with a port facing <paramref name="toward"/>.
        /// </summary>
        public static bool TryGetManifoldPort(
            TileMap map,
            TileCoord coord,
            TileLayer line,
            Direction toward,
            out GasPipeManifold manifold) =>
            TryGetManifold(map, coord, out manifold) && manifold.Spans(line) && manifold.HasPortToward(toward);

        /// <summary>
        /// A device on this tile with a port facing <paramref name="toward"/> on <paramref name="line"/>,
        /// with a gap in that line for it. Devices sit on Surface, so an underfloor pipe never sees
        /// one as a same-layer neighbour.
        /// </summary>
        public static bool HasDevicePort(TileMap map, TileCoord coord, TileLayer line, Direction toward)
        {
            Vector3 world = new Vector3(coord.Grid.x, 0, coord.Grid.y);
            return map.TryGetTileLocation(TileLayer.PipeSurface, world, out ITileLocation location)
                && location.TryGetPlacedObject(out PlacedTileObject placedObject)
                && placedObject != null
                && placedObject.TryGetComponent(out IPipePortDevice device)
                && device.HasPortToward(toward)
                && System.Array.IndexOf(device.PortLines, line) >= 0
                && !TryGetSegment(map, coord, line, out _);
        }

        public static TileCoord Offset(TileCoord coord, Direction direction)
        {
            System.Tuple<int, int> offset = TileHelper.ToCardinalVector(direction);
            return new TileCoord(coord.MapId, coord.Grid.x + offset.Item1, coord.Grid.y + offset.Item2);
        }

        private static void EnqueueAtManifoldPorts(
            TileMap map,
            GasPipeManifold manifold,
            TileCoord coord,
            Queue<PlacedTileObject> queue)
        {
            foreach (Direction direction in TileHelper.CardinalDirections())
            {
                if (!manifold.HasPortToward(direction))
                    continue;

                TileCoord portCoord = Offset(coord, direction);
                for (int i = 0; i < GasPipeLayers.Length; i++)
                {
                    TileLayer line = GasPipeLayers[i];
                    if (!manifold.Spans(line))
                        continue;

                    if (TryGetSegment(map, portCoord, line, out PlacedTileObject segment))
                        queue.Enqueue(segment);

                    if (TryGetManifoldPort(map, portCoord, line, TileHelper.GetOpposite(direction), out GasPipeManifold next))
                        queue.Enqueue(next.PlacedObject);
                }
            }
        }

        private static void EnqueueManifoldsBesidePipe(
            TileMap map,
            PlacedTileObject pipe,
            TileCoord coord,
            Queue<PlacedTileObject> queue)
        {
            foreach (Direction direction in TileHelper.CardinalDirections())
            {
                if (TryGetManifoldPort(map, Offset(coord, direction), pipe.Layer, TileHelper.GetOpposite(direction), out GasPipeManifold manifold))
                    queue.Enqueue(manifold.PlacedObject);
            }
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

            // Pipe layers are single-occupancy; avoid GetAllPlacedObject() which allocates a List.
            if (!location.TryGetPlacedObject(out PlacedTileObject placedObject) || placedObject == null)
                return false;

            if (!PipeConnectionRule.ParticipatesInGasNetwork(placedObject))
                return false;

            segment = placedObject;
            return true;
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
                        for (int layerIndex = 0; layerIndex < GasPipeLayers.Length; layerIndex++)
                        {
                            TileLayer layer = GasPipeLayers[layerIndex];
                            if (!map.TryGetTileLocation(layer, world, out ITileLocation location))
                                continue;

                            if (!location.TryGetPlacedObject(out PlacedTileObject placedObject)
                                || placedObject == null
                                || !PipeConnectionRule.ParticipatesInGasNetwork(placedObject))
                            {
                                continue;
                            }

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
                yield return Offset(coord, direction);
        }
    }
}
