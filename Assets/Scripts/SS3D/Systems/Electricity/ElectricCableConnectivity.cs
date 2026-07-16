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

        private static HashSet<Vector2Int> CollectCableNetworkTiles(TileMap map, Vector2Int originGrid)
        {
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();

            EnqueueCableNeighbours(map, originGrid, visited, queue);

            while (queue.Count > 0)
            {
                Vector2Int grid = queue.Dequeue();
                EnqueueCableNeighbours(map, grid, visited, queue);
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
