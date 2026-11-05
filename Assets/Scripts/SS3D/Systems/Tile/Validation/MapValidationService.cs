using System.Collections.Generic;
using System.Linq;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Electricity;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Roles;
using UnityEngine;

namespace SS3D.Systems.Tile.Validation
{
    /// <summary>Validates only objects tracked by the current template, excluding inventory/preview objects.</summary>
    public static class MapValidationService
    {
        public static MapValidationReport Validate(TileSubSystem tiles, string mode)
        {
            if (tiles == null || tiles.CurrentMap == null)
            {
                MapValidationReport missing = new();
                missing.AddIssue("map", false, "Map", "No station map loaded",
                    "No station map is loaded. Load or create a map first.");
                return missing;
            }

            TileMap map = tiles.CurrentMap;
            PlacedTileObject[] tileObjects = map.GetAllChunks().SelectMany(chunk => chunk.GetAllTilePlacedObjects())
                .Where(placed => placed != null).ToArray();
            bool hasNuke = tileObjects.Any(placed => placed.GetComponentInChildren<Nuke>(true) != null);
            bool hasDisk = map.PlacedItems.Any(placed => placed != null
                && placed.GetComponentInChildren<NukeCard>(true) != null);
            string[] jobs = SubSystems.TryGet(out RoleSubSystem roles)
                ? roles.Jobs.Select(job => job.Name).ToArray() : System.Array.Empty<string>();
            MapValidationReport report = MapValidator.Validate(mode, jobs, tiles.SpawnPoints.Records, hasNuke, hasDisk);
            foreach (SpawnPoints.SpawnPointRecord spawn in tiles.SpawnPoints.Records)
            {
                if (!map.TryGetTileLocation(TileLayer.Plenum, spawn.Position, out ITileLocation floor)
                    || floor.IsFullyEmpty())
                {
                    Vector2Int cell = SpawnPoints.SpawnPointRegistry.TileKey(spawn.Position);
                    report.AddIssue($"floor:{cell.x}:{cell.y}:{spawn.Kind}:{spawn.JobName}", true, "Jobs", $"Spawn at ({cell.x}, {cell.y}) has no floor",
                        $"Spawn at ({cell.x}, {cell.y}) has no floor. Place flooring beneath it.",
                        spawn.Position, true);
                }
            }
            CheckRoomDevices(report, map, tileObjects);
            return report;
        }

        private static void CheckRoomDevices(MapValidationReport report, TileMap map, IEnumerable<PlacedTileObject> tileObjects)
        {
            if (!SubSystems.TryGet(out AreaSubSystem areas))
                return;

            List<RoomDeviceRecord> devices = new();
            foreach (PlacedTileObject placed in tileObjects)
            {
                if (!TryGetRoomDeviceKind(placed, out RoomDeviceKind kind))
                    continue;

                // The same lookup these devices use at runtime, so a wall mount counts for the room it faces.
                bool inRoom = areas.TryGetAreaForDevice(placed, out AreaRecord room) && !room.Id.IsNone;
                string roomName = !inRoom ? null
                    : string.IsNullOrWhiteSpace(room.DisplayName) ? "Unnamed room" : room.DisplayName;
                bool isPort = kind is RoomDeviceKind.Vent or RoomDeviceKind.Scrubber;
                devices.Add(new RoomDeviceRecord(kind, placed.transform.position, inRoom ? room.Id.Value : AreaId.None, roomName,
                    inRoom && areas.TryGetAreaApc(room.Id, out _),
                    !isPort || AtmosDevicePipeResolver.HasPipeSegment(map, new TileCoord(placed.MapId, placed.WorldOrigin))));
            }
            MapValidator.AddRoomDeviceChecks(report, devices, areas.HasAuthoredAreas);
        }

        private static bool TryGetRoomDeviceKind(PlacedTileObject placed, out RoomDeviceKind kind)
        {
            kind = default;
            if (placed.TryGetComponent(out VentController _))
                kind = RoomDeviceKind.Vent;
            else if (placed.TryGetComponent(out ScrubberController _))
                kind = RoomDeviceKind.Scrubber;
            else if (placed.TryGetComponent(out AirAlarmController _))
                kind = RoomDeviceKind.AirAlarm;
            else if (placed.TryGetComponent(out LightPower _) || placed.TryGetComponent(out LightSwitchController _))
                kind = RoomDeviceKind.Light;
            else
                return false;
            return true;
        }
    }
}
