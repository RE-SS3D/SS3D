using System.Collections.Generic;
using System.Linq;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Electricity;
using SS3D.Systems.Furniture;
using SS3D.Systems.Inventory.Items.Generic;
using SS3D.Systems.Roles;
using SS3D.Systems.Tile.GasMarkers;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;

namespace SS3D.Systems.Tile.Validation
{
    /// <summary>Validates only objects tracked by the current template, excluding inventory/preview objects.</summary>
    public static class MapValidationService
    {
        /// <param name="includeSystemsViews">
        /// Add the systems views' problems (Phase 4: pipes, power), read from the server's live networks through the map
        /// editor's sources. Round start passes false: they are warnings only, and right after a template load the
        /// networks are not rebuilt yet (a source that isn't current reports nothing anyway).
        /// </param>
        public static MapValidationReport Validate(TileSubSystem tiles, string mode, bool includeSystemsViews = true)
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
            CheckRoomDevices(report, tiles, map, tileObjects);
            CheckGasMarkerRooms(report, tiles, map);
            if (includeSystemsViews && SubSystems.TryGet(out MapEditorSubSystem editor))
            {
                List<SystemsViewIssue> issues = new();
                editor.SystemsViews.Analyze(issues);
                MapValidator.AddSystemsIssues(report, issues);
            }

            return report;
        }

        private static void CheckGasMarkerRooms(MapValidationReport report, TileSubSystem tiles, TileMap map)
        {
            List<TileCoord> cells = new();
            foreach (GasMarkerRecord marker in tiles.GasMarkers.Records)
            {
                if (marker.Kind == GasMarkerKind.LineFill)
                    continue;

                if (!AtmosRoomFlood.Collect(tiles.QueryService, tiles.QueryService.WorldToTile(marker.Position, map.MapId), cells))
                    MapValidator.AddUnsealedGasMarker(report, marker);
            }
        }

        private static void CheckRoomDevices(MapValidationReport report, TileSubSystem tiles, TileMap map, IEnumerable<PlacedTileObject> tileObjects)
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
                    : SystemsViewProblems.RoomName(room.DisplayName);
                devices.Add(new RoomDeviceRecord(kind, placed.transform.position, inRoom ? room.Id.Value : AreaId.None, roomName));
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
            else if (placed.TryGetComponent(out AtmosPumpController _))
                kind = RoomDeviceKind.GasPump;
            else if (placed.TryGetComponent(out AtmosValveController _))
                kind = RoomDeviceKind.Valve;
            else if (placed.TryGetComponent(out AtmosThreeWayDeviceBase _))
                kind = RoomDeviceKind.ThreeWay;
            else if (placed.TryGetComponent(out AtmosMeterController _) || placed.TryGetComponent(out AtmosInjectorController _))
                kind = RoomDeviceKind.LineReader;
            else if (placed.TryGetComponent(out AirAlarmController _))
                kind = RoomDeviceKind.AirAlarm;
            else if (placed.TryGetComponent(out LightPower _) || placed.TryGetComponent(out LightSwitchController _))
                kind = RoomDeviceKind.Light;
            else if (placed.TryGetComponent(out IAreaApcOrigin _))
                kind = RoomDeviceKind.Apc;
            else
                return false;
            return true;
        }
    }
}
