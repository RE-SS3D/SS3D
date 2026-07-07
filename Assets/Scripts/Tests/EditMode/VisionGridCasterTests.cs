using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Vision;
using SS3D.Tests;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EditorTests
{
    public class VisionGridCasterTests : EditModeTest
    {
        [Test]
        public void EmptyGrid_AllSamplesReachMaxRange()
        {
            MapContext context = CreateMapWithPlenum(new Vector3(5f, 0f, 5f));
            var occlusion = new VisionOcclusionProvider(context.Map, context.Query);

            float[] depths = new float[8];
            VisionGridCaster.Cast(
                occlusion,
                context.Query,
                new Vector3(5f, 0f, 5f),
                yawRadians: 0f,
                viewRange: 10f,
                viewConeWidthDegrees: 360f,
                sampleCount: depths.Length,
                depths);

            foreach (float depth in depths)
                Assert.AreEqual(1f, depth, 0.001f);
        }

        [Test]
        public void SingleWall_BlocksNorthRay()
        {
            MapContext context = CreateMapWithPlenum(new Vector3(5f, 0f, 5f), new Vector3(5f, 0f, 6f));
            PlacedTileObject wall = CreateWallTile(new Vector2Int(5, 6));
            RegisterOnMap(context.Map, wall, new Vector3(5f, 0f, 6f));
            ProcessAdjacency(context.Map, wall);

            var occlusion = new VisionOcclusionProvider(context.Map, context.Query);
            float depth = VisionGridCaster.CastRay(
                occlusion,
                context.Query,
                new Vector3(5f, 0f, 5f),
                angleRadians: 0f,
                maxRange: 10f);

            Assert.Less(depth, 1.5f);
            Assert.Greater(depth, 0.4f);
        }

        [Test]
        public void LShapedWall_BlocksNorthBeforeEast()
        {
            MapContext context = CreateMapWithPlenum(
                new Vector3(5f, 0f, 5f),
                new Vector3(5f, 0f, 6f),
                new Vector3(6f, 0f, 6f));

            PlacedTileObject northWall = CreateWallTile(new Vector2Int(5, 6));
            PlacedTileObject eastWall = CreateWallTile(new Vector2Int(6, 6));
            RegisterOnMap(context.Map, northWall, new Vector3(5f, 0f, 6f));
            RegisterOnMap(context.Map, eastWall, new Vector3(6f, 0f, 6f));
            ProcessAdjacency(context.Map, northWall);
            ProcessAdjacency(context.Map, eastWall);

            var occlusion = new VisionOcclusionProvider(context.Map, context.Query);

            float northDepth = VisionGridCaster.CastRay(
                occlusion,
                context.Query,
                new Vector3(5f, 0f, 5f),
                angleRadians: 0f,
                maxRange: 10f);

            float eastDepth = VisionGridCaster.CastRay(
                occlusion,
                context.Query,
                new Vector3(5f, 0f, 5f),
                angleRadians: Mathf.PI * 0.5f,
                maxRange: 10f);

            Assert.Less(northDepth, 1.5f);
            Assert.Greater(eastDepth, 1.5f);
        }

        [Test]
        public void DoorTile_BlocksRayUntilOpenStateExists()
        {
            MapContext context = CreateMapWithPlenum(new Vector3(5f, 0f, 5f), new Vector3(5f, 0f, 6f));
            PlacedTileObject door = CreateDoorTile(new Vector2Int(5, 6), Direction.North);
            RegisterOnMap(context.Map, door, new Vector3(5f, 0f, 6f));
            ProcessAdjacency(context.Map, door);

            var occlusion = new VisionOcclusionProvider(context.Map, context.Query);
            float depth = VisionGridCaster.CastRay(
                occlusion,
                context.Query,
                new Vector3(5f, 0f, 5f),
                angleRadians: 0f,
                maxRange: 10f);

            Assert.Less(depth, 1.5f);
        }

        [Test]
        public void ChunkBoundaryRay_HitsWallAcrossChunkEdge()
        {
            MapContext context = CreateMapWithPlenum(new Vector3(15f, 0f, 5f), new Vector3(16f, 0f, 5f));
            PlacedTileObject wall = CreateWallTile(new Vector2Int(16, 5));
            RegisterOnMap(context.Map, wall, new Vector3(16f, 0f, 5f));
            ProcessAdjacency(context.Map, wall);

            var occlusion = new VisionOcclusionProvider(context.Map, context.Query);
            float depth = VisionGridCaster.CastRay(
                occlusion,
                context.Query,
                new Vector3(15f, 0f, 5f),
                angleRadians: Mathf.PI * 0.5f,
                maxRange: 10f);

            Assert.Less(depth, 1.5f);
        }

        [Test]
        public void TryGetOccupancy_TurfWallBlocksVision()
        {
            MapContext context = CreateMapWithPlenum(new Vector3(5f, 0f, 5f), new Vector3(5f, 0f, 6f));
            PlacedTileObject wall = CreateWallTile(new Vector2Int(5, 6));
            RegisterOnMap(context.Map, wall, new Vector3(5f, 0f, 6f));

            bool found = context.Query.TryGetOccupancy(new TileCoord(0, 5, 6), out TileOccupancy occupancy);

            Assert.IsTrue(found);
            Assert.IsTrue(occupancy.HasWall);
            Assert.IsTrue(occupancy.BlocksVision);
            Assert.AreNotEqual(0, occupancy.BlockedEdges);
        }

        private MapContext CreateMapWithPlenum(params Vector3[] positions)
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(instantiated);

            foreach (Vector3 position in positions)
                TileMapTestUtilities.PlacePlenum(context, position);

            return new MapContext(context.Map, context.Query);
        }

        private static void ProcessAdjacency(TileMap map, PlacedTileObject placed)
        {
            map.AdjacencyEngine.QueueCascadeFrom(placed);
            map.AdjacencyEngine.ProcessQueue();
        }

        private PlacedTileObject CreateWallTile(Vector2Int worldOrigin)
        {
            PlacedTileObject placed = CreateBarePlacedTile(TileObjectGenericType.Wall, TileObjectSpecificType.Steel);
            placed.gameObject.AddComponent<WallAdjacencyConnector>();
            SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_dir", Direction.North);
            placed.transform.position = new Vector3(worldOrigin.x, 0, worldOrigin.y);
            return placed;
        }

        private PlacedTileObject CreateDoorTile(Vector2Int worldOrigin, Direction direction)
        {
            PlacedTileObject placed = CreateBarePlacedTile(TileObjectGenericType.Door, TileObjectSpecificType.Steel);
            placed.gameObject.AddComponent<DoorAdjacencyConnector>();
            SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_dir", direction);
            placed.transform.position = new Vector3(worldOrigin.x, 0, worldOrigin.y);
            return placed;
        }

        private PlacedTileObject CreateBarePlacedTile(
            TileObjectGenericType genericType,
            TileObjectSpecificType specificType,
            TileLayer layer = TileLayer.Turf)
        {
            CreateGameObject(out GameObject go, out PlacedTileObject placed);
            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = genericType;
            so.specificType = specificType;
            so.layer = layer;
            SetPrivateField(placed, "_tileObjectSo", so);
            return placed;
        }

        private static void RegisterOnMap(TileMap map, PlacedTileObject placed, Vector3 worldPosition)
        {
            ITileLocation location = map.GetOrCreateTileLocation(TileLayer.Turf, worldPosition);
            location.AddPlacedObject(placed, Direction.North);
            placed.transform.SetParent(map.transform);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo field = target.GetType().GetField(fieldName, flags);
            Assert.IsNotNull(field, $"Field {fieldName} not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }

        private readonly struct MapContext
        {
            public MapContext(TileMap map, TileQueryService query)
            {
                Map = map;
                Query = query;
            }

            public TileMap Map { get; }
            public TileQueryService Query { get; }
        }
    }
}
