using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SS3D.Core;
using SS3D.Systems.Area;
using SS3D.Systems.Electricity;
using SS3D.Systems.Electricity.SystemsView;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using static SS3D.Editor.Automation.MapEditorSystemsPilot;

namespace SS3D.Editor.Automation
{
    /// <summary>
    /// The Power view's half of <see cref="MapEditorSystemsPilot"/> (Documents/architecture/2026-11_map-editor-systems-views.md
    /// Phase 3): the overview with circuit colours, room tint and consumer dots, a traced cable circuit, a traced room, and a
    /// hatched room (an APC smashed for the shot, repaired after and on abort). Runs after the Atmos steps.
    /// </summary>
    internal static class MapEditorSystemsPowerPilot
    {
        private static Vector2Int _powerFocus;
        private static Vector2Int _hatchedRoomTile;
        private static Vector2Int _cableTraceTile;
        private static Vector2Int _roomTraceTile;
        private static BreakableDevice _brokenApc;
        private static ushort _hatchedRoom;

        /// <summary>Repairs an APC a run smashed and didn't get to repair (an abort), and forgets it.</summary>
        internal static void Reset()
        {
            if (_brokenApc != null)
                _brokenApc.ServerRepair();
            _brokenApc = null;
        }

        internal static Step[] Steps() => new Step[]
        {
            new Step
            {
                Name = "Turn the Power view on from the toolbar",
                Enter = () =>
                {
                    // Back out of the close-up the Atmos steps zoomed into.
                    MapEditorSystemsPilot.Editor().AutomationZoomStep(-13f);
                    Submit(EditorRoot().Q<Button>("systems-view-power"));
                },
                Until = () => Client().Structure is { } structure && structure.View == SystemsViewKind.Power
                    && structure.Segments.Count > 0 && structure.Rooms.Count > 0 && Client().Overlay.ChunkCount > 0
                    && Client().RoomTintOverlay.IsVisible,
                Timeout = 15,
                Verify = () =>
                {
                    _powerFocus = FramePower(Client().Structure);
                    Settle(1.5);
                },
            },
            new Step
            {
                Name = "Power overview: circuit colours, room tint and consumer dots",
                Until = Settled,
                Verify = () =>
                {
                    SystemsViewStructure structure = Client().Structure;
                    SystemsViewBadge[] consumers = structure.Badges.Values.Where(b => PowerSystemsViewCodes.IsConsumer(b.Kind)).ToArray();
                    Require(consumers.Length > 0, $"Consumers have status dots ({consumers.Length})");
                    Require(structure.Badges.Values.Any(b => b.Kind == PowerSystemsViewCodes.KindApc), "APCs have badges");
                    Require(structure.Rooms.Values.Any(r => r.CircuitViewId > 0), "Some room is tinted by its APC's circuit");
                    Require(structure.Networks.Values.All(n => !string.IsNullOrEmpty(n.Label)), "Every circuit and room has a name");
                    VisualElement legend = EditorRoot().Q("systems-legend");
                    Require(legend != null && legend.resolvedStyle.display == DisplayStyle.Flex
                        && legend.Query<Label>().ToList().Any(l => l.text.StartsWith("Every machine has a dot")),
                        "The legend explains the consumer dots");
                    AddMeasurement($"power client: {structure.Segments.Count} cable tiles, "
                        + $"{structure.Networks.Keys.Count(id => !PowerSystemsViewCodes.IsRoomViewId(id))} circuits, "
                        + $"{structure.Rooms.Count} rooms ({structure.Rooms.Values.Count(r => r.IsHatched)} hatched), "
                        + $"{structure.Badges.Count - consumers.Length} grid devices, consumers "
                        + $"{consumers.Count(b => PowerSystemsViewCodes.StatusOf(b.State) == PowerSystemsViewCodes.Powered)} powered / "
                        + $"{consumers.Count(b => PowerSystemsViewCodes.StatusOf(b.State) == PowerSystemsViewCodes.ChannelOff)} channel off / "
                        + $"{consumers.Count(b => PowerSystemsViewCodes.StatusOf(b.State) == PowerSystemsViewCodes.Unpowered)} unpowered");
                    Capture("20-power-overview");
                },
            },
            new Step
            {
                Name = "Frame a cable on the busiest circuit",
                Enter = () =>
                {
                    _cableTraceTile = PickCableTraceTile(Client().Structure);
                    MapEditorSystemsPilot.Editor().AutomationSelectTool(MapEditorTool.Select);
                    MapEditorSystemsPilot.Editor().AutomationCenterOn(new Vector3(_cableTraceTile.x, 0f, _cableTraceTile.y));
                    Settle(1.2);
                },
                Until = Settled,
                Verify = () =>
                {
                    Point(_cableTraceTile);
                    Settle(0.3);
                },
            },
            new Step
            {
                Name = "Select on a cable traces its circuit",
                Until = Settled,
                Verify = () => SelectAt(_cableTraceTile),
            },
            new Step
            {
                Name = "Circuit trace panel: supply against demand and its devices",
                Until = () => Client().Live is { } live && live.Networks.Length == Client().TraceSides.Count,
                Timeout = 5,
                Verify = () =>
                {
                    int circuit = Client().Structure.Segments[new SystemsViewKey(_cableTraceTile, PowerSystemsViewCodes.CableSlot)].ViewId;
                    Require(Client().TraceSides.Count == 1 && Client().TraceSides[0].ViewId == circuit, "The trace follows the cable's circuit");
                    VisualElement panel = EditorRoot().Q("systems-trace");
                    Require(panel != null && panel.resolvedStyle.display == DisplayStyle.Flex
                        && panel.Query<Label>().ToList().Any(l => l.text == "Supply")
                        && panel.Query<Label>().ToList().Any(l => l.text == "Demand"), "The panel shows supply against demand");
                    SystemsViewLiveNetwork network = Client().Live.Value.Networks[0];
                    AddMeasurement($"circuit trace {_cableTraceTile}: {Client().TraceLabel(Client().TraceSides[0])} "
                        + string.Join(", ", network.Rows.Select(r => r.Label + " " + r.Value)) + $"; {network.Devices.Length} devices");
                    Capture("22-power-circuit-trace");
                    Settle(0.5);
                },
            },
            new Step
            {
                Name = "Clear ends the circuit trace",
                Until = Settled,
                Verify = () =>
                {
                    Submit(EditorRoot().Q<Button>("systems-trace-close"));
                    Require(!Client().IsTracing, "Clear ends the circuit trace");
                },
            },
            new Step
            {
                Name = "Frame a floor in the room with the most machines",
                Enter = () =>
                {
                    _roomTraceTile = PickRoomTraceTile(Client().Structure);
                    MapEditorSystemsPilot.Editor().AutomationCenterOn(new Vector3(_roomTraceTile.x, 0f, _roomTraceTile.y));
                    Settle(1.2);
                },
                Until = Settled,
                Verify = () =>
                {
                    Point(_roomTraceTile);
                    Settle(0.3);
                },
            },
            new Step
            {
                Name = "Select on a floor traces its room",
                Until = Settled,
                Verify = () => SelectAt(_roomTraceTile),
            },
            new Step
            {
                Name = "Room trace panel: its APC and the machines it powers",
                Until = () => Client().Live is { } live && live.Networks.Length == Client().TraceSides.Count,
                Timeout = 5,
                Verify = () =>
                {
                    Require(Client().TraceSides.Count == 1 && PowerSystemsViewCodes.IsRoomViewId(Client().TraceSides[0].ViewId),
                        "The trace follows the floor's room");
                    SystemsViewLiveNetwork network = Client().Live.Value.Networks[0];
                    Require(network.ValueOf("APC") != null && network.Devices.Length > 1, "The panel shows the room's APC and its machines");
                    Require(EditorRoot().Q<Button>("systems-trace-locate-0") != null, "Each device has a Locate");
                    AddMeasurement($"room trace {_roomTraceTile}: {Client().TraceLabel(Client().TraceSides[0])} "
                        + string.Join(", ", network.Rows.Select(r => r.Label + " " + r.Value)) + $"; {network.Devices.Length} devices");
                    Capture("23-power-room-trace");
                    Settle(0.5);
                },
            },
            new Step
            {
                Name = "Clear ends the room trace",
                Until = Settled,
                Verify = () =>
                {
                    Submit(EditorRoot().Q<Button>("systems-trace-close"));
                    Require(!Client().IsTracing && Client().Overlay.Focus.Count == 0, "Clear ends the room trace");
                },
            },
            new Step
            {
                Name = "A room with no working APC is hatched",
                Enter = () =>
                {
                    // The shipped stations give every room an APC: smash one (round state only; repaired below) so the
                    // hatch, the APC's broken mark and the room's unpowered dots are on screen.
                    _hatchedRoomTile = PickHatchedRoomTile(Client().Structure);
                    if (_hatchedRoomTile == NoTile)
                    {
                        _brokenApc = PickApcToBreak(Client().Structure, out _hatchedRoom);
                        Require(_brokenApc != null && _brokenApc.ServerApplyDamage(_brokenApc.MaxIntegrity * 2f, StructuralDamageSource.Console),
                            "An APC breaks for the hatched-room check");
                    }
                },
                Until = () => _hatchedRoomTile != NoTile
                    || (Client().Structure.Rooms.TryGetValue(_hatchedRoom, out SystemsViewRoom room) && room.IsHatched),
                Timeout = 5,
                Verify = () =>
                {
                    if (_hatchedRoomTile == NoTile)
                        _hatchedRoomTile = PickHatchedRoomTile(Client().Structure);
                    MapEditorSystemsPilot.Editor().AutomationCenterOn(new Vector3(_hatchedRoomTile.x, 0f, _hatchedRoomTile.y));
                    Settle(1.5);
                },
            },
            new Step
            {
                Name = "Close-up of the hatched room",
                Until = Settled,
                Verify = () =>
                {
                    Point(_hatchedRoomTile);
                    AddCheck($"Hatched room at {_hatchedRoomTile}: {Client().HoverText(_hatchedRoomTile).Replace('\n', '/')}");
                    Capture("21-power-hatched-room");
                    Settle(0.5);
                },
            },
            new Step
            {
                Name = "Repairing the APC takes the hatch away",
                Enter = () =>
                {
                    if (_brokenApc != null)
                        _brokenApc.ServerRepair();
                },
                Until = () => Settled() && (_brokenApc == null
                    || (Client().Structure.Rooms.TryGetValue(_hatchedRoom, out SystemsViewRoom room) && !room.IsHatched)),
                Timeout = 5,
                Verify = () =>
                {
                    _brokenApc = null;
                    Submit(EditorRoot().Q<Button>("systems-view-power"));
                },
            },
            new Step
            {
                Name = "The Power view's overlay and room tint are gone",
                Until = () => Client().Structure == null,
                Verify = () => Require(!Client().RoomTintOverlay.IsVisible, "The room tint hides with the view"),
            },
        };

        private static readonly Vector2Int NoTile = new(int.MinValue, int.MinValue);

        /// <summary>A Select click on <paramref name="tile"/> through the Select tool's handler, as on the Atmos device.</summary>
        private static void SelectAt(Vector2Int tile)
        {
            Point(tile, InputUpdateType.Editor);
            MapEditorSystemsPilot.Editor().AutomationSelectClick();
            if (!Client().IsTracing)
            {
                Require(false, $"Select on {tile} traced something ({DescribePick(tile)})");
            }

            AddCheck($"Select on {tile} traced {string.Join(", ", Client().TraceSides.Select(side => Client().TraceLabel(side)))}");
        }

        /// <summary>Every floor tile per room id, from the room membership the client has (as the room tint draws it).</summary>
        private static Dictionary<ushort, List<Vector2Int>> RoomTiles()
        {
            Dictionary<ushort, List<Vector2Int>> rooms = new();
            foreach (KeyValuePair<Vector2Int, ushort[]> chunk in SubSystems.Get<AreaSubSystem>().FloorVisualCache.EnumerateChunks())
            {
                for (int i = 0; i < chunk.Value.Length; i++)
                {
                    if (chunk.Value[i] == AreaId.None) continue;
                    if (!rooms.TryGetValue(chunk.Value[i], out List<Vector2Int> tiles))
                    {
                        tiles = new List<Vector2Int>();
                        rooms[chunk.Value[i]] = tiles;
                    }

                    tiles.Add(chunk.Key * TileConstants.ChunkSize + new Vector2Int(i % TileConstants.ChunkSize, i / TileConstants.ChunkSize));
                }
            }

            return rooms;
        }

        /// <summary>Frames the chunk with the most consumers and cable, so the overview shows dots, tint and circuits together.</summary>
        private static Vector2Int FramePower(SystemsViewStructure structure)
        {
            Vector2Int chunk = structure.Badges.Keys.Select(k => k.Tile).Concat(structure.Segments.Keys.Select(k => k.Tile))
                .GroupBy(MapEditorSystemsOverlay.ChunkOf)
                .OrderByDescending(group => group.Count())
                .First().Key;
            Vector2Int centre = chunk * TileConstants.ChunkSize + new Vector2Int(TileConstants.ChunkSize / 2, TileConstants.ChunkSize / 2);
            MapEditorSystemsPilot.Editor().AutomationCenterOn(new Vector3(centre.x, 0f, centre.y));
            return centre;
        }

        /// <summary>The APC of the powered room with the fewest machines (and some floor), to smash for the hatched-room check.</summary>
        private static BreakableDevice PickApcToBreak(SystemsViewStructure structure, out ushort areaId)
        {
            Dictionary<ushort, List<Vector2Int>> rooms = RoomTiles();
            areaId = 0;
            foreach (SystemsViewRoom room in structure.Rooms.Values.Where(r => !r.IsHatched && rooms.ContainsKey(r.AreaId) && rooms[r.AreaId].Count >= 6)
                .OrderBy(r => ConsumersIn(structure, r)))
            {
                if (SubSystems.Get<AreaSubSystem>().TryGetArea(new AreaId(room.AreaId), out AreaRecord record)
                    && record.Apc is Component apc && apc != null && apc.TryGetComponent(out BreakableDevice breakable))
                {
                    areaId = room.AreaId;
                    return breakable;
                }
            }

            return null;
        }

        /// <summary>The middle tile of the largest hatched room, or <see cref="NoTile"/>.</summary>
        private static Vector2Int PickHatchedRoomTile(SystemsViewStructure structure)
        {
            Dictionary<ushort, List<Vector2Int>> rooms = RoomTiles();
            List<Vector2Int> tiles = structure.Rooms.Values.Where(r => r.IsHatched && rooms.ContainsKey(r.AreaId))
                .Select(r => rooms[r.AreaId]).OrderByDescending(t => t.Count).FirstOrDefault();
            return tiles == null ? NoTile : Middle(tiles);
        }

        /// <summary>A cable tile with no device on it, on the circuit with the most grid devices, nearest that circuit's middle.</summary>
        private static Vector2Int PickCableTraceTile(SystemsViewStructure structure)
        {
            int circuit = structure.Segments.Values.Select(s => s.ViewId).Distinct()
                .OrderByDescending(id => structure.Badges.Values.Count(b => PowerSystemsViewCodes.IsGrid(b.Kind) && b.ViewId == id))
                .First();
            List<Vector2Int> cables = structure.Segments.Values.Where(s => s.ViewId == circuit).Select(s => s.Key.Tile).ToList();
            Vector2Int middle = Middle(cables);
            return cables.Where(tile => !structure.Badges.Keys.Any(k => k.Tile == tile))
                .OrderBy(tile => (tile - middle).sqrMagnitude).First();
        }

        /// <summary>A floor of the powered room with the most consumers, with no cable or device on it, nearest its middle.</summary>
        private static Vector2Int PickRoomTraceTile(SystemsViewStructure structure)
        {
            Dictionary<ushort, List<Vector2Int>> rooms = RoomTiles();
            SystemsViewRoom room = structure.Rooms.Values.Where(r => !r.IsHatched && rooms.ContainsKey(r.AreaId))
                .OrderByDescending(r => ConsumersIn(structure, r))
                .First();
            List<Vector2Int> tiles = rooms[room.AreaId];
            Vector2Int middle = Middle(tiles);
            HashSet<Vector2Int> busy = new(structure.Badges.Keys.Select(k => k.Tile).Concat(structure.Segments.Keys.Select(k => k.Tile)));
            return tiles.Where(tile => !busy.Contains(tile)).OrderBy(tile => (tile - middle).sqrMagnitude).First();
        }

        /// <summary>How many machines a room has dots for.</summary>
        private static int ConsumersIn(SystemsViewStructure structure, SystemsViewRoom room) =>
            structure.Badges.Values.Count(b => PowerSystemsViewCodes.IsConsumer(b.Kind) && b.OtherViewId == room.ViewId);

        private static Vector2Int Middle(List<Vector2Int> tiles)
        {
            Vector2 sum = Vector2.zero;
            foreach (Vector2Int tile in tiles) sum += tile;
            Vector2 mean = sum / Mathf.Max(1, tiles.Count);
            return tiles.OrderBy(tile => ((Vector2)tile - mean).sqrMagnitude).First();
        }

        /// <summary>
        /// The Power view's numbers: its first-open size, a cold build (cables scanned, circuits joined, every device and
        /// room read), the warm poll the publisher runs four times a second while someone watches, and a warm build.
        /// </summary>
        internal static void Measure()
        {
            ElectricitySubSystem electricity = SubSystems.Get<ElectricitySubSystem>();
            Require(electricity.SystemsViewSource != null, "The power source exists on the server");
            PowerSystemsViewSource fresh = electricity.CreateSystemsViewSource();
            Stopwatch cold = Stopwatch.StartNew();
            _ = fresh.StructureVersion;
            SystemsViewStructure structure = fresh.BuildStructure();
            cold.Stop();
            Stopwatch poll = Stopwatch.StartNew();
            const int polls = 20;
            for (int i = 0; i < polls; i++)
                _ = fresh.StructureVersion;
            poll.Stop();
            Stopwatch warm = Stopwatch.StartNew();
            fresh.BuildStructure();
            warm.Stop();
            int bytes = SystemsViewWire.SizeOf(SystemsViewStructure.Diff(null, structure));
            AddMeasurement($"power {Station}: {structure.Segments.Count} cable tiles, "
                + $"{structure.Networks.Keys.Count(id => !PowerSystemsViewCodes.IsRoomViewId(id))} circuits, {structure.Rooms.Count} rooms, "
                + $"{structure.Badges.Count} badges ({structure.Badges.Values.Count(b => PowerSystemsViewCodes.IsConsumer(b.Kind))} consumers), "
                + $"{electricity.SystemsViewSource.GetType().Name} registered");
            AddMeasurement($"power first-open delta {bytes} B against a {MapEditorSystemsPilot.Editor().SystemsViews.MaxBytesPerSend} B send budget");
            AddMeasurement($"power build: cold {cold.Elapsed.TotalMilliseconds:0.0} ms (scan + join + read), "
                + $"poll {poll.Elapsed.TotalMilliseconds / polls:0.00} ms, warm build {warm.Elapsed.TotalMilliseconds:0.00} ms");
        }

    }
}
