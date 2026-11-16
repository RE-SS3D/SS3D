using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using SS3D.Systems.Tile.Validation;
using UnityEngine;
using static SS3D.Systems.Electricity.SystemsView.PowerSystemsViewCodes;
using static SS3D.Systems.Tile.MapEditor.SystemsView.SystemsViewProblems;

namespace SS3D.Systems.Electricity.SystemsView
{
    /// <summary>
    /// The Power view's problems (Documents/architecture/systems/map-editor-systems-views.md, Problems; creative-mode §4
    /// "The views point out problems"), found from plain data so it is pure and testable: the source fills a
    /// <see cref="PowerProblemInput"/> from the circuits, devices and rooms it sends, and the issues go both into the view
    /// as markers and into Map Check as warnings. The v1 list:
    /// <list type="bullet">
    /// <item><b>APC on no cable</b> and <b>SMES on no circuit</b>: the device's circuit view id is 0 (alone, on no cable).</item>
    /// <item><b>Circuit with no generator or storage</b>: a circuit with an APC on it and no generator, solar panel or SMES.
    /// A cable with nothing that wants power on it is harmless, so it isn't marked.</item>
    /// <item><b>Needs more than it gives</b>: an authoring check, not the reading of the moment. Demand is what every
    /// consumer fed by the circuit's APCs draws at most (<see cref="IRatedPowerConsumer"/>, else <c>PowerNeeded</c>; every channel on); supply is every producer's rated
    /// output (<see cref="IRatedPowerProducer"/>: a generator running, a solar panel in full sun) plus every SMES's highest
    /// discharge rate, charged or not and switched on or not. Neither moves with the sun, a switch or a battery's charge,
    /// so the marker doesn't come and go.</item>
    /// <item><b>Room with machines but no APC</b>: a room with a consumer in it and no APC at all (a broken one is still
    /// there, and the Power view hatches it). Map Check's wording and id from before this phase.</item>
    /// </list>
    /// </summary>
    public static class PowerProblemAnalyzer
    {
        public const string Category = "Power";

        /// <summary>Demand within this of supply is not "more than it gives": float sums of the same loads.</summary>
        public const float DemandSlackKw = 0.05f;

        public const byte CodeApcNoCable = 1;
        public const byte CodeNoSource = 2;
        public const byte CodeOverDemand = 3;
        public const byte CodeRoomNoApc = 4;
        public const byte CodeSmesNoCircuit = 5;

        public static readonly SystemsViewProblemKind ApcNoCable = new(CodeApcNoCable, "APC on no cable",
            "its room runs on the APC's own cell until it's flat");
        public static readonly SystemsViewProblemKind NoSource = new(CodeNoSource, "Circuit with no generator or storage",
            "nothing feeds it, so its rooms go dark once their APC cells run flat");
        public static readonly SystemsViewProblemKind OverDemand = new(CodeOverDemand, "Circuit that needs more than it gives",
            "its rooms can draw more than its generators, solar panels and SMES units supply at most");
        public static readonly SystemsViewProblemKind RoomNoApc = new(CodeRoomNoApc, "Room with machines but no APC",
            "nothing here gets local power");
        public static readonly SystemsViewProblemKind SmesNoCircuit = new(CodeSmesNoCircuit, "SMES on no circuit",
            "it can't store or give power");

        public static readonly SystemsViewProblemKind[] Kinds = { ApcNoCable, NoSource, OverDemand, RoomNoApc, SmesNoCircuit };

        public static List<SystemsViewIssue> Analyze(PowerProblemInput input)
        {
            List<SystemsViewIssue> issues = new();
            if (input == null)
                return issues;

            List<PowerProblemDevice> devices = input.Devices.ToList();
            devices.Sort((a, b) => a.Key.Tile != b.Key.Tile ? CompareTiles(a.Key.Tile, b.Key.Tile) : a.Key.Slot.CompareTo(b.Key.Slot));
            List<PowerProblemDevice> apcs = devices.Where(device => device.Kind == KindApc).ToList();
            // Each device's own marker key: two APCs on the two faces of one wall get a marker each.
            byte[] onTile = IndexOnTile(devices, device => device.Key.Tile);
            Dictionary<SystemsViewKey, byte> sub = new();
            for (int i = 0; i < devices.Count; i++)
                sub[devices[i].Key] = onTile[i];
            SystemsViewKey Mark(PowerProblemDevice device, byte code) =>
                new(device.Key.Tile, MarkerSlot(code, sub.TryGetValue(device.Key, out byte index) ? index : (byte)0));

            foreach (PowerProblemDevice apc in apcs.Where(apc => apc.CircuitViewId <= 0))
            {
                string title = $"APC at {Cell(apc.Key.Tile)} is on no cable";
                issues.Add(Issue($"apc-cable:{apc.Key.Tile.x}:{apc.Key.Tile.y}", title,
                    $"{title}: {ApcNoCable.Words}. Run a cable to its tile.", apc.Position, new[] { Mark(apc, CodeApcNoCable) }));
            }

            foreach (IGrouping<int, PowerProblemDevice> circuit in apcs.Where(apc => apc.CircuitViewId > 0).GroupBy(apc => apc.CircuitViewId))
                AddCircuitIssue(issues, circuit.Key, circuit.ToArray(), devices, Mark);

            foreach (PowerProblemDevice smes in devices.Where(device => device.Kind == KindSmes && device.CircuitViewId <= 0))
            {
                string title = $"SMES at {Cell(smes.Key.Tile)} is on no circuit";
                issues.Add(Issue($"smes-circuit:{smes.Key.Tile.x}:{smes.Key.Tile.y}", title,
                    $"{title}: {SmesNoCircuit.Words}. Run a cable to its tile.", smes.Position, new[] { Mark(smes, CodeSmesNoCircuit) }));
            }

            AddRoomsWithoutApc(issues, input.Rooms, devices, Mark);
            return issues;
        }

        /// <summary>Nominal supply of a circuit: rated producer output plus every SMES's highest discharge rate.</summary>
        public static float NominalSupplyKw(int circuitViewId, IEnumerable<PowerProblemDevice> devices) =>
            devices.Where(device => device.CircuitViewId == circuitViewId && IsSupply(device.Kind)).Sum(device => device.SupplyKw);

        /// <summary>Nominal demand of a circuit: what every consumer its APCs feed draws, every channel on.</summary>
        public static float NominalDemandKw(int circuitViewId, IEnumerable<PowerProblemDevice> devices) =>
            devices.Where(device => device.CircuitViewId == circuitViewId && IsConsumer(device.Kind)).Sum(device => device.DemandKw);

        /// <summary>
        /// One circuit's problem, at most: with no generator, solar panel or SMES it is "no generator or storage" and the
        /// supply is never compared, so the two never both fire.
        /// </summary>
        private static void AddCircuitIssue(List<SystemsViewIssue> issues, int circuitViewId, PowerProblemDevice[] apcs,
            List<PowerProblemDevice> devices, System.Func<PowerProblemDevice, byte, SystemsViewKey> mark)
        {
            PowerProblemDevice first = apcs[0];
            string onIt = apcs.Length == 1 ? $"the APC at {Cell(first.Key.Tile)}" : $"{apcs.Length} APCs (first at {Cell(first.Key.Tile)})";
            bool hasSource = devices.Any(device => device.CircuitViewId == circuitViewId && IsSupply(device.Kind));
            if (!hasSource)
            {
                string title = $"No generator or SMES on the circuit of {onIt}";
                issues.Add(Issue($"circuit-source:{first.Key.Tile.x}:{first.Key.Tile.y}", title,
                    $"{title}: {NoSource.Words}. Connect it to a generator, solar panels or an SMES.", first.Position,
                    apcs.Select(apc => mark(apc, CodeNoSource)).ToArray()));
                return;
            }

            float supply = NominalSupplyKw(circuitViewId, devices);
            float demand = NominalDemandKw(circuitViewId, devices);
            if (demand <= supply + DemandSlackKw)
                return;

            string overTitle = $"The circuit of {onIt} needs more power than it gives";
            issues.Add(Issue($"circuit-demand:{first.Key.Tile.x}:{first.Key.Tile.y}", overTitle,
                $"{overTitle}: {OverDemand.Words}: up to {Kw(demand)} against at most {Kw(supply)}. "
                + "Add a generator or solar panels, or move some rooms onto another circuit.", first.Position,
                apcs.Select(apc => mark(apc, CodeOverDemand)).ToArray()));
        }

        private static void AddRoomsWithoutApc(List<SystemsViewIssue> issues, IEnumerable<PowerProblemRoom> rooms, List<PowerProblemDevice> devices,
            System.Func<PowerProblemDevice, byte, SystemsViewKey> mark)
        {
            foreach (PowerProblemRoom room in rooms.Where(room => room.AreaId != 0 && !room.HasApc).OrderBy(room => room.AreaId))
            {
                // The first machine west to east, where the marker and Locate go.
                PowerProblemDevice? first = null;
                foreach (PowerProblemDevice device in devices)
                {
                    if (IsConsumer(device.Kind) && device.AreaId == room.AreaId)
                    {
                        first = device;
                        break;
                    }
                }

                if (first is not { } consumer)
                    continue;

                string title = $"No APC in {RoomName(room.Name)} {Cell(consumer.Key.Tile)}";
                issues.Add(Issue($"no-apc:{room.AreaId}", title, $"{title}, so {RoomNoApc.Words}. Place an APC in the room.",
                    consumer.Position, new[] { mark(consumer, CodeRoomNoApc) }, MapValidator.ApcEntry));
            }
        }

        private static string Kw(float kw) => SystemsViewText.Number(kw, "0.0") + " kW";

        private static SystemsViewIssue Issue(string id, string title, string body, Vector3 position, SystemsViewKey[] markers,
            string entryKey = null) => SystemsViewProblems.Issue(Category, id, title, body, position, markers, entryKey);
    }

    /// <summary>
    /// One electric device as the power analyzer reads it: its badge kind (<see cref="PowerSystemsViewCodes"/>) and place,
    /// its circuit as its badge carries it (a consumer's is its APC's; 0 is none), its room, and its nominal kW.
    /// </summary>
    public struct PowerProblemDevice
    {
        public byte Kind;
        public SystemsViewKey Key;
        /// <summary>Its world position, where Map Check's Locate goes.</summary>
        public Vector3 Position;
        public int CircuitViewId;
        public ushort AreaId;
        /// <summary>A producer's rated output or an SMES's highest discharge rate; 0 for anything else.</summary>
        public float SupplyKw;
        /// <summary>A consumer's draw; 0 for anything else.</summary>
        public float DemandKw;
    }

    /// <summary>One room: whether an APC is there at all (broken or not).</summary>
    public struct PowerProblemRoom
    {
        public ushort AreaId;
        public string Name;
        public bool HasApc;
    }

    /// <summary>Everything one power analysis reads.</summary>
    public sealed class PowerProblemInput
    {
        public readonly List<PowerProblemDevice> Devices = new();
        public readonly List<PowerProblemRoom> Rooms = new();
    }
}
