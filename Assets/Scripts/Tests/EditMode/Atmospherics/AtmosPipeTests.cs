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
    public class AtmosPipeTests
    {
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

        [Test]
        public void StraightPipeRun_FormsSingleNetwork()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacedTileObject western = CreateGasPipeAt(new Vector2Int(5, 5), TileLayer.PipeMiddle);
            PlacedTileObject eastern = CreateGasPipeAt(new Vector2Int(6, 5), TileLayer.PipeMiddle);

            RegisterOnMap(context.Map, western, TileLayer.PipeMiddle, new Vector3(5, 0, 5));
            RegisterOnMap(context.Map, eastern, TileLayer.PipeMiddle, new Vector3(6, 0, 5));
            RecomputeAdjacency(context.Map, western, eastern);

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);

            Assert.AreEqual(1, registry.NetworkCount);
            Assert.IsTrue(registry.TryGetNetworkForSegment(
                new GasPipeSegmentKey(new TileCoord(context.Map.MapId, 5, 5), TileLayer.PipeMiddle, TileObjectSpecificType.None),
                out GasPipeNetworkId westernNetwork,
                out GasPipeNetworkRecord record));
            Assert.IsTrue(registry.TryGetNetworkForSegment(
                new GasPipeSegmentKey(new TileCoord(context.Map.MapId, 6, 5), TileLayer.PipeMiddle, TileObjectSpecificType.None),
                out GasPipeNetworkId easternNetwork,
                out _));
            Assert.AreEqual(westernNetwork.Value, easternNetwork.Value);
            Assert.AreEqual(2, record.Segments.Count);
            Assert.AreEqual(2 * AtmosPipeConstants.SegmentVolume, record.Volume, 0.001f);
        }

        [Test]
        public void OffsetPipeLayers_OnSameTile_DoNotShareNetwork()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacedTileObject leftLayer = CreateGasPipeAt(new Vector2Int(5, 5), TileLayer.PipeLeft);
            PlacedTileObject middleLayer = CreateGasPipeAt(new Vector2Int(5, 5), TileLayer.PipeMiddle);

            RegisterOnMap(context.Map, leftLayer, TileLayer.PipeLeft, new Vector3(5, 0, 5));
            RegisterOnMap(context.Map, middleLayer, TileLayer.PipeMiddle, new Vector3(5, 0, 5));

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);

            Assert.AreEqual(2, registry.NetworkCount);
        }

        [Test]
        public void RemovingMiddleSegment_SplitsNetwork()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacedTileObject western = CreateGasPipeAt(new Vector2Int(5, 5), TileLayer.PipeMiddle);
            PlacedTileObject center = CreateGasPipeAt(new Vector2Int(6, 5), TileLayer.PipeMiddle);
            PlacedTileObject eastern = CreateGasPipeAt(new Vector2Int(7, 5), TileLayer.PipeMiddle);

            RegisterOnMap(context.Map, western, TileLayer.PipeMiddle, new Vector3(5, 0, 5));
            RegisterOnMap(context.Map, center, TileLayer.PipeMiddle, new Vector3(6, 0, 5));
            RegisterOnMap(context.Map, eastern, TileLayer.PipeMiddle, new Vector3(7, 0, 5));
            RecomputeAdjacency(context.Map, western, center, eastern);

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);
            Assert.AreEqual(1, registry.NetworkCount);

            context.Map.GetOrCreateTileLocation(TileLayer.PipeMiddle, new Vector3(6, 0, 5)).ClearAllPlacedObject();
            registry.RebuildAround(context.Map, new TileCoord(context.Map.MapId, 6, 5));

            Assert.AreEqual(2, registry.NetworkCount);
        }

        [Test]
        public void TransferTurfToNetwork_ConservesTotalMoles()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(5, 0, 5));

            PlacedTileObject pipe = CreateGasPipeAt(new Vector2Int(5, 5), TileLayer.PipeMiddle);
            RegisterOnMap(context.Map, pipe, TileLayer.PipeMiddle, new Vector3(5, 0, 5));

            using var turf = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            turf.CreateChunk(new TileChunkRef
            {
                MapId = context.Map.MapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });
            turf.UpdateCell(new TileCoord(context.Map.MapId, 5, 5));

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);
            Assert.IsTrue(registry.TryGetNetworkForSegment(
                GasPipeSegmentKey.From(pipe),
                out GasPipeNetworkId networkId,
                out GasPipeNetworkRecord network));

            var observer = new AtmosPipeObserver(context.Map, registry);
            var pipeSimulation = new AtmosPipeSimulation(registry, turf, null, AtmosConstants.DefaultGasCount, observer);

            float initialTurfMoles = turf.GetTotalMoles();
            turf.DebugAddMoles(new TileCoord(context.Map.MapId, 5, 5), AtmosConstants.Oxygen, 25f);
            float afterAdd = turf.GetTotalMoles();

            Assert.IsTrue(pipeSimulation.TryTransferMoles(
                networkId,
                AtmosConstants.Oxygen,
                10f,
                new TileCoord(context.Map.MapId, 5, 5),
                PipeTransferDirection.ToNetwork,
                out float moved));
            Assert.AreEqual(10f, moved, 0.001f);
            Assert.AreEqual(afterAdd, turf.GetTotalMoles() + pipeSimulation.GetTotalMolesAcrossNetworks(), 0.01f);
            Assert.AreEqual(10f, network.Moles[AtmosConstants.Oxygen.Value], 0.001f);
            Assert.Less(turf.DebugGetMoles(new TileCoord(context.Map.MapId, 5, 5), AtmosConstants.Oxygen), afterAdd);
            Assert.Greater(initialTurfMoles, 0f);
        }

        [Test]
        public void TransferNetworkToTurf_ConservesTotalMoles()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(5, 0, 5));

            PlacedTileObject pipe = CreateGasPipeAt(new Vector2Int(5, 5), TileLayer.PipeMiddle);
            RegisterOnMap(context.Map, pipe, TileLayer.PipeMiddle, new Vector3(5, 0, 5));

            using var turf = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            turf.CreateChunk(new TileChunkRef
            {
                MapId = context.Map.MapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });
            turf.UpdateCell(new TileCoord(context.Map.MapId, 5, 5));

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);
            Assert.IsTrue(registry.TryGetNetworkForSegment(
                GasPipeSegmentKey.From(pipe),
                out GasPipeNetworkId networkId,
                out GasPipeNetworkRecord network));

            network.Moles[AtmosConstants.CarbonDioxide.Value] = 15f;

            var observer = new AtmosPipeObserver(context.Map, registry);
            var pipeSimulation = new AtmosPipeSimulation(registry, turf, null, AtmosConstants.DefaultGasCount, observer);
            float totalBefore = turf.GetTotalMoles() + pipeSimulation.GetTotalMolesAcrossNetworks();

            Assert.IsTrue(pipeSimulation.TryTransferMoles(
                networkId,
                AtmosConstants.CarbonDioxide,
                8f,
                new TileCoord(context.Map.MapId, 5, 5),
                PipeTransferDirection.ToTurf,
                out float moved));
            Assert.AreEqual(8f, moved, 0.001f);
            Assert.AreEqual(totalBefore, turf.GetTotalMoles() + pipeSimulation.GetTotalMolesAcrossNetworks(), 0.01f);
            Assert.AreEqual(7f, network.Moles[AtmosConstants.CarbonDioxide.Value], 0.001f);
            Assert.AreEqual(8f, turf.DebugGetMoles(new TileCoord(context.Map.MapId, 5, 5), AtmosConstants.CarbonDioxide), 0.001f);
        }

        [Test]
        public void Transfer_FromEmptySource_MovesZeroMoles()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            TileMapTestUtilities.PlacePlenum(context, new Vector3(5, 0, 5));

            PlacedTileObject pipe = CreateGasPipeAt(new Vector2Int(5, 5), TileLayer.PipeMiddle);
            RegisterOnMap(context.Map, pipe, TileLayer.PipeMiddle, new Vector3(5, 0, 5));

            using var turf = new AtmosSimulation(context.Query, context.Map.MapId, AtmosConstants.DefaultGasCount);
            turf.CreateChunk(new TileChunkRef
            {
                MapId = context.Map.MapId,
                ChunkKey = Vector2Int.zero,
                Origin = Vector3.zero,
            });
            turf.UpdateCell(new TileCoord(context.Map.MapId, 5, 5));

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);
            Assert.IsTrue(registry.TryGetNetworkForSegment(
                GasPipeSegmentKey.From(pipe),
                out GasPipeNetworkId networkId,
                out _));

            var observer = new AtmosPipeObserver(context.Map, registry);
            var pipeSimulation = new AtmosPipeSimulation(registry, turf, null, AtmosConstants.DefaultGasCount, observer);

            Assert.IsFalse(pipeSimulation.TryTransferMoles(
                networkId,
                AtmosConstants.Plasma,
                5f,
                new TileCoord(context.Map.MapId, 5, 5),
                PipeTransferDirection.ToTurf,
                out float moved));
            Assert.AreEqual(0f, moved);
        }

        [Test]
        public void AtmosPortFlow_ReturnsZeroWhenPressuresEqual()
        {
            float flow = AtmosPortFlow.ComputeFlowMoles(
                101f,
                101f,
                AtmosPortConstants.VentRatedFlowMolesPerSecond,
                AtmosPortConstants.PortMaxDifferentialKpa,
                AtmosConstants.TickInterval);

            Assert.AreEqual(0f, flow);
        }

        [Test]
        public void AtmosPortFlow_ScalesWithPressureDifferential()
        {
            float lowDelta = AtmosPortFlow.ComputeFlowMoles(
                110f,
                100f,
                AtmosPortConstants.VentRatedFlowMolesPerSecond,
                AtmosPortConstants.PortMaxDifferentialKpa,
                AtmosConstants.TickInterval);

            float highDelta = AtmosPortFlow.ComputeFlowMoles(
                200f,
                100f,
                AtmosPortConstants.VentRatedFlowMolesPerSecond,
                AtmosPortConstants.PortMaxDifferentialKpa,
                AtmosConstants.TickInterval);

            Assert.Greater(lowDelta, 0f);
            Assert.Greater(highDelta, lowDelta);
        }

        [Test]
        public void AtmosDevicePipeResolver_FindsNetworkUnderFurnitureTile()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacedTileObject pipe = CreateGasPipeAt(new Vector2Int(5, 5), TileLayer.PipeMiddle);
            RegisterOnMap(context.Map, pipe, TileLayer.PipeMiddle, new Vector3(5, 0, 5));

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);

            TileCoord deviceCoord = new TileCoord(context.Map.MapId, 5, 5);
            Assert.IsTrue(AtmosDevicePipeResolver.TryResolveNetwork(
                context.Map,
                registry,
                deviceCoord,
                out GasPipeNetworkId networkId,
                out GasPipeSegmentKey segmentKey));
            Assert.IsFalse(networkId.IsNone);
            Assert.AreEqual(TileLayer.PipeMiddle, segmentKey.Layer);
        }

        [Test]
        public void SimpleAdjacencyConnectorPipe_ParticipatesInGasNetwork()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacedTileObject pipe = CreateSimpleGasPipeAt(new Vector2Int(5, 5), TileLayer.PipeMiddle);
            RegisterOnMap(context.Map, pipe, TileLayer.PipeMiddle, new Vector3(5, 0, 5));

            Assert.IsTrue(PipeConnectionRule.ParticipatesInGasNetwork(pipe));

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);

            Assert.AreEqual(1, registry.NetworkCount);
        }

        [Test]
        public void AtmosDevicePipeResolver_FindsNetworkOnAdjacentTile()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacedTileObject pipe = CreateGasPipeAt(new Vector2Int(6, 5), TileLayer.PipeMiddle);
            RegisterOnMap(context.Map, pipe, TileLayer.PipeMiddle, new Vector3(6, 0, 5));

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);

            TileCoord deviceCoord = new TileCoord(context.Map.MapId, 5, 5);
            Assert.IsTrue(AtmosDevicePipeResolver.TryResolveNetwork(
                context.Map,
                registry,
                deviceCoord,
                out GasPipeNetworkId networkId,
                out GasPipeSegmentKey segmentKey));
            Assert.IsFalse(networkId.IsNone);
            Assert.AreEqual(new TileCoord(context.Map.MapId, 6, 5), segmentKey.Coord);
        }

        private PlacedTileObject CreateGasPipeAt(Vector2Int worldOrigin, TileLayer layer)
        {
            GameObject go = new GameObject($"GasPipe_{worldOrigin}_{layer}");
            _instantiated.Add(go);

            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            go.AddComponent<PipeAdjacencyConnector>();

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = TileObjectGenericType.Pipe;
            so.specificType = TileObjectSpecificType.None;
            so.layer = layer;

            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_mapId", 0);
            placed.transform.position = new Vector3(worldOrigin.x, 0, worldOrigin.y);
            return placed;
        }

        private PlacedTileObject CreateSimpleGasPipeAt(Vector2Int worldOrigin, TileLayer layer)
        {
            GameObject go = new GameObject($"SimpleGasPipe_{worldOrigin}_{layer}");
            _instantiated.Add(go);

            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            go.AddComponent<SimpleAdjacencyConnector>();

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = TileObjectGenericType.Pipe;
            so.specificType = TileObjectSpecificType.None;
            so.layer = layer;

            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_mapId", 0);
            placed.transform.position = new Vector3(worldOrigin.x, 0, worldOrigin.y);
            return placed;
        }

        private static void RegisterOnMap(TileMap map, PlacedTileObject placed, TileLayer layer, Vector3 worldPosition)
        {
            ITileLocation location = map.GetOrCreateTileLocation(layer, worldPosition);
            location.AddPlacedObject(placed, Direction.North);
            placed.transform.SetParent(map.transform);
        }

        private static void RecomputeAdjacency(TileMap map, params PlacedTileObject[] segments)
        {
            foreach (PlacedTileObject segment in segments)
            {
                map.AdjacencyEngine.QueueCascadeFrom(segment);
            }

            map.AdjacencyEngine.ProcessQueue();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo field = target.GetType().GetField(fieldName, flags);
            Assert.IsNotNull(field, $"Field {fieldName} not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}
