using System;
using System.Collections.Generic;
using System.Text;
using SS3D.Systems.StructuralDamage;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using static SS3D.Systems.Electricity.SystemsView.PowerSystemsViewCodes;

namespace SS3D.Systems.Electricity.SystemsView
{
    /// <summary>
    /// Server: the Power view's structure (Documents/architecture/2026-11_map-editor-systems-views.md, Phase 3), read from
    /// the server's electricity state through <see cref="IPowerSystemsViewWorld"/>. Codes are <see cref="PowerSystemsViewCodes"/>.
    /// <para>
    /// <b>Circuits.</b> Every HV cable tile is a segment. Cable runs (cables joined side by side, as
    /// <see cref="ElectricCableConnectivity"/> walks them) and the grid devices on them are joined into one network per
    /// electricity circuit: two runs whose devices the graph puts in one circuit are one network, and grid devices joined
    /// without a cable (side by side) are one too. A lone device on no cable is on no network (view id 0). A
    /// <see cref="NetworkIdentityTracker{TSegment}"/> over cable tiles and device places keeps colours through edits.
    /// </para>
    /// <para>
    /// <b>Rooms and consumers.</b> Every room is a network of its own (<see cref="RoomViewId"/>), with its APC's circuit in a
    /// <see cref="SystemsViewRoom"/> for the client's tint. Every consumer is a badge: powered, its channel switched off at
    /// its APC, or unpowered. Device and room state has no change event, so <see cref="StructureVersion"/> compares every
    /// device and room with the last poll, keyed by the device (or room id) itself, and moves when anything differs. Cable
    /// runs are rescanned only when <see cref="IPowerSystemsViewWorld.TopologyVersion"/> moves; circuits are re-joined then,
    /// or when a grid device comes, goes or moves.
    /// </para>
    /// <para>
    /// <see cref="ReadLive"/> (the trace) reads a circuit's supply, demand and storage and every grid device on it, or a
    /// room's APC (charge, breakers, load) and the consumers it powers, now, through the view ids of the last build.
    /// </para>
    /// </summary>
    public sealed class PowerSystemsViewSource : ISystemsViewSource
    {
        private readonly IPowerSystemsViewWorld _world;
        private readonly NetworkIdentityTracker<SystemsViewKey> _tracker = new();

        // Cable runs: tile → run index, and each run's tiles; rescanned when the world's topology moves.
        private readonly Dictionary<Vector2Int, int> _cableRun = new();
        private readonly Dictionary<Vector2Int, byte> _cableLinks = new();
        private readonly List<List<Vector2Int>> _runs = new();

        // Circuits of the last join: networks by view id, and each grid device's view id.
        private readonly List<CircuitNetwork> _circuits = new();
        private readonly Dictionary<int, CircuitNetwork> _circuitByViewId = new();
        private readonly Dictionary<IElectricDevice, int> _circuitViewIdOf = new();
        private readonly Dictionary<Vector2Int, int> _cableViewIdOf = new();

        // What each device and room was at the last poll, keyed by the device or the room's id, so order never matters.
        private Dictionary<IElectricDevice, Seen> _seen = new();
        private Dictionary<IElectricDevice, Seen> _current = new();
        private Dictionary<ushort, SeenRoom> _seenRooms = new();
        private Dictionary<ushort, SeenRoom> _currentRooms = new();
        private readonly List<PowerSystemsViewRoomInfo> _roomScratch = new();

        // Rooms of the last build: their consumers and APC, for a traced room.
        private readonly Dictionary<ushort, List<IElectricDevice>> _roomConsumers = new();
        // SMES rates between reads: energy and tick at the last read.
        private readonly Dictionary<IPowerStorage, Rate> _rates = new();

        // A smashed APC's casing, looked up once per APC.
        private readonly Dictionary<IApcChannelSource, BreakableDevice> _breakables = new();

        private TileMap _seenMap;
        // Each device's room and APC are re-resolved after a topology move (area membership and APC changes move it).
        private bool _resolveLinks = true;
        private int _seenState;
        private int _seenDeviceCount = -1;
        private bool _pollAll = true;
        private readonly List<IElectricDevice> _producers = new();
        private int _seenTopology;
        private bool _cablesStale = true;
        private bool _circuitsStale = true;
        private int _version;

        public PowerSystemsViewSource(IPowerSystemsViewWorld world)
        {
            _world = world ?? throw new ArgumentNullException(nameof(world));
        }

        public SystemsViewKind View => SystemsViewKind.Power;

        public bool SimulationPaused => _world.Paused;

        public int StructureVersion
        {
            get
            {
                Poll();
                return _version;
            }
        }

        /// <summary>Whether the last <see cref="StructureVersion"/> read compared every device (a version moved), not only producers.</summary>
        public bool LastPolledAllDevices { get; private set; }

        /// <summary>Whether the last build rescanned the cables (a topology move) and re-joined circuits; for tests and budget logs.</summary>
        public bool LastRescannedCables { get; private set; }

        public bool LastRejoinedCircuits { get; private set; }

        /// <summary>Builds from what the last <see cref="StructureVersion"/> read saw; the publisher reads it first.</summary>
        public SystemsViewStructure BuildStructure()
        {
            SystemsViewStructure structure = new(SystemsViewKind.Power);
            LastRescannedCables = false;
            LastRejoinedCircuits = false;
            TileMap map = _world.Map;
            if (map == null)
                return structure;

            if (_cablesStale)
                RescanCables(map);
            if (_circuitsStale)
                JoinCircuits();

            foreach (CircuitNetwork circuit in _circuits)
                structure.Set(new SystemsViewNetwork { ViewId = circuit.ViewId, Label = "Circuit " + circuit.ViewId });
            foreach (KeyValuePair<Vector2Int, int> cable in _cableViewIdOf)
            {
                structure.Set(new SystemsViewSegment
                {
                    Key = new SystemsViewKey(cable.Key, CableSlot),
                    ViewId = cable.Value,
                    Links = _cableLinks.TryGetValue(cable.Key, out byte links) ? links : (byte)0,
                });
            }

            foreach (List<IElectricDevice> consumers in _roomConsumers.Values)
                consumers.Clear();
            foreach (KeyValuePair<IElectricDevice, Seen> pair in _seen)
            {
                Seen seen = pair.Value;
                int room = RoomViewId(seen.Room);
                int viewId = IsGrid(seen.Kind) ? CircuitViewIdOf(pair.Key)
                    : seen.Apc != null ? CircuitViewIdOf(seen.Apc) : 0;
                structure.Set(new SystemsViewBadge
                {
                    Key = seen.Key,
                    Kind = seen.Kind,
                    State = seen.State,
                    Facing = seen.Facing,
                    ViewId = viewId,
                    OtherViewId = seen.Kind == KindApc || IsConsumer(seen.Kind) ? room : 0,
                });

                if (IsConsumer(seen.Kind) && seen.Room != 0)
                {
                    if (!_roomConsumers.TryGetValue(seen.Room, out List<IElectricDevice> list))
                        _roomConsumers[seen.Room] = list = new List<IElectricDevice>();
                    list.Add(pair.Key);
                }
            }

            foreach (SeenRoom room in _seenRooms.Values)
            {
                int viewId = RoomViewId(room.AreaId);
                structure.Set(new SystemsViewNetwork { ViewId = viewId, Label = RoomLabel(room) });
                structure.Set(new SystemsViewRoom
                {
                    AreaId = room.AreaId,
                    ViewId = viewId,
                    CircuitViewId = room.Apc != null ? CircuitViewIdOf(room.Apc) : 0,
                    Flags = (byte)((room.Apc != null ? RoomHasApc : 0) | (room.ApcBroken ? RoomApcBroken : 0)
                        | (room.Apc == null || room.ApcBroken ? SystemsViewRoom.Hatched : 0)),
                });
            }

            return structure;
        }

        /// <summary>
        /// A traced circuit's supply against demand, storage, and every grid device on it; or a traced room's APC (charge,
        /// breakers, load and grid draw) and the consumers in it. Read now, formatted at the panel's precision, so the live
        /// stream sends only what a reader would see change.
        /// </summary>
        public bool ReadLive(int viewId, out SystemsViewLiveNetwork live)
        {
            live = default;
            if (viewId <= 0 || _world.Map == null)
                return false;

            if (IsRoomViewId(viewId))
                return ReadRoom((ushort)(viewId - RoomViewIdBase), out live);

            if (!_circuitByViewId.TryGetValue(viewId, out CircuitNetwork circuit))
                return false;

            float supply = 0f;
            float demand = 0f;
            float stored = 0f;
            float capacity = 0f;
            List<SystemsViewLiveDevice> devices = new();
            foreach (IElectricDevice device in circuit.Devices)
            {
                if (!IsLive(device) || !_seen.TryGetValue(device, out Seen seen))
                    continue;

                string detail;
                switch (device)
                {
                    case IApcChannelSource apc:
                        demand += _world.TryGetApcStats(apc, out CircuitStats apcStats) ? apcStats.TotalDemandKw : 0f;
                        detail = ApcDetail(apc, seen);
                        break;
                    case IPowerStorage storage:
                        stored += storage.StoredEnergyKwh;
                        capacity += storage.MaxCapacityKwh;
                        detail = SmesDetail(storage);
                        break;
                    case IPowerProducer producer:
                        supply += producer.PowerProduction;
                        detail = producer.PowerProduction > 0f ? Kw(producer.PowerProduction) : "not producing";
                        break;
                    default:
                        detail = StateWords(seen.Kind, seen.State);
                        break;
                }

                devices.Add(new SystemsViewLiveDevice { Key = seen.Key, Label = DeviceLabel(seen), Detail = detail });
            }

            List<SystemsViewLiveRow> rows = new()
            {
                new SystemsViewLiveRow("Supply", Kw(supply)),
                new SystemsViewLiveRow("Demand", Kw(demand)),
                new SystemsViewLiveRow("Balance", Balance(supply, demand, stored)),
            };
            if (capacity > 0f)
                rows.Add(new SystemsViewLiveRow("Stored", $"{Number(stored, "0.0")} of {Number(capacity, "0.0")} kWh ({Percent(stored, capacity)})"));
            rows.Add(new SystemsViewLiveRow("Cable", circuit.CableTiles == 1 ? "1 tile" : $"{circuit.CableTiles} tiles"));

            SystemsViewText.SortWestToEast(devices);
            live = new SystemsViewLiveNetwork { ViewId = viewId, Rows = rows.ToArray(), Devices = devices.ToArray() };
            return true;
        }

        private bool ReadRoom(ushort areaId, out SystemsViewLiveNetwork live)
        {
            live = default;
            if (!_seenRooms.TryGetValue(areaId, out SeenRoom room))
                return false;

            List<SystemsViewLiveRow> rows = new();
            List<SystemsViewLiveDevice> devices = new();
            IApcChannelSource apc = room.Apc != null && IsLive(room.Apc) ? room.Apc : null;
            if (apc == null)
            {
                rows.Add(new SystemsViewLiveRow("APC", "none: nothing here gets power"));
            }
            else
            {
                int circuit = CircuitViewIdOf(apc);
                rows.Add(new SystemsViewLiveRow("APC", circuit > 0 ? "on Circuit " + circuit : "on no cable"));
                bool broken = Broken(apc);
                rows.Add(new SystemsViewLiveRow("Condition", broken ? "broken" : "working"));
                if (apc is IPowerStorage cell)
                    rows.Add(new SystemsViewLiveRow("Charge",
                        $"{Percent(cell.StoredEnergyKwh, cell.MaxCapacityKwh)} ({Number(cell.StoredEnergyKwh, "0.00")} of {Number(cell.MaxCapacityKwh, "0.0")} kWh)"));
                _world.TryGetApcStats(apc, out CircuitStats stats);
                ApcControlFlags channels = apc.Channels;
                rows.Add(new SystemsViewLiveRow("Lighting", Breaker(channels, ApcControlFlags.Lighting, stats.LightingLoadKw)));
                rows.Add(new SystemsViewLiveRow("Equipment", Breaker(channels, ApcControlFlags.Equipment, stats.EquipmentLoadKw)));
                rows.Add(new SystemsViewLiveRow("Environment", Breaker(channels, ApcControlFlags.Environment, stats.EnvironmentLoadKw)));
                rows.Add(new SystemsViewLiveRow("Load", Kw(stats.TotalDemandKw)));
                rows.Add(new SystemsViewLiveRow("From the grid", $"{Number(stats.TotalSupplyKw, "0.0")} of {Kw(stats.GridAvailableKw)}"));
                rows.Add(new SystemsViewLiveRow("State", broken ? "dead: the room runs on fixture cells"
                    : ApcStatusDeriver.DerivePowerState(stats) switch
                    {
                        ApcPowerState.Critical => "critical: cell nearly flat",
                        ApcPowerState.Overload => "short: the cell covers the rest",
                        _ => "the grid covers the load",
                    }));
                if (_seen.TryGetValue(apc, out Seen apcSeen))
                    devices.Add(new SystemsViewLiveDevice { Key = apcSeen.Key, Label = "APC", Detail = ApcDetail(apc, apcSeen) });
            }

            List<SystemsViewLiveDevice> consumers = new();
            if (_roomConsumers.TryGetValue(areaId, out List<IElectricDevice> inRoom))
            {
                foreach (IElectricDevice device in inRoom)
                {
                    if (!IsLive(device) || device is not IPowerConsumer consumer || !_seen.TryGetValue(device, out Seen seen))
                        continue;

                    byte state = ConsumerState(StatusOf(consumer, apc), consumer.Channel);
                    string detail = $"{StatusWords(state)} · {ChannelName(consumer.Channel)} {Kw(consumer.PowerNeeded)}";
                    consumers.Add(new SystemsViewLiveDevice { Key = seen.Key, Label = DeviceLabel(seen), Detail = detail });
                }
            }

            SystemsViewText.SortWestToEast(consumers);
            devices.AddRange(consumers);
            live = new SystemsViewLiveNetwork { ViewId = RoomViewId(areaId), Rows = rows.ToArray(), Devices = devices.ToArray() };
            return true;
        }

        private void Poll()
        {
            TileMap map = _world.Map;
            if (map != _seenMap)
            {
                // A new map: nothing carries over.
                _seenMap = map;
                _seenTopology = _world.TopologyVersion;
                _tracker.Clear();
                _rates.Clear();
                _breakables.Clear();
                _resolveLinks = true;
                _cablesStale = true;
                _circuitsStale = true;
                _version++;
            }
            else if (_world.TopologyVersion != _seenTopology)
            {
                _seenTopology = _world.TopologyVersion;
                _resolveLinks = true;
                _cablesStale = true;
                _circuitsStale = true;
                _version++;
            }

            // Consumer, APC and SMES state changes move the world's state version; with neither version moved, only
            // producers (solar output follows the sun, with no single write) are read again.
            IReadOnlyList<IElectricDevice> devices = _world.Devices;
            int deviceCount = devices?.Count ?? 0;
            LastPolledAllDevices = _resolveLinks || _pollAll || _world.StateVersion != _seenState || deviceCount != _seenDeviceCount;
            if (LastPolledAllDevices)
            {
                _seenState = _world.StateVersion;
                _seenDeviceCount = deviceCount;
                _pollAll = false;
                if (PollDevices())
                    _version++;
            }
            else if (PollProducers())
            {
                _version++;
            }

            if (PollRooms())
                _version++;
        }

        /// <summary>Re-reads only the producers; one that went makes the next poll compare everything.</summary>
        private bool PollProducers()
        {
            bool changed = false;
            foreach (IElectricDevice device in _producers)
            {
                if (!_seen.TryGetValue(device, out Seen before) || !TryDescribe(device, before, out Seen now))
                {
                    _pollAll = true;
                    return true;
                }

                if (!before.Equals(now))
                {
                    _seen[device] = now;
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Compares every device with the last poll. A grid device that came, went or moved re-joins the circuits (it may
        /// bridge cable runs); anything else is a badge change only. Returns whether anything changed.
        /// </summary>
        private bool PollDevices()
        {
            _current.Clear();
            IReadOnlyList<IElectricDevice> devices = _world.Devices;
            for (int i = 0; devices != null && i < devices.Count; i++)
            {
                IElectricDevice device = devices[i];
                if (device != null && TryDescribe(device, _seen.TryGetValue(device, out Seen before) ? before : (Seen?)null, out Seen now))
                    _current[device] = now;
            }

            bool changed = false;
            foreach (KeyValuePair<IElectricDevice, Seen> pair in _current)
            {
                if (!_seen.TryGetValue(pair.Key, out Seen before))
                {
                    changed = true;
                    _circuitsStale |= IsGrid(pair.Value.Kind);
                }
                else if (!before.Equals(pair.Value))
                {
                    changed = true;
                    _circuitsStale |= IsGrid(pair.Value.Kind) && !before.Key.Equals(pair.Value.Key);
                }
            }

            foreach (KeyValuePair<IElectricDevice, Seen> pair in _seen)
            {
                if (_current.ContainsKey(pair.Key))
                    continue;

                changed = true;
                _circuitsStale |= IsGrid(pair.Value.Kind);
                if (pair.Key is IPowerStorage storage)
                    _rates.Remove(storage);
                if (pair.Key is IApcChannelSource gone)
                    _breakables.Remove(gone);
            }

            (_seen, _current) = (_current, _seen);
            _resolveLinks = false;
            _producers.Clear();
            foreach (KeyValuePair<IElectricDevice, Seen> pair in _seen)
            {
                if (pair.Value.Kind is KindGenerator or KindSolar)
                    _producers.Add(pair.Key);
            }

            return changed;
        }

        private bool PollRooms()
        {
            _currentRooms.Clear();
            _roomScratch.Clear();
            _world.CollectRooms(_roomScratch);
            foreach (PowerSystemsViewRoomInfo info in _roomScratch)
            {
                if (info.AreaId == 0)
                    continue;

                IApcChannelSource apc = info.Apc != null && IsLive(info.Apc) ? info.Apc : null;
                _currentRooms[info.AreaId] = new SeenRoom(info.AreaId, info.Name, apc, apc != null && Broken(apc));
            }

            bool changed = _currentRooms.Count != _seenRooms.Count;
            if (!changed)
            {
                foreach (KeyValuePair<ushort, SeenRoom> pair in _currentRooms)
                {
                    if (!_seenRooms.TryGetValue(pair.Key, out SeenRoom before) || !before.Equals(pair.Value))
                    {
                        changed = true;
                        break;
                    }
                }
            }

            (_seenRooms, _currentRooms) = (_currentRooms, _seenRooms);
            return changed;
        }

        /// <summary>
        /// A device's badge as it is now. <paramref name="before"/> carries what a poll need not read again: its kind and
        /// name, which never change (an asset name is a fresh string per read), its tile object, and its APC and room
        /// until the topology moves (area membership and APC changes move it) or the device does. On MetaStation these cut a poll of ~1,800 devices from ~11 ms.
        /// </summary>
        private bool TryDescribe(IElectricDevice device, Seen? before, out Seen seen)
        {
            seen = default;
            PlacedTileObject placed = before?.Placed;
            if (!IsLive(device))
                return false;
            if (placed == null && !TryGetLiveTileObject(device, out placed))
                return false;
            if (placed.Connector is CablesAdjacencyConnector)
                return false;

            byte kind = before?.Kind ?? KindOf(device, placed);
            if (kind == 0)
                return false;

            IApcChannelSource apc = null;
            byte state;
            switch (device)
            {
                case IApcChannelSource apcDevice:
                    state = ApcState(apcDevice.Channels, Broken(apcDevice));
                    break;
                case IPowerStorage storage:
                    state = storage.IsOn ? Running : (byte)0;
                    break;
                case IPowerProducer producer:
                    state = producer.PowerProduction > 0f ? Running : (byte)0;
                    break;
                case IPowerConsumer consumer:
                    // Which APC feeds a consumer changes only with the room → APC index, which moves the topology.
                    if (before is { } fed && !_resolveLinks)
                        apc = fed.Apc;
                    else
                        _world.TryGetApc(consumer, out apc);
                    if (apc != null && !IsLive(apc))
                        apc = null;
                    state = ConsumerState(StatusOf(consumer, apc), consumer.Channel);
                    break;
                default:
                    return false;
            }

            SystemsViewKey key = new(placed.WorldOrigin, DeviceSlot(placed.Layer, placed.Direction));
            ushort room = before is { } known && !_resolveLinks && known.Key.Equals(key) ? known.Room
                : _world.TryGetRoomOf(device, out ushort areaId) ? areaId : (ushort)0;
            seen = new Seen(key, kind, state, placed.Direction, room, apc, before?.Name ?? NameOf(placed), placed);
            return true;
        }

        private static string NameOf(PlacedTileObject placed) =>
            placed.tileObjectSO != null && placed.tileObjectSO.PrefabAsset != null ? placed.NameString : null;

        private byte StatusOf(IPowerConsumer consumer, IApcChannelSource apc)
        {
            if (consumer.PowerStatus == PowerStatus.Powered)
                return Powered;

            // A broken APC reports every channel off, but nothing there was switched off: the room has no power.
            if (apc != null && !Broken(apc) && !PowerGate.IsChannelEnabled(consumer.Channel, apc.Channels))
                return ChannelOff;

            return Unpowered;
        }

        private static byte KindOf(IElectricDevice device, PlacedTileObject placed) => device switch
        {
            IApcChannelSource => KindApc,
            IPowerStorage => KindSmes,
            SolarPanel => KindSolar,
            IPowerProducer => KindGenerator,
            IPowerConsumer when placed.TryGetComponent(out LightPower _) => KindLight,
            IPowerConsumer => KindConsumer,
            _ => 0,
        };

        /// <summary>Every cable tile, its links to the cables beside it, and the runs they make.</summary>
        private void RescanCables(TileMap map)
        {
            _cablesStale = false;
            LastRescannedCables = true;
            _cableRun.Clear();
            _cableLinks.Clear();
            _runs.Clear();
            // The connectivity rule is ElectricCableConnectivity's, the one that links grid devices, so the view and the
            // circuits it colours (and Phase 4's problems) agree on what a cable run is.
            HashSet<Vector2Int> cables = new(ElectricCableConnectivity.EnumerateCableTiles(map));
            foreach (Vector2Int tile in cables)
            {
                byte links = 0;
                foreach (Direction side in SystemsViewLinks.Sides)
                {
                    if (cables.Contains(tile + SystemsViewLinks.Step(side)))
                        links |= SystemsViewLinks.Bit(side);
                }

                _cableLinks[tile] = links;
            }

            // Runs in a stable order (west to east), so the tracker's ties don't depend on hash order.
            List<Vector2Int> ordered = new(cables);
            ordered.Sort(CompareTiles);
            foreach (Vector2Int start in ordered)
            {
                if (_cableRun.ContainsKey(start))
                    continue;

                int run = _runs.Count;
                List<Vector2Int> tiles = new(ElectricCableConnectivity.CollectCableRun(map, start));
                tiles.Sort(CompareTiles);
                _runs.Add(tiles);
                foreach (Vector2Int tile in tiles)
                    _cableRun[tile] = run;
            }

            _circuitsStale = true;
        }

        /// <summary>
        /// Joins cable runs and grid devices into circuits: a device on a cable tile is on that run; devices the
        /// electricity graph put in one circuit join their runs (or, off any cable, each other). Then gives each circuit
        /// its view id.
        /// </summary>
        private void JoinCircuits()
        {
            _circuitsStale = false;
            LastRejoinedCircuits = true;
            int runCount = _runs.Count;
            List<IElectricDevice> grid = new();
            List<Seen> gridSeen = new();
            foreach (KeyValuePair<IElectricDevice, Seen> pair in _seen)
            {
                if (IsGrid(pair.Value.Kind))
                {
                    grid.Add(pair.Key);
                    gridSeen.Add(pair.Value);
                }
            }

            int[] parent = new int[runCount + grid.Count];
            for (int i = 0; i < parent.Length; i++)
                parent[i] = i;

            int[] nodeOf = new int[grid.Count];
            Dictionary<object, int> firstInCircuit = new();
            for (int i = 0; i < grid.Count; i++)
            {
                nodeOf[i] = _cableRun.TryGetValue(gridSeen[i].Key.Tile, out int run) ? run : runCount + i;
                object circuit = _world.CircuitOf(grid[i]);
                if (circuit == null)
                    continue;

                if (firstInCircuit.TryGetValue(circuit, out int first))
                    Union(parent, first, nodeOf[i]);
                else
                    firstInCircuit[circuit] = nodeOf[i];
            }

            Dictionary<int, CircuitNetwork> byRoot = new();
            for (int run = 0; run < runCount; run++)
            {
                CircuitNetwork network = NetworkFor(byRoot, Find(parent, run));
                foreach (Vector2Int tile in _runs[run])
                    network.Add(new SystemsViewKey(tile, CableSlot), tile, cable: true);
            }

            for (int i = 0; i < grid.Count; i++)
            {
                CircuitNetwork network = NetworkFor(byRoot, Find(parent, nodeOf[i]));
                network.Devices.Add(grid[i]);
                network.Add(gridSeen[i].Key, gridSeen[i].Key.Tile, cable: false);
            }

            _circuits.Clear();
            foreach (CircuitNetwork network in byRoot.Values)
            {
                // A device alone on no cable is on no circuit.
                if (network.CableTiles > 0 || network.Devices.Count > 1)
                    _circuits.Add(network);
            }

            _circuits.Sort((a, b) => CompareTiles(a.First, b.First));
            List<IEnumerable<SystemsViewKey>> segments = new(_circuits.Count);
            foreach (CircuitNetwork network in _circuits)
                segments.Add(network.Segments);

            int[] ids = _tracker.Assign(segments);
            _circuitByViewId.Clear();
            _circuitViewIdOf.Clear();
            _cableViewIdOf.Clear();
            for (int i = 0; i < _circuits.Count; i++)
            {
                CircuitNetwork network = _circuits[i];
                network.ViewId = ids[i];
                _circuitByViewId[ids[i]] = network;
                foreach (IElectricDevice device in network.Devices)
                    _circuitViewIdOf[device] = ids[i];
                foreach (SystemsViewKey key in network.Segments)
                {
                    if (key.Slot == CableSlot && _cableRun.ContainsKey(key.Tile))
                        _cableViewIdOf[key.Tile] = ids[i];
                }
            }
        }

        private static CircuitNetwork NetworkFor(Dictionary<int, CircuitNetwork> byRoot, int root)
        {
            if (!byRoot.TryGetValue(root, out CircuitNetwork network))
                byRoot[root] = network = new CircuitNetwork();
            return network;
        }

        private static int Find(int[] parent, int node)
        {
            while (parent[node] != node)
            {
                parent[node] = parent[parent[node]];
                node = parent[node];
            }

            return node;
        }

        // The lower root wins, so a component's root doesn't depend on join order.
        private static void Union(int[] parent, int a, int b)
        {
            int ra = Find(parent, a);
            int rb = Find(parent, b);
            if (ra == rb)
                return;

            if (ra < rb) parent[rb] = ra;
            else parent[ra] = rb;
        }

        private int CircuitViewIdOf(IElectricDevice device) =>
            device != null && _circuitViewIdOf.TryGetValue(device, out int viewId) ? viewId : 0;

        private string ApcDetail(IApcChannelSource apc, Seen seen)
        {
            if (Broken(apc))
                return "broken";

            StringBuilder detail = new();
            if (apc is IPowerStorage cell)
                detail.Append(Percent(cell.StoredEnergyKwh, cell.MaxCapacityKwh)).Append(" · ");
            if (seen.Room != 0 && _seenRooms.TryGetValue(seen.Room, out SeenRoom room))
                detail.Append(RoomLabel(room)).Append(" · ");
            detail.Append(ChannelWords(apc.Channels));
            return detail.ToString();
        }

        /// <summary>An SMES's charge, its rate in or out since the last read, and whether its output is on.</summary>
        private string SmesDetail(IPowerStorage storage)
        {
            long tick = _world.TickCount;
            float energy = storage.StoredEnergyKwh;
            bool advanced = false;
            if (!_rates.TryGetValue(storage, out Rate rate))
            {
                rate = new Rate { Tick = tick, Energy = energy };
            }
            else if (tick > rate.Tick && _world.TickSeconds > 0f)
            {
                advanced = true;
                float hours = (tick - rate.Tick) * _world.TickSeconds / 3600f;
                rate.Kw = (energy - rate.Energy) / hours;
                rate.Tick = tick;
                rate.Energy = energy;
            }

            _rates[storage] = rate;
            // Paused with no tick since the last read, the last rate would read as still flowing.
            string flow = _world.Paused && !advanced ? "paused"
                : rate.Kw > 0.05f ? "charging " + Kw(rate.Kw)
                : rate.Kw < -0.05f ? "discharging " + Kw(-rate.Kw)
                : "idle";
            return $"{Percent(energy, storage.MaxCapacityKwh)} · {flow} · output {(storage.IsOn ? "on" : "off")}";
        }

        private static string Balance(float supply, float demand, float stored)
        {
            float spare = supply - demand;
            if (spare >= -0.05f)
                return demand <= 0.05f && supply <= 0.05f ? "nothing on it" : Kw(Math.Max(0f, spare)) + " spare";

            return Kw(-spare) + (stored > 0f ? " short, from storage" : " short");
        }

        private static string Breaker(ApcControlFlags channels, ApcControlFlags channel, float loadKw) =>
            (channels & channel) != 0 ? "on · " + Kw(loadKw) : "off";

        private string DeviceLabel(Seen seen) =>
            seen.Kind is KindLight or KindConsumer && !string.IsNullOrEmpty(seen.Name) ? Words(seen.Name) : KindName(seen.Kind);

        private static string RoomLabel(SeenRoom room) => string.IsNullOrWhiteSpace(room.Name) ? "Room " + room.AreaId : room.Name;

        /// <summary>"LightFixtureTube" → "Light Fixture Tube": an asset name in words, for a consumer's row.</summary>
        public static string Words(string name)
        {
            StringBuilder words = new(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '_' || c == '-')
                {
                    words.Append(' ');
                    continue;
                }

                if (i > 0 && char.IsUpper(c) && (char.IsLower(name[i - 1]) || (i + 1 < name.Length && char.IsLower(name[i + 1]) && char.IsUpper(name[i - 1]))))
                    words.Append(' ');
                words.Append(c);
            }

            return words.ToString().Trim();
        }

        private static string Kw(float kw) => Number(kw, "0.0") + " kW";

        private static string Percent(float value, float max) => max > 0f ? Number(value / max * 100f, "0") + " %" : "0 %";

        private static string Number(float value, string format) => SystemsViewText.Number(value, format);

        private static int CompareTiles(Vector2Int a, Vector2Int b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y);

        /// <summary>A smashed APC (<see cref="BreakableDevice"/>): dead on its wall, every channel off.</summary>
        private bool Broken(IApcChannelSource apc)
        {
            if (apc is not Component component || component == null)
                return false;

            if (!_breakables.TryGetValue(apc, out BreakableDevice breakable))
            {
                breakable = component.TryGetComponent(out BreakableDevice found) ? found : null;
                _breakables[apc] = breakable;
            }

            return breakable != null && breakable.IsBroken;
        }

        // Interface-typed devices skip Unity's fake-null; a destroyed one throws on TileObject (electricity Pitfalls).
        private static bool IsLive(IElectricDevice device) => device != null && (device is not UnityEngine.Object unity || unity);

        private static bool TryGetLiveTileObject(IElectricDevice device, out PlacedTileObject placed)
        {
            placed = null;
            if (!IsLive(device))
                return false;

            placed = device.TileObject;
            return placed != null;
        }

        private readonly struct Seen : IEquatable<Seen>
        {
            public readonly SystemsViewKey Key;
            public readonly byte Kind;
            public readonly byte State;
            public readonly Direction Facing;
            public readonly ushort Room;
            /// <summary>A consumer's APC, compared by reference: the badge's circuit follows it.</summary>
            public readonly IApcChannelSource Apc;
            public readonly string Name;
            /// <summary>Its tile object, kept so a poll doesn't look it up again; not part of the comparison.</summary>
            public readonly PlacedTileObject Placed;

            public Seen(SystemsViewKey key, byte kind, byte state, Direction facing, ushort room, IApcChannelSource apc, string name,
                PlacedTileObject placed)
            {
                Placed = placed;
                Key = key;
                Kind = kind;
                State = state;
                Facing = facing;
                Room = room;
                Apc = apc;
                Name = name;
            }

            public bool Equals(Seen other) => Key.Equals(other.Key) && Kind == other.Kind && State == other.State
                && Facing == other.Facing && Room == other.Room && ReferenceEquals(Apc, other.Apc);
        }

        private readonly struct SeenRoom : IEquatable<SeenRoom>
        {
            public readonly ushort AreaId;
            public readonly string Name;
            public readonly IApcChannelSource Apc;
            public readonly bool ApcBroken;

            public SeenRoom(ushort areaId, string name, IApcChannelSource apc, bool apcBroken)
            {
                AreaId = areaId;
                Name = name;
                Apc = apc;
                ApcBroken = apcBroken;
            }

            public bool Equals(SeenRoom other) => AreaId == other.AreaId && string.Equals(Name, other.Name, StringComparison.Ordinal)
                && ReferenceEquals(Apc, other.Apc) && ApcBroken == other.ApcBroken;
        }

        private struct Rate
        {
            public long Tick;
            public float Energy;
            public float Kw;
        }

        private sealed class CircuitNetwork
        {
            public readonly List<SystemsViewKey> Segments = new();
            public readonly List<IElectricDevice> Devices = new();
            public Vector2Int First;
            public int CableTiles;
            public int ViewId;
            private bool _hasFirst;

            public void Add(SystemsViewKey key, Vector2Int tile, bool cable)
            {
                Segments.Add(key);
                if (cable)
                    CableTiles++;
                if (!_hasFirst || CompareTiles(tile, First) < 0)
                {
                    First = tile;
                    _hasFirst = true;
                }
            }
        }
    }
}
