using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SS3D.Systems.Tile.SpawnPoints;

namespace SS3D.Systems.Tile.Validation
{
    /// <summary>Shared save-warning and round-start policy. No scene-wide object searches.</summary>
    public static class MapValidator
    {
        public static MapValidationReport Validate(string mode, IEnumerable<string> jobs,
            IReadOnlyList<SpawnPointRecord> spawns, bool hasNuke, bool hasDisk)
        {
            MapValidationReport report = new();
            string[] requiredJobs = jobs.Where(job => !string.IsNullOrWhiteSpace(job))
                .Distinct(StringComparer.Ordinal).OrderBy(job => job, StringComparer.Ordinal).ToArray();
            if (requiredJobs.Length == 0)
                report.AddIssue("jobs-config", false, "Jobs", "No jobs configured", "Configure the server's offered jobs before starting.");
            foreach (string job in requiredJobs)
            {
                if (!spawns.Any(spawn => spawn.Kind == SpawnPointKind.Job && spawn.JobName == job))
                    report.AddIssue("job:" + job, false, "Jobs", $"Missing job spawn: {job}",
                        $"Missing job spawn: {job}. Add it under Scripts → Spawn Placements.");
            }

            // Secret resolves to the sole existing antagonist mode, just as GamemodeSubSystem does.
            if (mode is "Secret" or "Nuclear Emergency")
            {
                if (!spawns.Any(spawn => spawn.Kind == SpawnPointKind.Antagonist
                    && spawn.AntagonistCategory == AntagonistSpawnCategory.NuclearOperatives))
                    report.AddIssue("operative", false, "Game Modes", "Missing Nuclear Operatives spawn",
                        "Missing Nuclear Operatives spawn. Add it under Scripts → Spawn Placements.");
                if (!hasNuke)
                    report.AddIssue("nuke", false, "Game Modes", "Missing nuclear device",
                        "Missing nuclear device. Place a NukeCart on the map.");
                if (!hasDisk)
                    report.AddIssue("disk", false, "Game Modes", "Missing authentication disk",
                        "Missing authentication disk. Place a NuclearAuthenticationDisk on the map.");
            }
            else if (mode != "Extended")
                report.AddIssue("mode", false, "Game Modes", "Unsupported game mode",
                    "Unsupported game mode. Select Extended, Secret or Nuclear Emergency.");

            foreach (string job in spawns.Where(spawn => spawn.Kind == SpawnPointKind.Job)
                .Select(spawn => spawn.JobName).Distinct(StringComparer.Ordinal)
                .OrderBy(job => job, StringComparer.Ordinal))
            {
                if (!requiredJobs.Contains(job, StringComparer.Ordinal))
                    report.AddIssue("unavailable:" + job, true, "Jobs", $"Spawn for unavailable job: {job}",
                        $"Spawn for unavailable job: {job}. Check its job name or server role configuration.",
                        spawns.First(spawn => spawn.Kind == SpawnPointKind.Job && spawn.JobName == job).Position, true);
            }
            // The policy retains one error per missing requirement; the editor groups related
            // failures into a single expandable checklist, including requirements already met.
            if (report.Issues.Any(issue => issue.Id.StartsWith("job:", StringComparison.Ordinal)))
            {
                report.Issues.RemoveAll(issue => issue.Id.StartsWith("job:", StringComparison.Ordinal));
                report.Issues.Insert(0, new MapValidationIssue
                {
                    Id = "job-spawns", Category = "Jobs", Title = "Offered jobs missing spawn markers",
                    Body = "Every offered job needs an authored spawn marker.",
                    Checks = requiredJobs.Select(job => new MapValidationCheck
                    {
                        Label = job, IsMet = spawns.Any(spawn => spawn.Kind == SpawnPointKind.Job && spawn.JobName == job),
                    }).ToArray(),
                });
            }
            if (report.Issues.Any(issue => issue.Id is "operative" or "nuke" or "disk"))
            {
                report.Issues.RemoveAll(issue => issue.Id is "operative" or "nuke" or "disk");
                report.Issues.Add(new MapValidationIssue
                {
                    Id = "nuclear", Category = "Game Modes", Title = "Nuclear requirements incomplete",
                    Body = "Nuclear Emergency and Secret require an operative marker, nuclear device and authentication disk.",
                    Checks = new[]
                    {
                        new MapValidationCheck { Label = "Operative marker", IsMet = spawns.Any(spawn => spawn.Kind == SpawnPointKind.Antagonist
                            && spawn.AntagonistCategory == AntagonistSpawnCategory.NuclearOperatives) },
                        new MapValidationCheck { Label = "Nuclear device", IsMet = hasNuke },
                        new MapValidationCheck { Label = "Authentication disk", IsMet = hasDisk },
                    },
                });
            }
            return report;
        }

        /// <summary>
        /// Warns about devices that are placed but cannot do their job. "Room" here is an area
        /// (the editor's Scripts → Rooms): an APC's flood fill bounded by walls and doors, or a
        /// painted room once the map is authored. Device names can repeat across rooms, so
        /// titles carry a position.
        /// </summary>
        public static void AddRoomDeviceChecks(MapValidationReport report,
            IEnumerable<RoomDeviceRecord> devices, bool paintedRooms)
        {
            RoomDeviceRecord[] ordered = devices
                .OrderBy(device => device.Position.x).ThenBy(device => device.Position.z).ToArray();
            string assignRoom = paintedRooms ? "Paint the tile into a room under Scripts → Rooms."
                : "Add an APC to that space, or paint it into a room under Scripts → Rooms.";

            foreach (RoomDeviceRecord device in ordered)
            {
                Vector2Int cell = SpawnPointRegistry.TileKey(device.Position);
                string name = KindName(device.Kind);
                string place = $"{cell.x}:{cell.y}";
                if (IsPort(device.Kind) && !device.HasPipe)
                    AddWarning(report, $"pipe:{device.Kind}:{place}", device,
                        $"{name} at ({cell.x}, {cell.y}) has no pipe",
                        "so it can't move any gas. Run a pipe to it.");
                if (device.AreaId == 0 && device.Kind == RoomDeviceKind.AirAlarm)
                    AddWarning(report, $"alarm-room:{place}", device,
                        $"{name} at ({cell.x}, {cell.y}) faces no room",
                        "so it controls nothing. Check which way it faces. " + assignRoom);
            }

            // One row for all of them: a map without APCs would otherwise list every device.
            // Locate goes to the first, and fixing it moves Locate on to the next.
            RoomDeviceRecord[] roomless = ordered.Where(device => device.AreaId == 0 && IsPort(device.Kind)).ToArray();
            if (roomless.Length > 0)
                AddWarning(report, "air-alarm-room", roomless[0],
                    $"{Counted(roomless.Count(device => device.Kind == RoomDeviceKind.Vent), "vent", roomless.Count(device => device.Kind == RoomDeviceKind.Scrubber), "scrubber")} outside every room",
                    $"so no air alarm can control {(roomless.Length == 1 ? "it" : "them")}. " + assignRoom);

            foreach (IGrouping<ushort, RoomDeviceRecord> room in ordered.Where(device => device.AreaId != 0)
                .GroupBy(device => device.AreaId).OrderBy(room => room.Key))
            {
                RoomDeviceRecord[] ports = room.Where(device => IsPort(device.Kind)).ToArray();
                int vents = ports.Count(device => device.Kind == RoomDeviceKind.Vent);
                int scrubbers = ports.Length - vents;
                if (ports.Length > 0 && room.All(device => device.Kind != RoomDeviceKind.AirAlarm))
                    AddWarning(report, $"air-alarm:{room.Key}", ports[0], $"No air alarm in {RoomLabel(ports[0])}",
                        $"so nothing controls its {Counted(vents, "vent", scrubbers, "scrubber")}. "
                        + "Place one on a wall facing into the room.");
                if (vents > 0 && scrubbers == 0)
                    AddWarning(report, $"no-scrubber:{room.Key}", ports[0], $"No scrubber in {RoomLabel(ports[0])}",
                        "so bad air can't be cleaned out of it. Place a scrubber on a pipe.");
                else if (scrubbers > 0 && vents == 0)
                    AddWarning(report, $"no-vent:{room.Key}", ports[0], $"No vent in {RoomLabel(ports[0])}",
                        "so lost air can't be replaced. Place a vent on a pipe.");

                RoomDeviceRecord first = room.First();
                if (!first.RoomHasApc)
                    AddWarning(report, $"no-apc:{room.Key}", first, $"No APC in {RoomLabel(first)}",
                        "so nothing here gets local power. Place an APC in the room.");
            }
        }

        private static void AddWarning(MapValidationReport report, string id, RoomDeviceRecord device, string title, string consequence) =>
            report.AddIssue(id, true, "Atmos", title, $"{title}, {consequence}", device.Position, true);

        private static bool IsPort(RoomDeviceKind kind) => kind is RoomDeviceKind.Vent or RoomDeviceKind.Scrubber;

        private static string KindName(RoomDeviceKind kind) => kind switch
        {
            RoomDeviceKind.Vent => "Vent",
            RoomDeviceKind.Scrubber => "Scrubber",
            RoomDeviceKind.AirAlarm => "Air alarm",
            _ => "Light",
        };

        private static string RoomLabel(RoomDeviceRecord device)
        {
            Vector2Int cell = SpawnPointRegistry.TileKey(device.Position);
            return $"{device.AreaName} ({cell.x}, {cell.y})";
        }

        private static string Counted(int first, string firstName, int second, string secondName)
        {
            string[] parts =
            {
                first > 0 ? $"{first} {firstName}{(first == 1 ? "" : "s")}" : null,
                second > 0 ? $"{second} {secondName}{(second == 1 ? "" : "s")}" : null,
            };
            return string.Join(" and ", parts.Where(part => part != null));
        }
    }

    public enum RoomDeviceKind
    {
        Vent,
        Scrubber,
        AirAlarm,
        Light,
    }

    /// <summary>A placed device and the room it belongs to (area 0 means no room).</summary>
    public readonly struct RoomDeviceRecord
    {
        public readonly RoomDeviceKind Kind;
        public readonly Vector3 Position;
        public readonly ushort AreaId;
        public readonly string AreaName;
        public readonly bool RoomHasApc;
        public readonly bool HasPipe;

        public RoomDeviceRecord(RoomDeviceKind kind, Vector3 position, ushort areaId, string areaName,
            bool roomHasApc = true, bool hasPipe = true)
        {
            Kind = kind;
            Position = position;
            AreaId = areaId;
            AreaName = areaName;
            RoomHasApc = roomHasApc;
            HasPipe = hasPipe;
        }
    }

    [Serializable]
    public struct MapValidationCheck
    {
        public string Label;
        public bool IsMet;
    }

    /// <summary>Server-authored issue data shared by save policy and the editor's Map Check panel.</summary>
    [Serializable]
    public struct MapValidationIssue
    {
        public string Id;
        public bool IsWarning;
        public string Category;
        public string Title;
        public string Body;
        /// <summary>Map location the issue points at (e.g. a spawn without floor); drives the Locate button.</summary>
        public Vector3 Position;
        public bool HasPosition;
        public MapValidationCheck[] Checks;
    }

    public sealed class MapValidationReport
    {
        public List<MapValidationIssue> Issues { get; } = new();

        public void AddIssue(string id, bool warning, string category, string title, string body,
            Vector3 position = default, bool hasPosition = false)
        {
            Issues.Add(new MapValidationIssue { Id = id, IsWarning = warning, Category = category,
                Title = title, Body = body, Position = position, HasPosition = hasPosition });
            (warning ? Warnings : Errors).Add(body);
        }

        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public bool CanStart => Errors.Count == 0;
        public bool HasIssues => Errors.Count > 0 || Warnings.Count > 0;

        public string Describe(string map, string mode, bool saved)
        {
            List<string> lines = new()
            {
                saved ? $"Saved {map}. Validation for {mode}:" : $"Cannot start {map} ({mode}):",
            };
            lines.AddRange(Errors.Select(error => "• " + error));
            lines.AddRange(Warnings.Select(warning => "• Warning: " + warning));
            if (saved && Errors.Count > 0)
                lines.Add("Saving is allowed. Fix these issues before starting this mode.");
            if (!saved)
                lines.Add("Fix the map in the editor, save it, then try Start again.");
            return string.Join("\n", lines);
        }
    }
}
