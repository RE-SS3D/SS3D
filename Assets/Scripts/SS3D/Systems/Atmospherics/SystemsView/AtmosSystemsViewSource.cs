using System;
using System.Collections.Generic;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using static SS3D.Systems.Atmospherics.SystemsView.AtmosSystemsViewCodes;

namespace SS3D.Systems.Atmospherics.SystemsView
{
    /// <summary>
    /// Server: the Atmos view's structure (Documents/architecture/2026-11_map-editor-systems-views.md, Phase 1). Every
    /// segment of <see cref="GasPipeNetworkRegistry"/> becomes a run on its line (a manifold one per line it spans), with
    /// a view id from a <see cref="NetworkIdentityTracker{TSegment}"/> so colours survive the registry replacing its
    /// records on every edit. Every device in <see cref="AtmosPortRegistry"/> becomes a badge; codes are
    /// <see cref="AtmosSystemsViewCodes"/>.
    /// <para>
    /// Badge networks are resolved against the current registry and tilemap each build, never the ports' cached ids,
    /// which only catch up on the device's next tick. Device state has no change event, so <see cref="StructureVersion"/>
    /// polls the ports when the publisher asks (four times a second, and only while an editor watches) and moves when the
    /// topology or any device changed. A pipe edit walks only the records the registry replaced; a device added, removed,
    /// moved or turned re-links only the pipes beside it; a state change walks none. The networks of all three ports of a
    /// filter or mixer are on its badge, so the trace panel lists them.
    /// </para>
    /// <para>
    /// <see cref="ReadLive"/> (Phase 2's trace) reads a traced network's record from the current registry, through the
    /// view ids the last build gave, and lists the devices whose badges in that build touch it.
    /// </para>
    /// </summary>
    public sealed class AtmosSystemsViewSource : ISystemsViewSource
    {
        private readonly Func<TileMap> _map;
        private readonly Func<GasPipeNetworkRegistry> _registry;
        private readonly AtmosPortRegistry _ports;
        private readonly NetworkIdentityTracker<GasPipeSegmentKey> _tracker = new();
        // Runs per network record. The registry keeps a record object until an edit touches its network, so a record
        // seen before still has the same segments and links; only replaced ones are walked again.
        private readonly Dictionary<GasPipeNetworkRecord, NetworkRuns> _runs = new();
        private readonly Dictionary<GasPipeNetworkId, int> _viewIds = new();
        private readonly Dictionary<int, GasPipeNetworkId> _networkIds = new();
        // The badges of the last build, for the devices of a traced network.
        private readonly List<SystemsViewBadge> _badges = new();
        private readonly Func<bool> _paused;
        private readonly Func<int, string> _gasName;
        private readonly List<NetworkRuns> _ordered = new();
        private GasPipeNetworkRegistry _seenRegistry;
        private int _seenTopology;
        // What each device was at the last poll, keyed by the device so registration order never matters.
        private Dictionary<AtmosPortControllerBase, Seen> _seen = new();
        private Dictionary<AtmosPortControllerBase, Seen> _current = new();
        // Tiles where a device came, went or turned its ports: the networks beside them are walked again.
        private readonly HashSet<TileCoord> _touched = new();
        private bool _runsStale = true;
        private int _version;

        /// <param name="paused">Whether the atmos simulation is paused (<c>AtmosSubSystem.SimulationPaused</c>); never, when null.</param>
        /// <param name="gasName">A gas slot's short name for the mix rows; the core gases' symbols, when null.</param>
        public AtmosSystemsViewSource(Func<TileMap> map, Func<GasPipeNetworkRegistry> registry, AtmosPortRegistry ports,
            Func<bool> paused = null, Func<int, string> gasName = null)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _ports = ports ?? throw new ArgumentNullException(nameof(ports));
            _paused = paused;
            _gasName = gasName;
        }

        public SystemsViewKind View => SystemsViewKind.Atmos;

        public bool SimulationPaused => _paused != null && _paused();

        public int StructureVersion
        {
            get
            {
                Poll();
                return _version;
            }
        }

        /// <summary>Networks the last build walked again (all of them on a first build, none after a state change); for tests and budget logs.</summary>
        public int LastWalkedNetworks { get; private set; }

        /// <summary>Pipes the last build re-linked in place beside a device that came, went, moved or turned.</summary>
        public int LastRelinkedRuns { get; private set; }

        /// <summary>Builds from what the last <see cref="StructureVersion"/> read saw; the publisher reads it first.</summary>
        public SystemsViewStructure BuildStructure()
        {
            SystemsViewStructure structure = new(SystemsViewKind.Atmos);
            TileMap map = _map();
            GasPipeNetworkRegistry registry = _registry();
            if (map == null || registry == null)
                return structure;

            LastWalkedNetworks = 0;
            LastRelinkedRuns = 0;
            if (_runsStale)
                RebuildRuns(map, registry);

            foreach (NetworkRuns runs in _ordered)
            {
                structure.Set(new SystemsViewNetwork { ViewId = runs.ViewId, Label = $"{LineName(runs.Line)} {runs.ViewId}" });
                foreach (Run run in runs.Runs)
                    structure.Set(new SystemsViewSegment { Key = run.Key, ViewId = runs.ViewId, Links = run.Links });
            }

            _badges.Clear();
            IReadOnlyList<IAtmosPortDevice> ports = _ports.Ports;
            for (int i = 0; i < ports.Count; i++)
            {
                if (!TryDescribe(ports[i], out Device device))
                    continue;

                SystemsViewBadge badge = Badge(map, registry, device);
                structure.Set(badge);
                _badges.Add(badge);
            }

            return structure;
        }

        /// <summary>
        /// A traced network's pressure, temperature, mix and volume, read now from the current registry, and the devices
        /// on it as the last build saw them, each with its state and the side it touches. Values are formatted at the
        /// panel's precision, so the live stream only sends what a reader would see change.
        /// </summary>
        public bool ReadLive(int viewId, out SystemsViewLiveNetwork live)
        {
            live = default;
            GasPipeNetworkRegistry registry = _registry();
            TileMap map = _map();
            if (viewId <= 0 || registry == null || map == null)
                return false;

            // An edit since the last build replaced records: give them their view ids first, or a network that only
            // changed shape would read as gone. The version moves too, so the next publish sends the new structure.
            if (registry != _seenRegistry || registry.TopologyVersion != _seenTopology)
            {
                Poll();
                if (_runsStale)
                    RebuildRuns(map, registry);
            }

            if (!_networkIds.TryGetValue(viewId, out GasPipeNetworkId id)
                || !registry.TryGetNetwork(id, out GasPipeNetworkRecord record))
                return false;

            List<SystemsViewLiveRow> rows = new();
            int gases = record.Moles.Length;
            float total = record.GetTotalMoles(gases);
            rows.Add(new SystemsViewLiveRow("Pressure", Number(record.GetPressure(gases), "0.0") + " kPa"));
            rows.Add(new SystemsViewLiveRow("Temperature",
                $"{Number(record.Temperature, "0.0")} K ({Number(record.Temperature - 273.15f, "0")} °C)"));
            rows.Add(new SystemsViewLiveRow("Volume", Number(record.Volume, "0.0") + " m³"));
            if (total <= 1e-4f)
            {
                rows.Add(new SystemsViewLiveRow("Mix", "empty"));
            }
            else
            {
                for (int gas = 0; gas < gases; gas++)
                {
                    float share = record.Moles[gas] / total * 100f;
                    if (share >= 0.05f)
                        rows.Add(new SystemsViewLiveRow(GasName(gas), Number(share, share >= 10f ? "0" : "0.0") + " %"));
                }
            }

            List<SystemsViewLiveDevice> devices = new();
            foreach (SystemsViewBadge badge in _badges)
            {
                // Codes keep a one-network device's other ports at 0, so this is the side it touches.
                int port = badge.PortOf(viewId);
                if (port < 0)
                    continue;

                string detail = StateWords(badge.Kind, badge.State);
                if (IsTwoNetwork(badge.Kind))
                    detail += " · " + PortName(badge.Kind, (byte)port).ToLowerInvariant();
                devices.Add(new SystemsViewLiveDevice { Key = badge.Key, Label = KindName(badge.Kind), Detail = detail });
            }

            SystemsViewText.SortWestToEast(devices);
            live = new SystemsViewLiveNetwork { ViewId = viewId, Rows = rows.ToArray(), Devices = devices.ToArray() };
            return true;
        }

        private string GasName(int gas)
        {
            string name = _gasName?.Invoke(gas);
            if (!string.IsNullOrEmpty(name))
                return name;

            return gas < GasDefaults.Core.Length ? GasDefaults.Core[gas].Symbol : $"Gas {gas}";
        }

        private static string Number(float value, string format) => SystemsViewText.Number(value, format);

        private void Poll()
        {
            GasPipeNetworkRegistry registry = _registry();
            if (registry != _seenRegistry)
            {
                // A new round or map: a fresh registry, so nothing carries over.
                _seenRegistry = registry;
                _seenTopology = registry?.TopologyVersion ?? 0;
                _tracker.Clear();
                _runs.Clear();
                _touched.Clear();
                _viewIds.Clear();
                _networkIds.Clear();
                _badges.Clear();
                _runsStale = true;
                _version++;
            }
            else if (registry != null && registry.TopologyVersion != _seenTopology)
            {
                _seenTopology = registry.TopologyVersion;
                _runsStale = true;
                _version++;
            }

            if (PollDevices())
                _version++;
        }

        /// <summary>
        /// Compares every device with what it was at the last poll. A device added, removed or moved, or a pipe-port
        /// device turned, can change the links of the pipes beside it without a topology rebuild (a pipe draws into a
        /// port), so its tiles are marked for the next build to re-link the pipes there. Anything else that
        /// changed (switch, mode, a vent's facing) is a badge change only. Returns whether anything changed.
        /// </summary>
        private bool PollDevices()
        {
            _current.Clear();
            IReadOnlyList<IAtmosPortDevice> ports = _ports.Ports;
            for (int i = 0; i < ports.Count; i++)
            {
                if (TryDescribe(ports[i], out Device device))
                    _current[device.Port] = new Seen(device);
            }

            bool changed = false;
            foreach (KeyValuePair<AtmosPortControllerBase, Seen> pair in _current)
            {
                Seen now = pair.Value;
                if (!_seen.TryGetValue(pair.Key, out Seen before))
                {
                    Touch(now.Coord);
                    changed = true;
                }
                else if (!before.Coord.Equals(now.Coord) || before.Slot != now.Slot || before.Kind != now.Kind
                    || (now.HasPipePorts && before.Facing != now.Facing))
                {
                    Touch(before.Coord);
                    Touch(now.Coord);
                    changed = true;
                }
                else if (before.State != now.State || before.Facing != now.Facing)
                {
                    changed = true;
                }
            }

            foreach (KeyValuePair<AtmosPortControllerBase, Seen> pair in _seen)
            {
                if (!_current.ContainsKey(pair.Key))
                {
                    Touch(pair.Value.Coord);
                    changed = true;
                }
            }

            (_seen, _current) = (_current, _seen);
            return changed;
        }

        private void Touch(TileCoord coord)
        {
            _touched.Add(coord);
            _runsStale = true;
        }

        /// <summary>
        /// Re-reads the links of the pipes on and beside each touched tile, in place in their network's cached runs: a
        /// device changes only its neighbours' links, never which network a pipe is in (that would be a topology bump).
        /// A network not cached yet is walked whole anyway.
        /// </summary>
        private void RelinkTouchedRuns(TileMap map, GasPipeNetworkRegistry registry)
        {
            int relinked = 0;
            foreach (TileCoord coord in _touched)
            {
                foreach (TileCoord around in AtmosPipeConnectivity.EnumerateCoordsAround(map, coord))
                {
                    foreach (TileLayer layer in AtmosPipeConnectivity.GasPipeLayers)
                    {
                        if (!TryGetLineSlot(layer, out byte slot)
                            || !AtmosPipeConnectivity.TryGetSegment(map, around, layer, out PlacedTileObject pipe)
                            || !registry.TryGetNetworkForSegment(GasPipeSegmentKey.From(pipe), out _, out GasPipeNetworkRecord record)
                            || !_runs.TryGetValue(record, out NetworkRuns runs)
                            || !runs.Index.TryGetValue(new SystemsViewKey(around.Grid, slot), out int index))
                            continue;

                        runs.Runs[index] = new Run(runs.Runs[index].Key, Links(map, pipe));
                        relinked++;
                    }
                }
            }

            _touched.Clear();
            LastRelinkedRuns = relinked;
        }

        private void RebuildRuns(TileMap map, GasPipeNetworkRegistry registry)
        {
            _runsStale = false;
            RelinkTouchedRuns(map, registry);
            HashSet<GasPipeNetworkRecord> live = new();
            _ordered.Clear();
            int walked = 0;
            foreach (GasPipeNetworkRecord record in registry.NetworkRecords)
            {
                live.Add(record);
                if (!_runs.TryGetValue(record, out NetworkRuns runs))
                {
                    _runs[record] = runs = Walk(map, record);
                    walked++;
                }

                _ordered.Add(runs);
            }

            List<GasPipeNetworkRecord> gone = null;
            foreach (GasPipeNetworkRecord record in _runs.Keys)
            {
                if (!live.Contains(record))
                    (gone ??= new List<GasPipeNetworkRecord>()).Add(record);
            }

            if (gone != null)
            {
                foreach (GasPipeNetworkRecord record in gone)
                    _runs.Remove(record);
            }

            // A stable order for the tracker's ties: by each network's first tile.
            _ordered.Sort((a, b) => a.First.x != b.First.x ? a.First.x.CompareTo(b.First.x)
                : a.First.y != b.First.y ? a.First.y.CompareTo(b.First.y)
                : a.Line != b.Line ? a.Line.CompareTo(b.Line)
                : a.Record.Id.Value.CompareTo(b.Record.Id.Value));
            List<IEnumerable<GasPipeSegmentKey>> networks = new(_ordered.Count);
            foreach (NetworkRuns runs in _ordered)
                networks.Add(runs.Record.Segments);

            int[] ids = _tracker.Assign(networks);
            _viewIds.Clear();
            _networkIds.Clear();
            for (int i = 0; i < _ordered.Count; i++)
            {
                _ordered[i].ViewId = ids[i];
                _viewIds[_ordered[i].Record.Id] = ids[i];
                _networkIds[ids[i]] = _ordered[i].Record.Id;
            }

            LastWalkedNetworks = walked;
        }

        /// <summary>One network's runs: each pipe on its line with the sides it really joins, each manifold on every line it spans.</summary>
        private static NetworkRuns Walk(TileMap map, GasPipeNetworkRecord record)
        {
            NetworkRuns runs = new(record);
            int[] perLine = new int[4];
            bool hasFirst = false;
            foreach (GasPipeSegmentKey key in record.Segments)
            {
                Vector2Int tile = key.Coord.Grid;
                if (!hasFirst || tile.x < runs.First.x || (tile.x == runs.First.x && tile.y < runs.First.y))
                {
                    runs.First = tile;
                    hasFirst = true;
                }

                if (key.Layer == TileLayer.PipeManifold)
                {
                    if (!AtmosPipeConnectivity.TryGetManifold(map, key.Coord, out GasPipeManifold manifold))
                        continue;

                    Direction facing = manifold.PlacedObject.Direction;
                    byte links = (byte)(SystemsViewLinks.Bit(facing) | SystemsViewLinks.Bit(TileHelper.GetOpposite(facing)));
                    foreach (TileLayer line in manifold.PortLines)
                    {
                        if (TryGetLineSlot(line, out byte slot))
                            runs.Add(new Run(new SystemsViewKey(tile, (byte)(ManifoldSlotBase + slot)), links));
                    }

                    continue;
                }

                if (!TryGetLineSlot(key.Layer, out byte lineSlot))
                    continue;

                perLine[lineSlot]++;
                byte pipeLinks = AtmosPipeConnectivity.TryGetSegment(map, key.Coord, key.Layer, out PlacedTileObject pipe) ? Links(map, pipe) : (byte)0;
                runs.Add(new Run(new SystemsViewKey(tile, lineSlot), pipeLinks));
            }

            // The label names the line most of the network runs on (Supply first on a tie, the palette's order).
            byte best = Supply;
            for (byte line = Supply; line <= Surface; line++)
            {
                if (perLine[line] > perLine[best])
                    best = line;
            }

            if (perLine[best] == 0 && runs.Runs.Count > 0)
                best = LineOf(runs.Runs[0].Key.Slot, out _);

            runs.Line = best;
            return runs;
        }

        /// <summary>The sides a pipe joins, as its mesh does: same-line neighbours, device ports and manifold ports.</summary>
        private static byte Links(TileMap map, PlacedTileObject pipe)
        {
            if (pipe.Connector is not IEngineDrivenAdjacency { ConnectionRule: { } rule })
                return 0;

            AdjacencyMap adjacency = AdjacencyEngine.ComputeAdjacencyMap(pipe, rule, map);
            byte links = 0;
            foreach (Direction side in SystemsViewLinks.Sides)
            {
                if (adjacency.HasConnection(side))
                    links |= SystemsViewLinks.Bit(side);
            }

            return links;
        }

        private SystemsViewBadge Badge(TileMap map, GasPipeNetworkRegistry registry, Device device)
        {
            GasPipeNetworkId first = GasPipeNetworkId.None;
            GasPipeNetworkId second = GasPipeNetworkId.None;
            GasPipeNetworkId side = GasPipeNetworkId.None;
            switch (device.Port)
            {
                case AtmosInlineDeviceBase:
                    AtmosDevicePipeResolver.TryResolveInlineNetworks(map, registry, device.Coord, device.Facing, out first, out second);
                    break;
                case AtmosThreeWayDeviceBase threeWay:
                    AtmosDevicePipeResolver.TryResolveThreeWayNetworks(map, registry, device.Coord, device.Facing,
                        threeWay.SideOnLeft, threeWay.PipeLayers, out first, out second, out side);
                    break;
                default:
                    AtmosDevicePipeResolver.TryResolveNetwork(map, registry, device.Coord, device.Port.PipeLayers, out first, out _);
                    break;
            }

            return new SystemsViewBadge
            {
                Key = new SystemsViewKey(device.Tile, device.Slot),
                Kind = device.Kind,
                State = device.State,
                Facing = device.Facing,
                ViewId = ViewIdOf(first),
                OtherViewId = IsTwoNetwork(device.Kind) ? ViewIdOf(second) : 0,
                SideViewId = ViewIdOf(side),
            };
        }

        private int ViewIdOf(GasPipeNetworkId id) => !id.IsNone && _viewIds.TryGetValue(id, out int viewId) ? viewId : 0;

        /// <summary>A port's badge code, state and place; false for a port the view has no badge for.</summary>
        public static bool TryDescribe(IAtmosPortDevice port, out Device device)
        {
            device = default;
            if (port is not AtmosPortControllerBase controller || controller == null
                || !controller.TryGetComponent(out PlacedTileObject placed))
                return false;

            byte kind = controller switch
            {
                VentController => KindVent,
                ScrubberController => KindScrubber,
                AtmosPumpController => KindPump,
                AtmosValveController => KindValve,
                AtmosFilterController => KindFilter,
                AtmosMixerController => KindMixer,
                _ => 0,
            };
            if (kind == 0)
                return false;

            byte switchState = !controller.IsEnabled ? Off : !controller.IsPowered() ? NoPower : On;
            byte mode = controller switch
            {
                ScrubberController scrubber => IsSiphoning(scrubber) ? ModeSiphon : ModeScrub,
                AtmosPumpController pump => pump.Regulation == PumpRegulation.Volume ? ModeVolume : ModePressure,
                _ => 0,
            };
            bool sideOnLeft = controller is AtmosThreeWayDeviceBase threeWay && threeWay.SideOnLeft;
            device = new Device
            {
                Port = controller,
                Coord = new TileCoord(placed.MapId, placed.WorldOrigin),
                Slot = (byte)placed.Layer,
                Kind = kind,
                State = State(switchState, mode, sideOnLeft),
                Facing = placed.Direction,
            };
            return true;
        }

        /// <summary>The air alarm's panic settings: flat out on every gas.</summary>
        private static bool IsSiphoning(ScrubberController scrubber)
        {
            scrubber.GetFilterStates(out bool o2, out bool n2, out bool co2, out bool toxins);
            return scrubber.FlowRate >= AtmosPortConstants.ScrubberMaxFlowRate && o2 && n2 && co2 && toxins;
        }

        public struct Device
        {
            public AtmosPortControllerBase Port;
            public TileCoord Coord;
            public byte Slot;
            public byte Kind;
            public byte State;
            public Direction Facing;

            public Vector2Int Tile => Coord.Grid;
        }

        private readonly struct Seen
        {
            public readonly TileCoord Coord;
            public readonly byte Slot;
            public readonly byte Kind;
            public readonly byte State;
            public readonly Direction Facing;
            // Pipes draw into this device's ports, so turning it changes their links.
            public readonly bool HasPipePorts;

            public Seen(Device device)
            {
                Coord = device.Coord;
                Slot = device.Slot;
                Kind = device.Kind;
                State = device.State;
                Facing = device.Facing;
                HasPipePorts = device.Port is IPipePortDevice;
            }
        }

        private readonly struct Run
        {
            public readonly SystemsViewKey Key;
            public readonly byte Links;

            public Run(SystemsViewKey key, byte links)
            {
                Key = key;
                Links = links;
            }
        }

        private sealed class NetworkRuns
        {
            public readonly GasPipeNetworkRecord Record;
            public readonly List<Run> Runs = new();
            /// <summary>Where each run sits in <see cref="Runs"/>, for re-linking in place.</summary>
            public readonly Dictionary<SystemsViewKey, int> Index = new();
            public Vector2Int First;
            public byte Line;
            public int ViewId;

            public NetworkRuns(GasPipeNetworkRecord record) => Record = record;

            public void Add(Run run)
            {
                Index[run.Key] = Runs.Count;
                Runs.Add(run);
            }
        }
    }
}
