using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using SS3D.Systems.Electricity;
using SS3D.Systems.Electricity.SystemsView;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.MapEditor;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using static SS3D.Systems.Electricity.SystemsView.PowerSystemsViewCodes;
using Object = UnityEngine.Object;

namespace EditorTests
{
    /// <summary>
    /// The Power view's source (Documents/architecture/2026-11_map-editor-systems-views.md, Phase 3): cable circuits with
    /// their grid devices, rooms tinted by their APC's circuit or hatched, consumer status dots, colours that stay put
    /// through a cable edit, the trace's live rows, and the structure moving after a world resumption.
    /// </summary>
    public class PowerSystemsViewSourceTests
    {
        private List<GameObject> _instantiated;
        private TileMapTestUtilities.MapContext _context;
        private World _world;
        private PowerSystemsViewSource _source;
        private Producer _generator;
        private Storage _smes;
        private Apc _apc;
        private Consumer _lit;
        private Consumer _switchedOff;
        private Consumer _starved;
        private Consumer _storeroom;
        private readonly object _circuit = new();

        private const ushort Bridge = 1;
        private const ushort Storeroom = 2;
        private static int BridgeId => RoomViewId(Bridge);
        private static int StoreroomId => RoomViewId(Storeroom);

        // Row y = 0: cables 0..4 with a generator on 0, an SMES on 2 and the Bridge's APC on 4; a lone run at 8..9.
        // Row y = 3: three Bridge consumers (lit, equipment switched off at the APC, starved) and one in the Storeroom,
        // which has no APC.
        [SetUp]
        public void SetUp()
        {
            _instantiated = new List<GameObject>();
            _context = TileMapTestUtilities.CreateContext(_instantiated);
            foreach (int x in new[] { 0, 1, 2, 3, 4, 8, 9 })
                PlaceCable(x, 0);

            _generator = new Producer(Place(0, 0, TileLayer.FurnitureBase, Direction.North)) { Output = 10f };
            _smes = new Storage(Place(2, 0, TileLayer.FurnitureBase, Direction.North)) { Stored = 50f, Capacity = 100f };
            _apc = new Apc(Place(4, 0, TileLayer.WallMountHigh, Direction.North))
                { Channels = ApcControlFlags.Lighting | ApcControlFlags.Environment, Stored = 4f, Capacity = 5f };
            _lit = new Consumer(Place(1, 3, TileLayer.FurnitureBase, Direction.North), PowerChannel.Lighting, 0.4f) { PowerStatus = PowerStatus.Powered };
            _switchedOff = new Consumer(Place(2, 3, TileLayer.FurnitureBase, Direction.North), PowerChannel.Equipment, 1f);
            _starved = new Consumer(Place(3, 3, TileLayer.FurnitureBase, Direction.North), PowerChannel.Environment, 0.5f);
            _storeroom = new Consumer(Place(6, 3, TileLayer.FurnitureBase, Direction.North), PowerChannel.Equipment, 1f);

            _world = new World { Map = _context.Map };
            foreach (IElectricDevice device in new IElectricDevice[] { _generator, _smes, _apc })
            {
                _world.DeviceList.Add(device);
                _world.Circuits[device] = _circuit;
            }

            foreach (Consumer consumer in new[] { _lit, _switchedOff, _starved })
            {
                _world.DeviceList.Add(consumer);
                _world.ApcOf[consumer] = _apc;
                _world.RoomOf[consumer] = Bridge;
            }

            _world.DeviceList.Add(_storeroom);
            _world.RoomOf[_storeroom] = Storeroom;
            _world.RoomOf[_apc] = Bridge;
            _world.Rooms.Add(new PowerSystemsViewRoomInfo(Bridge, "Bridge", _apc));
            _world.Rooms.Add(new PowerSystemsViewRoomInfo(Storeroom, "Storeroom", null));
            _world.Stats[_apc] = new CircuitStats
            {
                TotalSupplyKw = 2.5f, GridAvailableKw = 10f, TotalDemandKw = 2.5f, ApcBatteryCharge = 0.8f,
                LightingLoadKw = 0.4f, EnvironmentLoadKw = 0.5f, GridMeetsLoad = true,
            };
            _source = new PowerSystemsViewSource(_world);
        }

        [TearDown]
        public void TearDown()
        {
            PowerSystemsViewStyle.Instance.AreaAt = _defaultAreaAt;
            foreach (GameObject go in _instantiated)
                Object.DestroyImmediate(go);
            _instantiated.Clear();
        }

        private readonly System.Func<Vector2Int, ushort> _defaultAreaAt = PowerSystemsViewStyle.Instance.AreaAt;

        // As the publisher does: read the version (which polls), then build.
        private SystemsViewStructure Build()
        {
            _ = _source.StructureVersion;
            return _source.BuildStructure();
        }

        private static SystemsViewKey Cable(int x) => new(new Vector2Int(x, 0), CableSlot);

        private static SystemsViewKey KeyAt(int x, int y, TileLayer layer = TileLayer.FurnitureBase, Direction facing = Direction.North) =>
            new(new Vector2Int(x, y), DeviceSlot(layer, facing));

        [Test]
        public void Structure_HasCircuits_GridDevices_Rooms_AndAConsumerInEachStatus()
        {
            SystemsViewStructure structure = Build();

            Assert.AreEqual(SystemsViewKind.Power, structure.View);
            Assert.AreEqual("Circuit 1", structure.Networks[1].Label, "The west run, its generator, SMES and APC");
            Assert.AreEqual("Circuit 2", structure.Networks[2].Label, "A run with no device is still a circuit");
            Assert.AreEqual("Bridge", structure.Networks[BridgeId].Label, "A room is a network of its own, named after it");
            Assert.AreEqual("Storeroom", structure.Networks[StoreroomId].Label);
            Assert.AreEqual(4, structure.Networks.Count);

            Assert.AreEqual(7, structure.Segments.Count);
            Assert.AreEqual(SystemsViewLinks.East, structure.Segments[Cable(0)].Links);
            Assert.AreEqual(SystemsViewLinks.East | SystemsViewLinks.West, structure.Segments[Cable(2)].Links);
            Assert.AreEqual(SystemsViewLinks.West, structure.Segments[Cable(4)].Links);
            Assert.AreEqual(1, structure.Segments[Cable(3)].ViewId);
            Assert.AreEqual(2, structure.Segments[Cable(9)].ViewId);

            SystemsViewBadge generator = structure.Badges[KeyAt(0, 0)];
            Assert.AreEqual(KindGenerator, generator.Kind);
            Assert.AreEqual(Running, generator.State);
            Assert.AreEqual(1, generator.ViewId);
            Assert.AreEqual(KindSmes, structure.Badges[KeyAt(2, 0)].Kind);
            SystemsViewBadge apc = structure.Badges[KeyAt(4, 0, TileLayer.WallMountHigh)];
            Assert.AreEqual(KindApc, apc.Kind);
            Assert.AreEqual(ApcControlFlags.Lighting | ApcControlFlags.Environment, ApcChannelsOf(apc.State));
            Assert.AreEqual(1, apc.ViewId, "An APC shows the circuit it hangs off");
            Assert.AreEqual(BridgeId, apc.OtherViewId, "and its room");

            AssertConsumer(structure.Badges[KeyAt(1, 3)], Powered, 1, BridgeId);
            AssertConsumer(structure.Badges[KeyAt(2, 3)], ChannelOff, 1, BridgeId);
            AssertConsumer(structure.Badges[KeyAt(3, 3)], Unpowered, 1, BridgeId);
            AssertConsumer(structure.Badges[KeyAt(6, 3)], Unpowered, 0, StoreroomId);
            Assert.AreEqual(7, structure.Badges.Count);

            SystemsViewRoom bridge = structure.Rooms[Bridge];
            Assert.AreEqual(BridgeId, bridge.ViewId);
            Assert.AreEqual(1, bridge.CircuitViewId, "The room is tinted with its APC's circuit");
            Assert.AreEqual(RoomHasApc, bridge.Flags);
            Assert.IsFalse(bridge.IsHatched);
            SystemsViewRoom storeroom = structure.Rooms[Storeroom];
            Assert.AreEqual(0, storeroom.CircuitViewId);
            Assert.IsTrue(storeroom.IsHatched, "A room with no APC is hatched");

            Assert.AreEqual("Machine: equipment channel off · Bridge · Circuit 1",
                PowerSystemsViewStyle.Instance.Describe(structure, new Vector2Int(2, 3)));
            Assert.AreEqual("Machine: unpowered · Storeroom · no APC",
                PowerSystemsViewStyle.Instance.Describe(structure, new Vector2Int(6, 3)));
            Assert.AreEqual("APC: lighting on, equipment off, environment on · Bridge · Circuit 1\nCable, part of Circuit 1",
                PowerSystemsViewStyle.Instance.Describe(structure, new Vector2Int(4, 0)));
        }

        private static void AssertConsumer(SystemsViewBadge badge, byte status, int circuit, int room)
        {
            Assert.AreEqual(KindConsumer, badge.Kind);
            Assert.AreEqual(status, StatusOf(badge.State), StatusWords(badge.State));
            Assert.AreEqual(circuit, badge.ViewId, "A consumer carries its APC's circuit");
            Assert.AreEqual(room, badge.OtherViewId, "and its room");
        }

        [Test]
        public void ACableCut_KeepsTheLargerSidesColour_AndRejoiningGivesItBack()
        {
            Build();
            int version = _source.StructureVersion;
            Assert.AreEqual(version, _source.StructureVersion, "Nothing changed: the version holds");

            // Cut the west run at 3, as the graph then sees it: the APC on a circuit of its own.
            _context.Map.GetOrCreateTileLocation(TileLayer.Wire, new Vector3(3, 0, 0)).ClearAllPlacedObject();
            _world.Circuits[_apc] = new object();
            _world.TopologyVersion++;
            Assert.AreNotEqual(version, _source.StructureVersion);
            SystemsViewStructure cut = _source.BuildStructure();
            Assert.IsTrue(_source.LastRescannedCables);
            Assert.AreEqual(1, cut.Segments[Cable(0)].ViewId, "The larger side keeps its colour");
            Assert.AreEqual(1, cut.Badges[KeyAt(2, 0)].ViewId);
            Assert.AreEqual(2, cut.Segments[Cable(8)].ViewId, "An untouched circuit keeps its colour");
            Assert.AreEqual(3, cut.Segments[Cable(4)].ViewId, "The cut-off side gets a fresh one");
            Assert.AreEqual(3, cut.Badges[KeyAt(4, 0, TileLayer.WallMountHigh)].ViewId);
            Assert.AreEqual(3, cut.Rooms[Bridge].CircuitViewId, "The room follows its APC's circuit");
            Assert.AreEqual(3, cut.Badges[KeyAt(1, 3)].ViewId, "and so do its consumers");
            Assert.IsFalse(cut.Segments.ContainsKey(Cable(3)));

            PlaceCable(3, 0);
            _world.Circuits[_apc] = _circuit;
            _world.TopologyVersion++;
            SystemsViewStructure joined = Build();
            Assert.AreEqual(1, joined.Segments[Cable(4)].ViewId, "Rejoined, the larger part's colour wins");
            Assert.AreEqual(1, joined.Rooms[Bridge].CircuitViewId);
            Assert.IsFalse(joined.Networks.ContainsKey(3));
        }

        [Test]
        public void AConsumerStatusChange_MovesTheVersion_AndChangesOnlyItsBadge()
        {
            SystemsViewStructure before = Build();
            int version = _source.StructureVersion;

            // Nothing announced it yet: an idle poll reads only producers, so the version holds.
            _starved.PowerStatus = PowerStatus.Powered;
            Assert.AreEqual(version, _source.StructureVersion, "An idle poll doesn't read consumers");
            Assert.IsFalse(_source.LastPolledAllDevices);
            // The electricity tick bumps the state version when it writes a status (PowerStateVersion).
            _world.StateVersion++;
            Assert.AreNotEqual(version, _source.StructureVersion, "A consumer powered moves the version");
            Assert.IsTrue(_source.LastPolledAllDevices);
            SystemsViewStructure after = _source.BuildStructure();
            Assert.IsFalse(_source.LastRescannedCables, "A state change scans no cable");
            Assert.IsFalse(_source.LastRejoinedCircuits, "and joins no circuit");
            SystemsViewDelta delta = SystemsViewStructure.Diff(before, after);
            Assert.IsEmpty(delta.SegmentsSet);
            Assert.IsEmpty(delta.RoomsSet);
            Assert.AreEqual(1, delta.BadgesSet.Length);
            Assert.AreEqual(KeyAt(3, 3), delta.BadgesSet[0].Key);

            // An APC's breaker switched: its badge, and the consumer whose channel it is.
            version = _source.StructureVersion;
            _apc.Channels = ApcControlFlags.All;
            _world.StateVersion++;
            Assert.AreNotEqual(version, _source.StructureVersion);
            delta = SystemsViewStructure.Diff(after, _source.BuildStructure());
            CollectionAssert.AreEquivalent(new[] { KeyAt(4, 0, TileLayer.WallMountHigh), KeyAt(2, 3) },
                delta.BadgesSet.Select(badge => badge.Key));

            // A generator stopping is read on an idle poll: producers have no single write to announce it.
            version = _source.StructureVersion;
            _generator.Output = 0f;
            Assert.AreNotEqual(version, _source.StructureVersion, "A producer is read on every poll");
            Assert.IsFalse(_source.LastPolledAllDevices);
            Assert.AreEqual(0, _source.BuildStructure().Badges[KeyAt(0, 0)].State);

            // A room gains an APC: only the room changes (and the consumer now carries the circuit).
            version = _source.StructureVersion;
            _world.Rooms[1] = new PowerSystemsViewRoomInfo(Storeroom, "Storeroom", _apc);
            Assert.AreNotEqual(version, _source.StructureVersion);
            Assert.AreEqual(1, _source.BuildStructure().Rooms[Storeroom].CircuitViewId);
        }

        [Test]
        public void ReadLive_GivesSupplyAgainstDemand_Storage_AndTheCircuitsDevices()
        {
            Build();
            Assert.IsTrue(_source.ReadLive(1, out SystemsViewLiveNetwork live));
            Assert.AreEqual(1, live.ViewId);
            Assert.AreEqual("10.0 kW", live.ValueOf("Supply"));
            Assert.AreEqual("2.5 kW", live.ValueOf("Demand"), "Demand is what the APCs on it draw for their rooms");
            Assert.AreEqual("7.5 kW spare", live.ValueOf("Balance"));
            Assert.AreEqual("50.0 of 100.0 kWh (50 %)", live.ValueOf("Stored"));
            Assert.AreEqual("5 tiles", live.ValueOf("Cable"));
            CollectionAssert.AreEqual(new[] { "Generator", "SMES", "APC" }, live.Devices.Select(d => d.Label));
            Assert.AreEqual("10.0 kW", live.Devices[0].Detail);
            Assert.AreEqual("50 % · idle · output on", live.Devices[1].Detail);
            Assert.AreEqual("80 % · Bridge · lighting on, equipment off, environment on", live.Devices[2].Detail);

            // Ten ticks of 0.2 s later the SMES holds 0.01 kWh more: it is charging at 18 kW.
            _smes.Stored += 0.01f;
            _world.TickCount += 10;
            Assert.IsTrue(_source.ReadLive(1, out live));
            StringAssert.StartsWith("50 % · charging 18.0 kW", live.Devices[1].Detail);

            // Paused, with no tick since: the rate doesn't read as still flowing.
            _world.Paused = true;
            Assert.IsTrue(_source.ReadLive(1, out live));
            Assert.AreEqual("50 % · paused · output on", live.Devices[1].Detail);
            _world.Paused = false;

            Assert.IsTrue(_source.ReadLive(2, out SystemsViewLiveNetwork lone));
            Assert.AreEqual("nothing on it", lone.ValueOf("Balance"));
            Assert.IsEmpty(lone.Devices);
            Assert.IsFalse(_source.ReadLive(99, out _), "An id the build didn't give names nothing");
        }

        [Test]
        public void ReadLive_OnARoom_GivesItsApc_AndTheConsumersItPowers()
        {
            Build();
            Assert.IsTrue(_source.ReadLive(BridgeId, out SystemsViewLiveNetwork bridge));
            Assert.AreEqual("on Circuit 1", bridge.ValueOf("APC"));
            Assert.AreEqual("working", bridge.ValueOf("Condition"));
            Assert.AreEqual("80 % (4.00 of 5.0 kWh)", bridge.ValueOf("Charge"));
            Assert.AreEqual("on · 0.4 kW", bridge.ValueOf("Lighting"));
            Assert.AreEqual("off", bridge.ValueOf("Equipment"));
            Assert.AreEqual("2.5 of 10.0 kW", bridge.ValueOf("From the grid"));
            Assert.AreEqual("the grid covers the load", bridge.ValueOf("State"));
            CollectionAssert.AreEqual(new[] { KeyAt(4, 0, TileLayer.WallMountHigh), KeyAt(1, 3), KeyAt(2, 3), KeyAt(3, 3) },
                bridge.Devices.Select(d => d.Key), "The APC first, then its consumers west to east");
            Assert.AreEqual("powered · Lighting 0.4 kW", bridge.Devices[1].Detail);
            Assert.AreEqual("equipment channel off · Equipment 1.0 kW", bridge.Devices[2].Detail);
            Assert.AreEqual("unpowered · Environment 0.5 kW", bridge.Devices[3].Detail);

            Assert.IsTrue(_source.ReadLive(StoreroomId, out SystemsViewLiveNetwork storeroom));
            Assert.AreEqual("none: nothing here gets power", storeroom.ValueOf("APC"));
            CollectionAssert.AreEqual(new[] { KeyAt(6, 3) }, storeroom.Devices.Select(d => d.Key));
            Assert.IsFalse(_source.ReadLive(RoomViewId(7), out _), "A room that doesn't exist names nothing");

            _world.Paused = true;
            Assert.IsTrue(_source.SimulationPaused, "Paused is the server electricity channel's");
        }

        [Test]
        public void Style_TracesACableOrDeviceCircuit_AConsumersRoom_AndAFloorsRoomOrCable()
        {
            SystemsViewStructure structure = Build();
            PowerSystemsViewStyle style = PowerSystemsViewStyle.Instance;
            style.AreaAt = tile => tile.y == 3 ? (tile.x < 5 ? Bridge : Storeroom) : (ushort)0;
            List<SystemsViewTraceSide> targets = new();

            style.TraceTargets(structure, new Vector2Int(1, 0), TileLayer.Wire, targets);
            CollectionAssert.AreEqual(new[] { new SystemsViewTraceSide(1) }, targets, "A cable traces its circuit");

            targets.Clear();
            style.TraceTargets(structure, new Vector2Int(4, 0), TileLayer.WallMountHigh, targets);
            CollectionAssert.AreEqual(new[] { new SystemsViewTraceSide(1, "Circuit"), new SystemsViewTraceSide(BridgeId, "Room") }, targets,
                "An APC traces its circuit and its room");

            targets.Clear();
            style.TraceTargets(structure, new Vector2Int(2, 3), TileLayer.FurnitureBase, targets);
            CollectionAssert.AreEqual(new[] { new SystemsViewTraceSide(BridgeId) }, targets, "A consumer traces its room");

            targets.Clear();
            style.TraceTargets(structure, new Vector2Int(6, 3), TileLayer.Turf, targets);
            CollectionAssert.AreEqual(new[] { new SystemsViewTraceSide(StoreroomId) }, targets, "A floor traces its room");

            targets.Clear();
            style.TraceTargets(structure, new Vector2Int(2, 0), TileLayer.Turf, targets);
            CollectionAssert.AreEqual(new[] { new SystemsViewTraceSide(1) }, targets, "A floor over a cable traces the cable");

            targets.Clear();
            style.TraceTargets(structure, new Vector2Int(5, 5), TileLayer.Turf, targets);
            Assert.IsEmpty(targets, "A floor in no room over no cable traces nothing");

            StringAssert.EndsWith("Room: Storeroom · no APC", style.Describe(structure, new Vector2Int(6, 3)));
        }

        [Test]
        public void RoomTint_ChangesOnlyRoomsWhoseColourChanged()
        {
            MapEditorSystemsViewClient client = new();
            try
            {
                client.Update(SystemsViewKind.Power);
                SystemsViewStructure first = Build();
                Assert.IsTrue(client.Receive(SystemsViewStructure.Diff(null, first)));
                CollectionAssert.AreEquivalent(new[] { Bridge, Storeroom }, client.LastTintChangedRooms, "The first structure tints every room");
                Assert.IsTrue(client.RoomTint(Storeroom).Hatched);
                Assert.AreEqual(SystemsViewPalette.ColorFor(1), client.RoomTint(Bridge).Color);

                // Tracing the Bridge's circuit dims only the room off it.
                int invalidations = client.RoomTintInvalidations;
                Assert.IsTrue(client.BeginTrace(new Vector2Int(1, 0), TileLayer.Wire));
                CollectionAssert.AreEqual(new[] { Storeroom }, client.LastTintChangedRooms);

                // A badge-only delta while tracing leaves every room's colour alone: nothing rebuilds.
                invalidations = client.RoomTintInvalidations;
                _starved.PowerStatus = PowerStatus.Powered;
                _world.StateVersion++;
                Assert.IsTrue(client.Receive(SystemsViewStructure.Diff(first, Build())));
                Assert.AreEqual(invalidations, client.RoomTintInvalidations, "No room's tint changed");
            }
            finally
            {
                // As SystemsViewLiveTests: Dispose uses play-mode Destroy, so clear the overlay's objects by hand.
                foreach (GameObject root in Resources.FindObjectsOfTypeAll<GameObject>()
                             .Where(go => go.name is "MapEditorSystemsOverlay" or "MapEditorSystemsRoomTint").ToArray())
                    Object.DestroyImmediate(root);
                foreach (Material material in Resources.FindObjectsOfTypeAll<Material>()
                             .Where(m => m.name is "MapEditorSystemsRuns" or "MapEditorSystemsBadges" or "MapEditorSystemsRoomTint").ToArray())
                    Object.DestroyImmediate(material);
            }
        }

        [Test]
        public void AWorldResumption_MovesTheTopology_SoTheStructureIsRebuilt()
        {
            GameObject go = new("ElectricityResumeTest");
            _instantiated.Add(go);
            ElectricitySubSystem electricity = go.AddComponent<ElectricitySubSystem>();
            int before = electricity.SystemsViewTopologyVersion;
            MethodInfo resumed = typeof(ElectricitySubSystem).GetMethod("HandleChannelResumed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(resumed, "ElectricitySubSystem handles its channel's Resumed");
            resumed.Invoke(electricity, null);
            Assert.AreNotEqual(before, electricity.SystemsViewTopologyVersion, "A resumption (a template load ending) moves the version");

            // The source rescans on that move, as a template load needs.
            Build();
            int version = _source.StructureVersion;
            _world.TopologyVersion = electricity.SystemsViewTopologyVersion + 100;
            Assert.AreNotEqual(version, _source.StructureVersion);
            _source.BuildStructure();
            Assert.IsTrue(_source.LastRescannedCables);
        }

        // Phase 4: the Storeroom has a machine and no APC; everything else on this map is sound (the lone run powers nothing).
        [Test]
        public void Structure_CarriesTheAnalyzersMarkers_AndAFixRemovesItsMarker()
        {
            SystemsViewStructure structure = Build();
            SystemsViewKey noApc = new(new Vector2Int(6, 3), SystemsViewProblems.MarkerSlot(PowerProblemAnalyzer.CodeRoomNoApc));
            Assert.AreEqual(1, structure.Markers.Count);
            Assert.AreEqual("no-apc:2", structure.Markers[noApc].IssueId);
            SystemsViewIssue issue = _source.Analyze().Single();
            Assert.AreEqual("no-apc:2", issue.Id);
            Assert.AreEqual("No APC in Storeroom (6, 3)", issue.Title);
            Assert.IsFalse(_source.LastAnalyzed, "Analyze after a build reads the cached result");

            // A consumer's status changing moves only its badge; the analysis isn't run again.
            _starved.PowerStatus = PowerStatus.Powered;
            _world.StateVersion++;
            SystemsViewStructure powered = Build();
            Assert.IsFalse(_source.LastAnalyzed);
            Assert.IsEmpty(SystemsViewStructure.Diff(structure, powered).MarkersRemoved);

            // The room gains an APC: its marker goes, from the view and from Map Check.
            _world.Rooms[1] = new PowerSystemsViewRoomInfo(Storeroom, "Storeroom", _apc);
            SystemsViewStructure fixedRoom = Build();
            Assert.IsTrue(_source.LastAnalyzed);
            CollectionAssert.AreEqual(new[] { noApc }, SystemsViewStructure.Diff(powered, fixedRoom).MarkersRemoved);
            Assert.IsEmpty(_source.Analyze());

            // The APC's cable cut away: it is on no cable now.
            for (int x = 0; x <= 4; x++)
                _context.Map.GetOrCreateTileLocation(TileLayer.Wire, new Vector3(x, 0, 0)).ClearAllPlacedObject();
            _world.Circuits.Clear();
            _world.TopologyVersion++;
            CollectionAssert.AreEquivalent(new[] { "apc-cable:4:0", "smes-circuit:2:0" }, _source.Analyze().Select(found => found.Id));
        }

        // Phase 4: "needs more than it gives" compares ratings, never the output or draw of the moment, so it doesn't flicker.
        [Test]
        public void DemandAboveSupply_ReadsRatings_NotTheMomentsOutputOrDraw()
        {
            _world.DeviceList.Remove(_generator);
            _world.DeviceList.Remove(_smes);
            // Switched off (or a solar panel at night): it produces nothing now, but counts at its 5 kW rating.
            RatedProducer off = new(Place(0, 0, TileLayer.FurnitureBase, Direction.North)) { Rated = 5f };
            RatedConsumer machine = new(Place(4, 3, TileLayer.FurnitureBase, Direction.North)) { Rated = 2.5f, Now = 0.1f };
            _world.DeviceList.Add(off);
            _world.Circuits[off] = _circuit;
            _world.DeviceList.Add(machine);
            _world.ApcOf[machine] = _apc;
            _world.RoomOf[machine] = Bridge;

            // The Bridge draws 0.4 + 1 + 0.5 + 2.5 at most: 4.4 kW against 5.
            Build();
            Assert.IsFalse(_source.Analyze().Any(issue => issue.Id.StartsWith("circuit-")), "A generator switched off still counts at its rating");

            // At most 4 kW for the machine (idle, it draws 0.1): 5.9 against 5.
            machine.Rated = 4f;
            _world.TopologyVersion++;
            SystemsViewIssue over = _source.Analyze().Single(issue => issue.Id.StartsWith("circuit-"));
            Assert.AreEqual("circuit-demand:4:0", over.Id);
            StringAssert.Contains("up to 5.9 kW against at most 5.0 kW", over.Body);

            // The machine goes into use and back: the reading moves, the check doesn't.
            machine.Now = 4f;
            _world.TopologyVersion++;
            Assert.AreEqual(over.Body, _source.Analyze().Single(issue => issue.Id.StartsWith("circuit-")).Body);
        }

        // Phase 4: right after a template load the circuit graph is a tick behind; the source reports nothing until it is current.
        [Test]
        public void Analysis_WaitsForTheCircuitGraph_ThenReportsOnItsRebuild()
        {
            _world.CircuitsCurrent = false;
            _world.Circuits.Clear();
            _world.TopologyVersion++;
            Assert.IsFalse(_source.AnalysisCurrent);
            Assert.IsEmpty(_source.Analyze(), "No bogus \"on no circuit\" rows from a graph that hasn't caught up");
            Assert.IsEmpty(Build().Markers);

            // The graph rebuilds (which moves the topology): the real problems now.
            foreach (IElectricDevice device in new IElectricDevice[] { _generator, _smes, _apc })
                _world.Circuits[device] = _circuit;
            _world.CircuitsCurrent = true;
            _world.TopologyVersion++;
            Assert.IsTrue(_source.AnalysisCurrent);
            CollectionAssert.AreEqual(new[] { "no-apc:2" }, _source.Analyze().Select(issue => issue.Id));
        }

        [Test]
        public void Ratings_OfTheRealProducersAndMachines_DontMoveWithTheirState()
        {
            GameObject go = new("RatedPowerTest");
            _instantiated.Add(go);
            FuelPowerGenerator generator = go.AddComponent<FuelPowerGenerator>();
            SetPrivateField(generator, "_powerProduction", 25f);
            Assert.AreEqual(0f, generator.PowerProduction, "Off, it produces nothing");
            Assert.AreEqual(25f, generator.RatedPowerKw, "Its rating is the prefab's output, read before it starts");

            SolarPanel panel = go.AddComponent<SolarPanel>();
            panel.PeakPowerKw = 8f;
            Assert.AreEqual(8f, panel.RatedPowerKw, "A panel's rating is its peak, sun or no sun");

            MachinePowerConsumer machine = go.AddComponent<MachinePowerConsumer>();
            SetPrivateField(machine, "_powerConsumptionIdle", 0.5f);
            SetPrivateField(machine, "_powerConsumptionInUse", 3f);
            machine.isIdle = true;
            Assert.AreEqual(3f, machine.RatedPowerKw);
            machine.isIdle = false;
            Assert.AreEqual(3f, machine.RatedPowerKw);
        }

        private void PlaceCable(int x, int y)
        {
            PlacedTileObject placed = Place(x, y, TileLayer.Wire, Direction.North);
            placed.gameObject.AddComponent<CablesAdjacencyConnector>();
            SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());
            _context.Map.GetOrCreateTileLocation(TileLayer.Wire, new Vector3(x, 0, y)).AddPlacedObject(placed, Direction.North);
        }

        private PlacedTileObject Place(int x, int y, TileLayer layer, Direction facing)
        {
            GameObject go = new($"{layer}_{x}_{y}");
            _instantiated.Add(go);
            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.layer = layer;
            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_worldOrigin", new Vector2Int(x, y));
            SetPrivateField(placed, "_mapId", 0);
            SetPrivateField(placed, "_dir", facing);
            go.transform.position = new Vector3(x, 0, y);
            go.transform.SetParent(_context.Map.transform);
            return placed;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }

            Assert.Fail($"Field {fieldName} not found on {target.GetType().Name}");
        }

        private sealed class World : IPowerSystemsViewWorld
        {
            public readonly List<IElectricDevice> DeviceList = new();
            public readonly Dictionary<IElectricDevice, object> Circuits = new();
            public readonly Dictionary<IPowerConsumer, IApcChannelSource> ApcOf = new();
            public readonly Dictionary<IElectricDevice, ushort> RoomOf = new();
            public readonly List<PowerSystemsViewRoomInfo> Rooms = new();
            public readonly Dictionary<IApcChannelSource, CircuitStats> Stats = new();

            public TileMap Map { get; set; }
            public int TopologyVersion { get; set; }
            public int StateVersion { get; set; }
            public bool Paused { get; set; }
            public bool CircuitsCurrent { get; set; } = true;
            public long TickCount { get; set; }
            public float TickSeconds { get; set; } = 0.2f;
            public IReadOnlyList<IElectricDevice> Devices => DeviceList;

            public object CircuitOf(IElectricDevice device) => Circuits.TryGetValue(device, out object circuit) ? circuit : null;

            public bool TryGetApc(IPowerConsumer consumer, out IApcChannelSource apc) => ApcOf.TryGetValue(consumer, out apc);

            public bool TryGetRoomOf(IElectricDevice device, out ushort areaId) => RoomOf.TryGetValue(device, out areaId);

            public void CollectRooms(List<PowerSystemsViewRoomInfo> into) => into.AddRange(Rooms);

            public bool TryGetApcStats(IApcChannelSource apc, out CircuitStats stats) => Stats.TryGetValue(apc, out stats);
        }

        private abstract class Device : IElectricDevice
        {
            protected Device(PlacedTileObject tile) => TileObject = tile;

            public PlacedTileObject TileObject { get; }
        }

        private sealed class Producer : Device, IPowerProducer
        {
            public Producer(PlacedTileObject tile) : base(tile) { }

            public float Output;
            public float PowerProduction => Output;
        }

        private class Storage : Device, IPowerStorage
        {
            public Storage(PlacedTileObject tile) : base(tile) { }

            public float Stored;
            public float Capacity;
            public float StoredEnergyKwh => Stored;
            public float MaxCapacityKwh => Capacity;
            public float RemainingCapacityKwh => Capacity - Stored;
            public float MaxDischargeRateKw => 50f;
            public float MaxChargeRateKw => 50f;
            public bool IsOn => true;
            public float MaxDeliverableKw(float tickSeconds) => 50f;
            public float RemovePowerKw(float requestedKw, float tickSeconds) => 0f;
            public float AddPowerKw(float requestedKw, float tickSeconds) => 0f;
        }

        private sealed class Apc : Storage, IApcChannelSource
        {
            public Apc(PlacedTileObject tile) : base(tile) { }

            public ApcControlFlags Channels { get; set; }
        }

        private sealed class RatedProducer : Device, IRatedPowerProducer
        {
            public RatedProducer(PlacedTileObject tile) : base(tile) { }

            public float Rated;
            public float PowerProduction => 0f;
            public float RatedPowerKw => Rated;
        }

        private sealed class RatedConsumer : Device, IRatedPowerConsumer
        {
            public RatedConsumer(PlacedTileObject tile) : base(tile) { }

            public float Rated;
            public float Now;
            public float PowerNeeded => Now;
            public float RatedPowerKw => Rated;
            public PowerStatus PowerStatus { get; set; }
            public PowerChannel Channel => PowerChannel.Equipment;
        }

        private sealed class Consumer : Device, IPowerConsumer
        {
            public Consumer(PlacedTileObject tile, PowerChannel channel, float kw) : base(tile)
            {
                Channel = channel;
                PowerNeeded = kw;
            }

            public float PowerNeeded { get; }
            public PowerStatus PowerStatus { get; set; }
            public PowerChannel Channel { get; }
        }
    }
}
