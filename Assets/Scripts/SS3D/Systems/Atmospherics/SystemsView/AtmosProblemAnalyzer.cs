using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.Validation;
using UnityEngine;
using static SS3D.Systems.Atmospherics.SystemsView.AtmosSystemsViewCodes;
using static SS3D.Systems.Tile.MapEditor.SystemsView.SystemsViewProblems;

namespace SS3D.Systems.Atmospherics.SystemsView
{
    /// <summary>
    /// The Atmos view's problems (Documents/architecture/2026-11_map-editor-systems-views.md Q5, Phase 4; creative-mode §4
    /// "The views point out problems"), found from plain data so it is pure and testable: the source fills an
    /// <see cref="AtmosProblemInput"/> from the same runs and badges it sends, and the issues go both into the view as
    /// markers and into Map Check as warnings. The v1 list, and nothing that guesses at intent:
    /// <list type="bullet">
    /// <item><b>Open pipe end</b>: a pipe joined on at most one side, on a tile with no vent, scrubber, meter, injector or line fill. A
    /// run that reaches such a device's tile ends there on every line: stations run Supply and Waste side by side, so the
    /// vent's tile ends a Waste stub too, and marking that would be a false alarm on every room. Anything else that ends a
    /// pipe (a pump, filter, mixer, valve or manifold) is already a link.</item>
    /// <item><b>Device on no pipe</b>: the rule each device attaches by at runtime (<c>AtmosDevicePipeResolver</c>), with
    /// Map Check's ids and titles from before this phase.</item>
    /// <item><b>Vent with nothing feeding it / scrubber with nowhere to send gas</b>: see <see cref="IsSource"/> and
    /// <see cref="IsSink"/>.</item>
    /// <item><b>Device in a room with no power</b>: a device with a power consumer whose room has no working APC (none, or a
    /// broken one: what the Power view hatches). A device outside every room is Map Check's own row.</item>
    /// </list>
    /// </summary>
    public static class AtmosProblemAnalyzer
    {
        public const string Category = "Atmos";

        public const byte CodeOpenEnd = 1;
        public const byte CodeNoPipe = 2;
        public const byte CodeNoSource = 3;
        public const byte CodeNoSink = 4;
        public const byte CodeNoPower = 5;

        public static readonly SystemsViewProblemKind OpenEnd = new(CodeOpenEnd, "Open pipe end", "the pipe stops here, joined to nothing");
        // Words fit any count, since a network's or room's issue covers one device or many; every body carries them.
        public static readonly SystemsViewProblemKind NoPipe = new(CodeNoPipe, "Device on no pipe",
            "it does nothing until it is on a pipe it can use");
        public static readonly SystemsViewProblemKind NoSource = new(CodeNoSource, "Vent with nothing feeding it",
            "nothing puts gas into this network, so its vents can't fill their rooms");
        public static readonly SystemsViewProblemKind NoSink = new(CodeNoSink, "Scrubber with nowhere to send gas",
            "nothing takes gas out of this network, so its scrubbers soon stop cleaning the air");
        public static readonly SystemsViewProblemKind NoPower = new(CodeNoPower, "Device in a room with no power",
            "its room has no working APC, so a device there that needs power does nothing");

        public static readonly SystemsViewProblemKind[] Kinds = { OpenEnd, NoPipe, NoSource, NoSink, NoPower };

        public static List<SystemsViewIssue> Analyze(AtmosProblemInput input)
        {
            List<SystemsViewIssue> issues = new();
            if (input == null)
                return issues;

            List<AtmosProblemDevice> devices = input.Devices.ToList();
            devices.Sort((a, b) => a.Tile != b.Tile ? CompareTiles(a.Tile, b.Tile) : a.Slot.CompareTo(b.Slot));
            // Each device's own marker key: two devices on one tile with the same problem get a marker each.
            byte[] onTile = IndexOnTile(devices, device => device.Tile);
            Dictionary<(Vector2Int, byte), byte> sub = new();
            for (int i = 0; i < devices.Count; i++)
                sub[(devices[i].Tile, devices[i].Slot)] = onTile[i];
            SystemsViewKey KeyOf(AtmosProblemDevice device, byte code) =>
                new(device.Tile, MarkerSlot(code, sub.TryGetValue((device.Tile, device.Slot), out byte index) ? index : (byte)0));

            AddOpenEnds(issues, input.Pipes, devices);
            foreach (AtmosProblemDevice device in devices)
            {
                if (!device.HasPipe)
                    issues.Add(NoPipeIssue(device, KeyOf(device, CodeNoPipe)));
            }

            AddUnfedNetworks(issues, devices, KeyOf);
            AddUnpoweredRooms(issues, devices, KeyOf);
            return issues;
        }

        /// <summary>
        /// Gas goes into a network from it: a scrubber (it takes room air in, as a chamber intake does), the outlet of a
        /// pump, filter or mixer, a filter's side (the gases it lets out), either side of a valve (open or closed, since a
        /// closed valve is a switch the round flips), and a line fill. A device's state never counts: an author switches
        /// a pump on in a round, so "off" is not a mistake.
        /// </summary>
        public static bool IsSource(AtmosProblemDevice device, int viewId) => viewId > 0 && device.Kind switch
        {
            AtmosProblemDeviceKind.Scrubber or AtmosProblemDeviceKind.LineFill => device.BackViewId == viewId,
            AtmosProblemDeviceKind.Pump or AtmosProblemDeviceKind.Mixer => device.FrontViewId == viewId,
            AtmosProblemDeviceKind.Filter => device.FrontViewId == viewId || device.SideViewId == viewId,
            AtmosProblemDeviceKind.Valve => device.BackViewId == viewId || device.FrontViewId == viewId,
            _ => false,
        };

        /// <summary>
        /// Gas leaves a network through it: a vent (into its room, or out to space), the inlet of a pump, filter or mixer, a
        /// mixer's second inlet (its side), either side of a valve, and an injector.
        /// </summary>
        public static bool IsSink(AtmosProblemDevice device, int viewId) => viewId > 0 && device.Kind switch
        {
            AtmosProblemDeviceKind.Vent or AtmosProblemDeviceKind.Injector => device.BackViewId == viewId,
            AtmosProblemDeviceKind.Pump or AtmosProblemDeviceKind.Filter => device.BackViewId == viewId,
            AtmosProblemDeviceKind.Mixer => device.BackViewId == viewId || device.SideViewId == viewId,
            AtmosProblemDeviceKind.Valve => device.BackViewId == viewId || device.FrontViewId == viewId,
            _ => false,
        };

        private static void AddOpenEnds(List<SystemsViewIssue> issues, IEnumerable<AtmosProblemPipe> pipes, List<AtmosProblemDevice> devices)
        {
            HashSet<Vector2Int> ended = new();
            foreach (AtmosProblemDevice device in devices)
            {
                if (EndsPipes(device.Kind))
                    ended.Add(device.Tile);
            }

            List<AtmosProblemPipe> open = pipes.Where(pipe => LinkCount(pipe.Links) <= 1 && !ended.Contains(pipe.Tile)).ToList();
            open.Sort((a, b) => a.Tile != b.Tile ? CompareTiles(a.Tile, b.Tile) : a.Line.CompareTo(b.Line));
            foreach (AtmosProblemPipe pipe in open)
            {
                string title = $"Open {LineName(pipe.Line)} pipe end at {Cell(pipe.Tile)}";
                issues.Add(Issue($"open-end:{pipe.Line}:{pipe.Tile.x}:{pipe.Tile.y}", title,
                    $"{title}: {OpenEnd.Words}. Run it on to a device or another pipe, or remove the loose end.",
                    TileCentre(pipe.Tile), new[] { new SystemsViewKey(pipe.Tile, MarkerSlot(CodeOpenEnd, pipe.Line)) }));
            }
        }

        // Map Check's ids and titles from before Phase 4 (MapValidator.AddRoomDeviceChecks), kept so the rows a map author
        // knew keep their names; the body now carries the legend's words, then the fix.
        private static SystemsViewIssue NoPipeIssue(AtmosProblemDevice device, SystemsViewKey marker)
        {
            Vector2Int cell = device.Tile;
            string place = $"{cell.x}:{cell.y}";
            string name = DeviceName(device.Kind);
            string title;
            string consequence;
            switch (device.Kind)
            {
                case AtmosProblemDeviceKind.Pump or AtmosProblemDeviceKind.Valve:
                    title = $"{name} at {Cell(cell)} needs a Surface pipe at both ports";
                    consequence = "Run one up to its front and its back.";
                    break;
                case AtmosProblemDeviceKind.Filter or AtmosProblemDeviceKind.Mixer:
                    title = $"{name} at {Cell(cell)} can't tell which pipe line it's on";
                    consequence = "Its line needs a pipe at all three ports and none under it, and only one line may fit.";
                    break;
                default:
                    title = $"{name} at {Cell(cell)} has no {PipeLineName(device.Kind)} pipe on its tile";
                    consequence = "Run one under it.";
                    break;
            }

            return Issue($"pipe:{IdKind(device.Kind)}:{place}", title, $"{title}, so {NoPipe.Words}. {consequence}", device.Position,
                new[] { marker });
        }

        private static void AddUnfedNetworks(List<SystemsViewIssue> issues, List<AtmosProblemDevice> devices,
            System.Func<AtmosProblemDevice, byte, SystemsViewKey> keyOf)
        {
            foreach (bool vents in new[] { true, false })
            {
                AtmosProblemDeviceKind kind = vents ? AtmosProblemDeviceKind.Vent : AtmosProblemDeviceKind.Scrubber;
                foreach (IGrouping<int, AtmosProblemDevice> network in devices
                             .Where(device => device.Kind == kind && device.HasPipe && device.BackViewId > 0)
                             .GroupBy(device => device.BackViewId))
                {
                    int viewId = network.Key;
                    bool ok = vents ? devices.Any(device => IsSource(device, viewId)) : devices.Any(device => IsSink(device, viewId));
                    if (ok)
                        continue;

                    AtmosProblemDevice[] marked = network.ToArray();
                    Vector2Int first = marked[0].Tile;
                    int count = marked.Length;
                    string title = vents
                        ? count == 1 ? $"Nothing feeds the vent at {Cell(first)}" : $"Nothing feeds {count} vents on one network (first at {Cell(first)})"
                        : count == 1 ? $"Scrubber at {Cell(first)} has nowhere to send gas"
                        : $"{count} scrubbers on one network (first at {Cell(first)}) have nowhere to send gas";
                    string body = vents
                        ? $"{title}: {NoSource.Words}. Connect the network to a pump's outlet, a gas intake or a line fill."
                        : $"{title}: {NoSink.Words}. Connect the network to a pump's inlet, a valve or a vent out to space.";
                    byte code = vents ? CodeNoSource : CodeNoSink;
                    issues.Add(Issue($"{(vents ? "no-source" : "no-sink")}:{first.x}:{first.y}", title, body, marked[0].Position,
                        marked.Select(device => keyOf(device, code)).ToArray()));
                }
            }
        }

        private static void AddUnpoweredRooms(List<SystemsViewIssue> issues, List<AtmosProblemDevice> devices,
            System.Func<AtmosProblemDevice, byte, SystemsViewKey> keyOf)
        {
            foreach (IGrouping<ushort, AtmosProblemDevice> room in devices
                         .Where(device => device.NeedsPower && device.AreaId != 0 && !device.RoomPowered)
                         .GroupBy(device => device.AreaId).OrderBy(room => room.Key))
            {
                AtmosProblemDevice[] marked = room.ToArray();
                AtmosProblemDevice first = marked[0];
                string roomName = RoomName(first.RoomName);
                string what = string.Join(" and ", marked.GroupBy(device => DeviceName(device.Kind).ToLowerInvariant())
                    .Select(group => Counted(group.Count(), group.Key)));
                string title = $"{char.ToUpperInvariant(what[0])}{what.Substring(1)} in {roomName} {Cell(first.Tile)} {(marked.Length == 1 ? "has" : "have")} no power";
                string body = $"{title}: {NoPower.Words}. Place an APC in the room, or repair its APC.";
                issues.Add(Issue($"atmos-power:{room.Key}", title, body, first.Position,
                    marked.Select(device => keyOf(device, CodeNoPower)).ToArray(), MapValidator.ApcEntry));
            }
        }

        private static SystemsViewIssue Issue(string id, string title, string body, Vector3 position, SystemsViewKey[] markers,
            string entryKey = null) => SystemsViewProblems.Issue(Category, id, title, body, position, markers, entryKey);

        /// <summary>
        /// What sits on a pipe tile and ends the runs there: a vent, scrubber, meter or injector, or a line fill (a run filled
        /// from its last tile is meant to stop there).
        /// </summary>
        public static bool EndsPipes(AtmosProblemDeviceKind kind) => kind is AtmosProblemDeviceKind.Vent or AtmosProblemDeviceKind.Scrubber
            or AtmosProblemDeviceKind.Meter or AtmosProblemDeviceKind.Injector or AtmosProblemDeviceKind.LineFill;

        private static int LinkCount(byte links)
        {
            int count = 0;
            for (byte bits = links; bits != 0; bits &= (byte)(bits - 1))
                count++;
            return count;
        }

        public static string DeviceName(AtmosProblemDeviceKind kind) => kind switch
        {
            AtmosProblemDeviceKind.Vent => "Vent",
            AtmosProblemDeviceKind.Scrubber => "Scrubber",
            AtmosProblemDeviceKind.Pump => "Gas pump",
            AtmosProblemDeviceKind.Valve => "Valve",
            AtmosProblemDeviceKind.Filter or AtmosProblemDeviceKind.Mixer => "Filter or mixer",
            AtmosProblemDeviceKind.Meter or AtmosProblemDeviceKind.Injector => "Meter or injector",
            _ => "Line fill",
        };

        // Map Check's ids named these as its RoomDeviceKind values did.
        private static string IdKind(AtmosProblemDeviceKind kind) => kind switch
        {
            AtmosProblemDeviceKind.Vent => "Vent",
            AtmosProblemDeviceKind.Scrubber => "Scrubber",
            AtmosProblemDeviceKind.Pump => "GasPump",
            AtmosProblemDeviceKind.Valve => "Valve",
            AtmosProblemDeviceKind.Filter or AtmosProblemDeviceKind.Mixer => "ThreeWay",
            AtmosProblemDeviceKind.Meter or AtmosProblemDeviceKind.Injector => "LineReader",
            _ => "LineFill",
        };

        private static string PipeLineName(AtmosProblemDeviceKind kind) => kind switch
        {
            AtmosProblemDeviceKind.Vent => "Supply or Mixed",
            AtmosProblemDeviceKind.Scrubber => "Waste or Mixed",
            AtmosProblemDeviceKind.Meter or AtmosProblemDeviceKind.Injector => "gas",
            _ => "matching",
        };
    }

    /// <summary>The kinds of thing the atmos analyzer reads: every port device, plus line fill markers.</summary>
    public enum AtmosProblemDeviceKind : byte
    {
        Vent,
        Scrubber,
        Pump,
        Valve,
        Filter,
        Mixer,
        Meter,
        Injector,
        LineFill,
    }

    /// <summary>One pipe as the view draws it: its line (a run slot, Supply 0 to Surface 3), real links and network.</summary>
    public struct AtmosProblemPipe
    {
        public Vector2Int Tile;
        public byte Line;
        public byte Links;
        public int ViewId;
    }

    /// <summary>
    /// One device (or line fill) as the analyzer reads it: its networks as its badge carries them (a one-port device's in
    /// <see cref="BackViewId"/>; 0 is no pipe), whether it attaches, and its room's power.
    /// </summary>
    public struct AtmosProblemDevice
    {
        public AtmosProblemDeviceKind Kind;
        public Vector2Int Tile;
        /// <summary>Its tile layer, which orders two devices on one tile.</summary>
        public byte Slot;
        /// <summary>Its world position, where Map Check's Locate goes.</summary>
        public Vector3 Position;
        public int BackViewId;
        public int FrontViewId;
        public int SideViewId;
        /// <summary>It finds the pipes it needs, by the rule it attaches by at runtime.</summary>
        public bool HasPipe;
        /// <summary>It has a power consumer, so it does nothing without power.</summary>
        public bool NeedsPower;
        /// <summary>Its room's area id; 0 outside every room.</summary>
        public ushort AreaId;
        public string RoomName;
        /// <summary>Its room has a working APC.</summary>
        public bool RoomPowered;
    }

    /// <summary>A line fill marker as the Atmos source reads it: where it is and the line it fills.</summary>
    public readonly struct AtmosLineFill : System.IEquatable<AtmosLineFill>
    {
        public readonly TileCoord Coord;
        public readonly TileLayer Line;
        public readonly Vector3 Position;

        public AtmosLineFill(TileCoord coord, TileLayer line, Vector3 position)
        {
            Coord = coord;
            Line = line;
            Position = position;
        }

        public bool Equals(AtmosLineFill other) => Coord.Equals(other.Coord) && Line == other.Line && Position == other.Position;
        public override bool Equals(object obj) => obj is AtmosLineFill other && Equals(other);
        public override int GetHashCode() => System.HashCode.Combine(Coord, Line);
    }

    /// <summary>Everything one atmos analysis reads.</summary>
    public sealed class AtmosProblemInput
    {
        public readonly List<AtmosProblemPipe> Pipes = new();
        public readonly List<AtmosProblemDevice> Devices = new();
    }
}
