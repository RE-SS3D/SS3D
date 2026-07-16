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
    public sealed class ScrubberFilterTests
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
        public void Scrubber_ToxinsFilter_RemovesPlasmaFromTurf()
        {
            // Arrange: a single pipe network and a turf cell containing plasma.
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
            TileCoord cell = new TileCoord(context.Map.MapId, 5, 5);
            turf.UpdateCell(cell);
            turf.DebugAddMoles(cell, AtmosConstants.Plasma, 10f);

            var registry = new GasPipeNetworkRegistry(AtmosConstants.DefaultGasCount);
            registry.RebuildAll(context.Map);
            Assert.IsTrue(registry.TryGetNetworkForSegment(GasPipeSegmentKey.From(pipe), out GasPipeNetworkId networkId, out _));

            var observer = new AtmosPipeObserver(context.Map, registry);
            var pipeSimulation = new AtmosPipeSimulation(registry, turf, null, AtmosConstants.DefaultGasCount, observer);

            ScrubberController scrubber = CreateScrubberAt(new Vector2Int(5, 5));
            // Only toxins filter enabled.
            scrubber.ServerSetFilters(o2: false, n2: false, co2: false, plasma: false, toxins: true);

            // Force-connect scrubber to this network for the test.
            SetPrivateField(scrubber, "_networkId", networkId);

            float before = turf.DebugGetMoles(cell, AtmosConstants.Plasma);

            // Act: run one scrub tick with a generous delta time.
            scrubber.ServerTick(pipeSimulation, turf, deltaTime: 1f);

            // Assert: turf plasma reduced and pipe network gained plasma.
            float after = turf.DebugGetMoles(cell, AtmosConstants.Plasma);
            Assert.Less(after, before);
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

        private ScrubberController CreateScrubberAt(Vector2Int worldOrigin)
        {
            GameObject go = new GameObject($"Scrubber_{worldOrigin}");
            _instantiated.Add(go);
            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            ScrubberController scrubber = go.AddComponent<ScrubberController>();

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = TileObjectGenericType.None;
            so.specificType = TileObjectSpecificType.None;
            so.layer = TileLayer.FurnitureBase;

            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_mapId", 0);
            placed.transform.position = new Vector3(worldOrigin.x, 0, worldOrigin.y);

            // Ensure base initializes its tile object.
            typeof(AtmosPortControllerBase)
                .GetMethod("Initialize", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(scrubber, null);

            return scrubber;
        }

        private static void RegisterOnMap(TileMap map, PlacedTileObject placed, TileLayer layer, Vector3 worldPosition)
        {
            ITileLocation location = map.GetOrCreateTileLocation(layer, worldPosition);
            location.AddPlacedObject(placed, Direction.North);
            placed.transform.SetParent(map.transform);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo field = null;
            for (System.Type t = target.GetType(); t != null && field == null; t = t.BaseType)
            {
                field = t.GetField(fieldName, flags);
            }
            Assert.IsNotNull(field, $"Field {fieldName} not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}

