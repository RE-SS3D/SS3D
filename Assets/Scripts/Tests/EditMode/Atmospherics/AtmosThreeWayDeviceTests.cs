using NUnit.Framework;
using SS3D.Systems.Atmospherics;
using SS3D.Systems.Atmospherics.Pipes;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Atmospherics
{
    public sealed class AtmosThreeWayDeviceTests
    {
        private const float Tick = 1f;
        private static readonly TileLayer[] Plate = AtmosDevicePipeResolver.PlateLayers;

        private List<GameObject> _instantiated;

        [SetUp]
        public void SetUp() => _instantiated = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _instantiated)
                Object.DestroyImmediate(go);
            _instantiated.Clear();
        }

        [TestCase(Direction.North, true, Direction.West)]
        [TestCase(Direction.North, false, Direction.East)]
        [TestCase(Direction.East, true, Direction.North)]
        [TestCase(Direction.South, false, Direction.West)]
        public void SidePort_IsLeftOrRightOfTheFacing(Direction facing, bool sideOnLeft, Direction expected)
        {
            Assert.AreEqual(expected, AtmosDevicePipeResolver.GetSidePort(facing, sideOnLeft));
            Assert.IsTrue(AtmosDevicePipeResolver.HasThreeWayPortToward(facing, sideOnLeft, expected));
            Assert.IsFalse(AtmosDevicePipeResolver.HasThreeWayPortToward(facing, sideOnLeft, TileHelper.GetOpposite(expected)));
        }

        [Test]
        public void Plate_TakesTheLineWithPipesAtItsPortsAndAGapUnderIt()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacePorts(context, TileLayer.PipeLeft);

            // Waste passes under the plate, so it's not the plate's line even with pipes alongside.
            PlacePipe(context, 5, 4, TileLayer.PipeRight);
            PlacePipe(context, 5, 5, TileLayer.PipeRight);
            PlacePipe(context, 5, 6, TileLayer.PipeRight);

            Assert.IsTrue(ResolveLine(context, out TileLayer line));
            Assert.AreEqual(TileLayer.PipeLeft, line);
        }

        [Test]
        public void Plate_WithTwoLinesThatFit_StaysDisconnected()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacePorts(context, TileLayer.PipeLeft);
            PlacePorts(context, TileLayer.PipeRight);

            Assert.IsFalse(ResolveLine(context, out _));
        }

        [Test]
        public void Plate_MissingOnePort_StaysDisconnected()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacePipe(context, 5, 4, TileLayer.PipeLeft);
            PlacePipe(context, 5, 6, TileLayer.PipeLeft);

            Assert.IsFalse(ResolveLine(context, out _));
        }

        [Test]
        public void Plate_ResolvesEachPortToItsOwnNetwork()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacePorts(context, TileLayer.PipeMiddle);
            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);

            Assert.IsTrue(AtmosDevicePipeResolver.TryResolveThreeWayNetworks(
                context.Map, registry, new TileCoord(context.Map.MapId, 5, 5), Direction.North, true, Plate,
                out GasPipeNetworkId back, out GasPipeNetworkId front, out GasPipeNetworkId side));
            Assert.AreEqual(3, registry.NetworkCount);
            Assert.AreNotEqual(back.Value, front.Value);
            Assert.AreNotEqual(back.Value, side.Value);
            Assert.AreNotEqual(front.Value, side.Value);
        }

        [Test]
        public void UnderfloorPipe_DrawsIntoAPlatePort_OnlyOnItsLine()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacePlate(context, Direction.North, Plate);
            PlacedTileObject besidePort = PlacePipe(context, 5, 6, TileLayer.PipeLeft);
            PlacedTileObject besideBlank = PlacePipe(context, 6, 5, TileLayer.PipeLeft);

            var rule = (ICrossLayerConnectionRule)((IEngineDrivenAdjacency)besidePort.Connector).ConnectionRule;
            Assert.IsTrue(rule.IsConnectedAcrossLayers(besidePort, Direction.South, context.Map));
            Assert.IsFalse(rule.IsConnectedAcrossLayers(besideBlank, Direction.West, context.Map));
        }

        [Test]
        public void Filter_SendsSelectedGasOutTheSide_AndTheRestStraightOn()
        {
            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            GasPipeNetworkRecord inlet = Network(1, oxygen: 80f, plasma: 20f);
            GasPipeNetworkRecord outlet = Network(2);
            GasPipeNetworkRecord side = Network(3);
            AtmosFilterController filter = CreateFilter(selected: 1UL << AtmosConstants.Plasma.Value);

            float moved = filter.Filter(registry, inlet, outlet, side, Tick);

            float share = AtmosPortConstants.FilterDefaultLitresPerSecond / 1000f;
            Assert.That(moved, Is.EqualTo(100f * share).Within(0.001f));
            Assert.That(outlet.Moles[AtmosConstants.Oxygen.Value], Is.EqualTo(80f * share).Within(0.001f));
            Assert.AreEqual(0f, outlet.Moles[AtmosConstants.Plasma.Value]);
            Assert.That(side.Moles[AtmosConstants.Plasma.Value], Is.EqualTo(20f * share).Within(0.001f));
            Assert.AreEqual(0f, side.Moles[AtmosConstants.Oxygen.Value]);
            Assert.That(Total(inlet) + Total(outlet) + Total(side), Is.EqualTo(100f).Within(0.001f));
        }

        [Test]
        public void Filter_CarriesItsInletsHeat()
        {
            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            GasPipeNetworkRecord inlet = Network(1, oxygen: 100f);
            inlet.Temperature = 500f;
            GasPipeNetworkRecord outlet = Network(2);
            AtmosFilterController filter = CreateFilter(selected: 0UL);

            filter.Filter(registry, inlet, outlet, Network(3), Tick);

            Assert.That(outlet.Temperature, Is.EqualTo(500f).Within(0.01f));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        public void Filter_StallsWhenEitherOutletIsFull(bool outletFull, bool sideFull)
        {
            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            GasPipeNetworkRecord inlet = Network(1, oxygen: 100f);
            GasPipeNetworkRecord outlet = Network(2, oxygen: outletFull ? FullMoles() : 0f);
            GasPipeNetworkRecord side = Network(3, oxygen: sideFull ? FullMoles() : 0f);
            AtmosFilterController filter = CreateFilter(selected: 0UL);

            Assert.AreEqual(0f, filter.Filter(registry, inlet, outlet, side, Tick));
            Assert.IsTrue(filter.LastStalled);
            Assert.AreEqual(100f, Total(inlet));
        }

        [Test]
        public void Mixer_HoldsItsRatio_AndStopsAtItsTarget()
        {
            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            GasPipeNetworkRecord inlet1 = Network(1, oxygen: 5000f);
            GasPipeNetworkRecord inlet2 = Network(2, nitrogen: 5000f);
            GasPipeNetworkRecord outlet = Network(3);
            AtmosMixerController mixer = CreateMixer(inlet1Percent: 20, targetKpa: 4500);

            mixer.Mix(registry, inlet1, outlet, inlet2, Tick);

            float oxygen = outlet.Moles[AtmosConstants.Oxygen.Value];
            float nitrogen = outlet.Moles[AtmosConstants.Nitrogen.Value];
            Assert.That(oxygen / (oxygen + nitrogen), Is.EqualTo(0.2f).Within(0.001f));

            for (int i = 0; i < 40; i++)
                mixer.Mix(registry, inlet1, outlet, inlet2, Tick);

            Assert.That(outlet.GetPressure(AtmosConstants.DefaultGasCount), Is.EqualTo(4500f).Within(1f));
            Assert.AreEqual(0f, mixer.Mix(registry, inlet1, outlet, inlet2, Tick));
        }

        [Test]
        public void Mixer_StopsWhenAnInletRunsDry()
        {
            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            GasPipeNetworkRecord inlet1 = Network(1, oxygen: 100f);
            GasPipeNetworkRecord outlet = Network(3);
            AtmosMixerController mixer = CreateMixer(inlet1Percent: 50, targetKpa: 4500);

            Assert.AreEqual(0f, mixer.Mix(registry, inlet1, outlet, Network(2), Tick));
            Assert.IsTrue(mixer.LastInlet2Dry);
            Assert.IsFalse(mixer.LastInlet1Dry);
            Assert.AreEqual(100f, Total(inlet1));
        }

        [Test]
        public void Mixer_AtFullRatio_IgnoresTheOtherInlet()
        {
            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            GasPipeNetworkRecord outlet = Network(3);
            AtmosMixerController mixer = CreateMixer(inlet1Percent: 100, targetKpa: 4500);

            Assert.Greater(mixer.Mix(registry, Network(1, oxygen: 100f), outlet, Network(2), Tick), 0f);
            Assert.IsFalse(mixer.LastStalled);
        }

        [Test]
        public void Mixer_TakesNoMoreThanItsPoorerInletAllows()
        {
            float moles = AtmosMixerController.ComputeMoles(10f, 1000f, 0.5f, 1000f, Tick);

            Assert.That(moles, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void Meter_KeepsOneSampleASecond_OldestFirst()
        {
            var go = new GameObject("Meter");
            _instantiated.Add(go);
            go.AddComponent<PlacedTileObject>();
            AtmosMeterController meter = go.AddComponent<AtmosMeterController>();

            meter.Sample(10f, 0.5f);
            meter.Sample(20f, 0.5f);
            meter.Sample(30f, 0.5f);
            meter.Sample(40f, 0.5f);

            var history = new float[AtmosMeterController.HistoryLength];
            meter.CopyHistory(history);
            Assert.AreEqual(30f, history[AtmosMeterController.HistoryLength - 1]);
            Assert.AreEqual(10f, history[AtmosMeterController.HistoryLength - 2]);
            Assert.AreEqual(0f, history[0]);
        }

        private static float Total(GasPipeNetworkRecord record) => record.GetTotalMoles(AtmosConstants.DefaultGasCount);

        private static float FullMoles() =>
            AtmosPipeThermo.GetMolesForPressure(AtmosPortConstants.FilterOutletLimitKpa, AtmosConstants.StandardTemperature, 1f);

        private static GasPipeNetworkRecord Network(ushort id, float oxygen = 0f, float nitrogen = 0f, float plasma = 0f)
        {
            var record = new GasPipeNetworkRecord(new GasPipeNetworkId(id), AtmosConstants.DefaultGasCount) { Volume = 1f };
            record.Moles[AtmosConstants.Oxygen.Value] = oxygen;
            record.Moles[AtmosConstants.Nitrogen.Value] = nitrogen;
            record.Moles[AtmosConstants.Plasma.Value] = plasma;
            return record;
        }

        private AtmosFilterController CreateFilter(ulong selected)
        {
            var go = new GameObject("Filter");
            _instantiated.Add(go);
            go.AddComponent<PlacedTileObject>();
            AtmosFilterController filter = go.AddComponent<AtmosFilterController>();
            SetPrivateField(filter, "_selectedGases", selected);
            return filter;
        }

        private AtmosMixerController CreateMixer(int inlet1Percent, int targetKpa)
        {
            var go = new GameObject("Mixer");
            _instantiated.Add(go);
            go.AddComponent<PlacedTileObject>();
            AtmosMixerController mixer = go.AddComponent<AtmosMixerController>();
            SetPrivateField(mixer, "_inlet1Percent", inlet1Percent);
            SetPrivateField(mixer, "_targetOutletPressureKpa", targetKpa);
            return mixer;
        }

        private static bool ResolveLine(TileMapTestUtilities.MapContext context, out TileLayer line) =>
            AtmosDevicePipeResolver.TryResolveThreeWayLine(
                context.Map, new TileCoord(context.Map.MapId, 5, 5), Direction.North, true, Plate, out line);

        // Back, front and left side of a north-facing device at (5, 5).
        private void PlacePorts(TileMapTestUtilities.MapContext context, TileLayer line)
        {
            PlacePipe(context, 5, 4, line);
            PlacePipe(context, 5, 6, line);
            PlacePipe(context, 4, 5, line);
        }

        private void PlacePlate(TileMapTestUtilities.MapContext context, Direction facing, TileLayer[] lines)
        {
            var go = new GameObject("Plate");
            _instantiated.Add(go);
            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.layer = TileLayer.PipeSurface;
            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_worldOrigin", new Vector2Int(5, 5));
            SetPrivateField(placed, "_mapId", 0);
            SetPrivateField(placed, "_dir", facing);
            go.transform.position = new Vector3(5, 0, 5);
            go.AddComponent<PlateStub>().Lines = lines;
            RegisterOnMap(context.Map, placed, TileLayer.PipeSurface, new Vector3(5, 0, 5));
        }

        private PlacedTileObject PlacePipe(TileMapTestUtilities.MapContext context, int x, int y, TileLayer layer)
        {
            var go = new GameObject($"Pipe_{x}_{y}_{layer}");
            _instantiated.Add(go);
            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            go.AddComponent<PipeAdjacencyConnector>();
            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = TileObjectGenericType.Pipe;
            so.specificType = TileObjectSpecificType.None;
            so.layer = layer;
            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());
            SetPrivateField(placed, "_worldOrigin", new Vector2Int(x, y));
            SetPrivateField(placed, "_mapId", 0);
            go.transform.position = new Vector3(x, 0, y);
            RegisterOnMap(context.Map, placed, layer, new Vector3(x, 0, y));
            return placed;
        }

        private static void RegisterOnMap(TileMap map, PlacedTileObject placed, TileLayer layer, Vector3 position)
        {
            ITileLocation location = map.GetOrCreateTileLocation(layer, position);
            location.AddPlacedObject(placed, Direction.North);
            placed.transform.SetParent(map.transform);
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

            Assert.Fail($"No field {fieldName} on {target.GetType().Name}");
        }

        private sealed class PlateStub : MonoBehaviour, IPipePortDevice
        {
            public TileLayer[] Lines;

            public TileLayer[] PortLines => Lines;

            public bool HasPortToward(Direction direction) =>
                AtmosDevicePipeResolver.HasThreeWayPortToward(Direction.North, true, direction);
        }
    }
}
