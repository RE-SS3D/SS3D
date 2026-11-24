using SS3D.Core;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using SS3D.Systems.Electricity;
using UnityEngine;

namespace SS3D.Systems.Electricity
{
    /// <summary>
    /// Resolves electric device neighbours through underfloor cable runs on <see cref="TileLayer.Wire"/>.
    /// Only grid backbone devices (generators, SMES, APC) participate in HV cable links;
    /// area consumers such as lights and vending machines are powered through their APC.
    /// </summary>
    public static class ElectricCableConnectivity
    {
        /// <summary>
        /// Whether the device may join the station HV cable graph.
        /// </summary>
        public static bool ParticipatesInCableGrid(IElectricDevice device) =>
            device is IPowerProducer or IPowerStorage;

        public static bool ParticipatesInCableGrid(PlacedTileObject tileObject) =>
            tileObject != null
            && tileObject.TryGetComponent(out IElectricDevice device)
            && ParticipatesInCableGrid(device);

        // Rebuild-scoped caches (see BeginRebuildCache): each cable run is walked once, and its grid devices
        // listed once, instead of once per grid device on it.
        private static int _cacheDepth;
        private static readonly Dictionary<Vector2Int, HashSet<Vector2Int>> RunByTile = new();
        private static readonly Dictionary<HashSet<Vector2Int>, List<PlacedTileObject>> GridDevicesByRun = new();

        /// <summary>
        /// While the returned scope is open, cable runs and the grid devices on them are computed once and shared.
        /// Only open it while the map cannot change, as in <c>ElectricitySubSystem.RebuildElectricGraph</c>: every grid
        /// device used to flood-fill its whole run, which cost about 0.4 s per rebuild on a big station.
        /// </summary>
        public static RebuildCacheScope BeginRebuildCache()
        {
            _cacheDepth++;
            return new RebuildCacheScope();
        }

        public readonly struct RebuildCacheScope : System.IDisposable
        {
            public void Dispose()
            {
                if (--_cacheDepth > 0)
                {
                    return;
                }

                _cacheDepth = 0;
                RunByTile.Clear();
                GridDevicesByRun.Clear();
            }
        }

        public static List<PlacedTileObject> GetCableLinkedDevices(PlacedTileObject deviceTile)
        {
            var linked = new List<PlacedTileObject>();
            if (deviceTile == null
                || !ParticipatesInCableGrid(deviceTile)
                || !SubSystems.TryGet(out TileSubSystem tileSubSystem))
            {
                return linked;
            }

            TileMap map = tileSubSystem.CurrentMap;
            if (map == null)
            {
                return linked;
            }

            HashSet<Vector2Int> networkTiles = CollectCableNetworkTiles(map, deviceTile.WorldOrigin);
            networkTiles.Add(deviceTile.WorldOrigin);

            if (_cacheDepth > 0 && RunByTile.ContainsKey(deviceTile.WorldOrigin))
            {
                if (!GridDevicesByRun.TryGetValue(networkTiles, out List<PlacedTileObject> onRun))
                {
                    onRun = CollectGridDevices(map, networkTiles, null);
                    GridDevicesByRun[networkTiles] = onRun;
                }

                foreach (PlacedTileObject device in onRun)
                {
                    if (device != deviceTile)
                    {
                        linked.Add(device);
                    }
                }

                return linked;
            }

            return CollectGridDevices(map, networkTiles, deviceTile);
        }

        /// <summary>The grid devices on <paramref name="networkTiles"/>, each once, except <paramref name="exclude"/>.</summary>
        private static List<PlacedTileObject> CollectGridDevices(TileMap map, HashSet<Vector2Int> networkTiles,
            PlacedTileObject exclude)
        {
            var linked = new List<PlacedTileObject>();
            PlacedTileObject deviceTile = exclude;

            var seen = new HashSet<PlacedTileObject>();
            foreach (Vector2Int grid in networkTiles)
            {
                foreach (PlacedTileObject tileObject in GetElectricDevicesOnTile(map, grid))
                {
                    if (tileObject == null
                        || tileObject == deviceTile
                        || !ParticipatesInCableGrid(tileObject)
                        || !seen.Add(tileObject))
                    {
                        continue;
                    }

                    linked.Add(tileObject);
                }
            }

            return linked;
        }

        /// <summary>Whether an HV cable lies on <paramref name="grid"/> (the Wire layer, a <see cref="CablesAdjacencyConnector"/>).</summary>
        public static bool HasCable(TileMap map, Vector2Int grid) => map != null && TryGetCableOnTile(map, grid, out _);

        /// <summary>
        /// The cable run through <paramref name="grid"/>: every cable tile joined to it side by side, by the walk that links
        /// grid devices (<see cref="GetCableLinkedDevices"/>); empty when no cable lies there.
        /// </summary>
        public static HashSet<Vector2Int> CollectCableRun(TileMap map, Vector2Int grid)
        {
            if (!HasCable(map, grid))
            {
                return new HashSet<Vector2Int>();
            }

            HashSet<Vector2Int> run = CollectCableNetworkTiles(map, grid);
            run.Add(grid);
            return run;
        }

        /// <summary>Every tile of <paramref name="map"/> with an HV cable on it.</summary>
        public static IEnumerable<Vector2Int> EnumerateCableTiles(TileMap map)
        {
            if (map == null)
            {
                yield break;
            }

            foreach (TileChunk chunk in map.GetAllChunks())
            {
                for (int x = 0; x < TileChunk.ChunkSize; x++)
                {
                    for (int y = 0; y < TileChunk.ChunkSize; y++)
                    {
                        Vector3 world = chunk.GetWorldPosition(x, y);
                        Vector2Int grid = new(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.z));
                        if (TryGetCableOnTile(map, grid, out _))
                        {
                            yield return grid;
                        }
                    }
                }
            }
        }

        private static HashSet<Vector2Int> CollectCableNetworkTiles(TileMap map, Vector2Int originGrid)
        {
            if (_cacheDepth > 0 && RunByTile.TryGetValue(originGrid, out HashSet<Vector2Int> cached))
            {
                return cached;
            }

            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();

            EnqueueCableNeighbours(map, originGrid, visited, queue);

            while (queue.Count > 0)
            {
                Vector2Int grid = queue.Dequeue();
                EnqueueCableNeighbours(map, grid, visited, queue);
            }

            // Cached only for a tile with a cable: then the run holds the origin too, so callers adding it change
            // nothing, and every tile of the run shares the one set.
            if (_cacheDepth > 0 && TryGetCableOnTile(map, originGrid, out _))
            {
                visited.Add(originGrid);
                foreach (Vector2Int tile in visited)
                {
                    RunByTile[tile] = visited;
                }
            }

            return visited;
        }

        private static void EnqueueCableNeighbours(
            TileMap map,
            Vector2Int grid,
            HashSet<Vector2Int> visited,
            Queue<Vector2Int> queue)
        {
            if (!TryGetCableOnTile(map, grid, out _))
            {
                return;
            }

            foreach (Direction direction in TileHelper.CardinalDirections())
            {
                Vector2Int neighbour = OffsetGrid(grid, direction);
                if (visited.Contains(neighbour) || !TryGetCableOnTile(map, neighbour, out _))
                {
                    continue;
                }

                visited.Add(neighbour);
                queue.Enqueue(neighbour);
            }
        }

        private static bool TryGetCableOnTile(TileMap map, Vector2Int grid, out PlacedTileObject cable)
        {
            cable = null;
            Vector3 world = new Vector3(grid.x, 0, grid.y);
            if (!map.TryGetTileLocation(TileLayer.Wire, world, out ITileLocation location))
            {
                return false;
            }

            foreach (PlacedTileObject placedObject in location.GetAllPlacedObject())
            {
                if (placedObject?.Connector is CablesAdjacencyConnector)
                {
                    cable = placedObject;
                    return true;
                }
            }

            return false;
        }

        private static List<PlacedTileObject> GetElectricDevicesOnTile(TileMap map, Vector2Int grid)
        {
            var devices = new List<PlacedTileObject>();
            Vector3 world = new Vector3(grid.x, 0, grid.y);
            TileChunk chunk = map.GetChunk(world);
            if (chunk == null)
            {
                return devices;
            }

            Vector2Int local = chunk.GetXY(world);
            List<ITileLocation> locations = chunk.GetTileLocations(local.x, local.y);
            foreach (ITileLocation location in locations)
            {
                if (location == null)
                {
                    continue;
                }

                foreach (PlacedTileObject tileObject in location.GetAllPlacedObject())
                {
                    if (tileObject != null && tileObject.TryGetComponent(out IElectricDevice _))
                    {
                        devices.Add(tileObject);
                    }
                }
            }

            return devices;
        }

        private static Vector2Int OffsetGrid(Vector2Int grid, Direction direction)
        {
            System.Tuple<int, int> offset = TileHelper.ToCardinalVector(direction);
            return new Vector2Int(grid.x + offset.Item1, grid.y + offset.Item2);
        }
    }
}
