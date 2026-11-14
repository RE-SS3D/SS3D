using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Atmospherics.SystemsView;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.MapEditor.SystemsView;
using UnityEngine;
using static SS3D.Systems.Atmospherics.SystemsView.AtmosSystemsViewCodes;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    /// <summary>
    /// The Atmos view's source (Documents/architecture/2026-11_map-editor-systems-views.md, Phase 1): runs per line with
    /// their real links, a manifold on every line it spans, badges with both networks of a pump, and colours that stay
    /// put through a pipe edit with the real tracker and registry.
    /// </summary>
    public class AtmosSystemsViewSourceTests
    {
        private List<GameObject> _instantiated;
        private TileMapTestUtilities.MapContext _context;
        private GasPipeNetworkRegistry _registry;
        private AtmosPortRegistry _ports;
        private AtmosSystemsViewSource _source;
        private VentController _vent;
        private AtmosPumpController _pump;

        // Row y = 5, west to east: Supply pipes at 2 and 3 (a vent on 2), a manifold at 4 joining Supply to Surface,
        // a Surface pipe at 5, a pump at 6 facing east, Surface pipes at 7 and 8.
        [SetUp]
        public void SetUp()
        {
            _instantiated = new List<GameObject>();
            _context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacePipe(2, 5, TileLayer.PipeLeft);
            PlacePipe(3, 5, TileLayer.PipeLeft);
            PlacedTileObject manifold = Place(4, 5, TileLayer.PipeManifold, Direction.East);
            manifold.gameObject.AddComponent<GasPipeManifold>().SetLines(new[] { TileLayer.PipeLeft, TileLayer.PipeSurface });
            PlacePipe(5, 5, TileLayer.PipeSurface);
            PlacePipe(7, 5, TileLayer.PipeSurface);
            PlacePipe(8, 5, TileLayer.PipeSurface);
            _pump = Place(6, 5, TileLayer.PipeSurface, Direction.East).gameObject.AddComponent<AtmosPumpController>();
            _vent = Place(2, 5, TileLayer.FurnitureBase, Direction.North).gameObject.AddComponent<VentController>();

            _registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            _registry.RebuildAll(_context.Map);
            _ports = new AtmosPortRegistry();
            _ports.Register(_vent);
            _ports.Register(_pump);
            _source = new AtmosSystemsViewSource(() => _context.Map, () => _registry, _ports);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _instantiated)
                Object.DestroyImmediate(go);
            _instantiated.Clear();
        }

        private static SystemsViewKey Run(int x, byte slot) => new(new Vector2Int(x, 5), slot);

        // As the publisher does: read the version (which polls), then build.
        private SystemsViewStructure Build()
        {
            _ = _source.StructureVersion;
            return _source.BuildStructure();
        }

        [Test]
        public void Structure_HasRunsPerLine_AManifoldOnEachLine_AndBothNetworksOfAPump()
        {
            SystemsViewStructure structure = Build();

            Assert.AreEqual(SystemsViewKind.Atmos, structure.View);
            Assert.AreEqual(2, _registry.NetworkCount);
            // West network first (lower first tile): Supply 1, mostly Supply pipe. East network: Surface 2.
            Assert.AreEqual("Supply 1", structure.Networks[1].Label);
            Assert.AreEqual("Surface 2", structure.Networks[2].Label);

            SystemsViewSegment west = structure.Segments[Run(2, Supply)];
            Assert.AreEqual(1, west.ViewId);
            Assert.AreEqual(SystemsViewLinks.East, west.Links, "The run's end joins only its neighbour");
            Assert.AreEqual(SystemsViewLinks.East | SystemsViewLinks.West, structure.Segments[Run(3, Supply)].Links,
                "A pipe beside a manifold port draws into it");

            foreach (byte line in new[] { Supply, Surface })
            {
                SystemsViewSegment manifold = structure.Segments[Run(4, (byte)(ManifoldSlotBase + line))];
                Assert.AreEqual(1, manifold.ViewId);
                Assert.AreEqual(SystemsViewLinks.East | SystemsViewLinks.West, manifold.Links, "A manifold joins at its front and back");
            }

            Assert.IsFalse(structure.Segments.ContainsKey(Run(4, Supply)), "A manifold's runs don't take a plain pipe's slot");
            Assert.AreEqual(SystemsViewLinks.East | SystemsViewLinks.West, structure.Segments[Run(5, Surface)].Links,
                "A pipe draws into an inline device's port");
            Assert.AreEqual(2, structure.Segments[Run(8, Surface)].ViewId);
            Assert.AreEqual(7, structure.Segments.Count);

            SystemsViewBadge pump = structure.Badges[new SystemsViewKey(new Vector2Int(6, 5), (byte)TileLayer.PipeSurface)];
            Assert.AreEqual(KindPump, pump.Kind);
            Assert.AreEqual(On, SwitchOf(pump.State));
            Assert.AreEqual(ModePressure, ModeOf(pump.State));
            Assert.AreEqual(Direction.East, pump.Facing);
            Assert.AreEqual(1, pump.ViewId, "The back end is the network gas comes from");
            Assert.AreEqual(2, pump.OtherViewId, "The front end is the network it goes to");

            SystemsViewBadge vent = structure.Badges[new SystemsViewKey(new Vector2Int(2, 5), (byte)TileLayer.FurnitureBase)];
            Assert.AreEqual(KindVent, vent.Kind);
            Assert.AreEqual(1, vent.ViewId);
            Assert.AreEqual(0, vent.OtherViewId);

            StringAssert.Contains("Pump: on, to a pressure · Supply 1 → Surface 2",
                AtmosSystemsViewStyle.Instance.Describe(structure, new Vector2Int(6, 5)));
            Assert.AreEqual("Vent: on · Supply 1\nSupply 1",
                AtmosSystemsViewStyle.Instance.Describe(structure, new Vector2Int(2, 5)));
            StringAssert.Contains("Surface line, part of Supply 1 (manifold)",
                AtmosSystemsViewStyle.Instance.Describe(structure, new Vector2Int(4, 5)));
        }

        [Test]
        public void ADeviceStateChange_MovesTheVersion_AndChangesOnlyItsBadge()
        {
            SystemsViewStructure before = Build();
            int version = _source.StructureVersion;
            Assert.AreEqual(version, _source.StructureVersion, "Nothing changed: the version holds");

            // A pump switched to volume regulation. (Switching a device off goes through FishNet's server checks,
            // which need a running server; the visual pilot covers it live.)
            SetPrivateField(_pump, "_pumpRegulation", PumpRegulation.Volume);
            Assert.AreNotEqual(version, _source.StructureVersion, "A device state change moves the version");

            SystemsViewDelta delta = SystemsViewStructure.Diff(before, Build());
            Assert.IsEmpty(delta.SegmentsSet);
            Assert.IsEmpty(delta.NetworksSet);
            Assert.AreEqual(1, delta.BadgesSet.Length);
            Assert.AreEqual(KindPump, delta.BadgesSet[0].Kind);
            Assert.AreEqual(ModeVolume, ModeOf(delta.BadgesSet[0].State));
            Assert.AreEqual(0, _source.LastWalkedNetworks, "A state change walks no pipes");
        }

        [Test]
        public void APipeEdit_KeepsEachNetworksColour_AndWalksOnlyTheChangedNetwork()
        {
            Build();
            Assert.AreEqual(2, _source.LastWalkedNetworks);
            int version = _source.StructureVersion;

            // Extend the east network: it keeps its id, the west network is not walked again.
            PlacePipe(9, 5, TileLayer.PipeSurface);
            _registry.RebuildAround(_context.Map, new TileCoord(_context.Map.MapId, 9, 5));
            Assert.AreNotEqual(version, _source.StructureVersion, "A topology rebuild moves the version");
            SystemsViewStructure extended = Build();
            Assert.AreEqual(1, _source.LastWalkedNetworks);
            Assert.AreEqual(2, extended.Segments[Run(9, Surface)].ViewId);
            Assert.AreEqual(1, extended.Segments[Run(2, Supply)].ViewId);

            // Cut the east run's first pipe: the larger rest (8 and 9) keeps the colour, the pump's front reaches nothing.
            _context.Map.GetOrCreateTileLocation(TileLayer.PipeSurface, new Vector3(7, 0, 5)).ClearAllPlacedObject();
            _registry.RebuildAround(_context.Map, new TileCoord(_context.Map.MapId, 7, 5));
            SystemsViewStructure split = Build();
            Assert.IsFalse(split.Segments.ContainsKey(Run(7, Surface)));
            Assert.AreEqual(2, split.Segments[Run(8, Surface)].ViewId);
            Assert.AreEqual(2, split.Segments[Run(9, Surface)].ViewId);
            Assert.AreEqual(1, split.Segments[Run(5, Surface)].ViewId);
            SystemsViewBadge pump = split.Badges[new SystemsViewKey(new Vector2Int(6, 5), (byte)TileLayer.PipeSurface)];
            Assert.AreEqual(1, pump.ViewId);
            Assert.AreEqual(0, pump.OtherViewId, "Read from the current registry, not the pump's cached ports");
        }

        [Test]
        public void ADeviceAtAPipeEnd_ReLinksOnlyThatPipe_AndRegistrationOrderDoesNotMatter()
        {
            Build();
            int version = _source.StructureVersion;

            // The same devices registered in another order: nothing changed.
            _ports.Unregister(_vent);
            _ports.Register(_vent);
            Assert.AreEqual(version, _source.StructureVersion, "Re-registering a device is no change");

            // A pump at the east run's open end: that pipe now draws into its port, with no topology rebuild.
            Assert.AreEqual(SystemsViewLinks.West, Build().Segments[Run(8, Surface)].Links);
            PlacedTileObject placed = Place(9, 5, TileLayer.PipeSurface, Direction.East);
            AtmosPumpController pump = placed.gameObject.AddComponent<AtmosPumpController>();
            _ports.Register(pump);
            Assert.AreNotEqual(version, _source.StructureVersion);
            SystemsViewStructure added = _source.BuildStructure();
            Assert.AreEqual(0, _source.LastWalkedNetworks, "No network is walked again");
            Assert.AreEqual(1, _source.LastRelinkedRuns, "Only the pipe beside the pump is re-read");
            Assert.AreEqual(SystemsViewLinks.West | SystemsViewLinks.East, added.Segments[Run(8, Surface)].Links);
            Assert.AreEqual(2, added.Badges[new SystemsViewKey(new Vector2Int(9, 5), (byte)TileLayer.PipeSurface)].ViewId);

            // Taken away again: the badge goes and the pipe is an open end once more.
            _ports.Unregister(pump);
            _context.Map.GetOrCreateTileLocation(TileLayer.PipeSurface, new Vector3(9, 0, 5)).ClearAllPlacedObject();
            SystemsViewStructure removed = Build();
            Assert.AreEqual(0, _source.LastWalkedNetworks);
            Assert.AreEqual(SystemsViewLinks.West, removed.Segments[Run(8, Surface)].Links);
            Assert.IsFalse(removed.Badges.ContainsKey(new SystemsViewKey(new Vector2Int(9, 5), (byte)TileLayer.PipeSurface)));
        }

        private PlacedTileObject PlacePipe(int x, int y, TileLayer layer)
        {
            PlacedTileObject placed = Place(x, y, layer, Direction.North, pipe: true);
            return placed;
        }

        private PlacedTileObject Place(int x, int y, TileLayer layer, Direction facing, bool pipe = false)
        {
            GameObject go = new($"{layer}_{x}_{y}");
            _instantiated.Add(go);
            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.layer = layer;
            if (pipe)
            {
                go.AddComponent<PipeAdjacencyConnector>();
                so.genericType = TileObjectGenericType.Pipe;
                so.specificType = TileObjectSpecificType.None;
                SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());
            }

            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_worldOrigin", new Vector2Int(x, y));
            SetPrivateField(placed, "_mapId", 0);
            SetPrivateField(placed, "_dir", facing);
            go.transform.position = new Vector3(x, 0, y);
            ITileLocation location = _context.Map.GetOrCreateTileLocation(layer, new Vector3(x, 0, y));
            location.AddPlacedObject(placed, Direction.North);
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
    }
}
