using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile;
using SS3D.Tests;
using System.Reflection;
using UnityEngine;

namespace EditorTests
{
    public class TileMapPhase1Tests : EditModeTest
    {
        [Test]
        public void ApplySyncedIdentity_ResolvesTileFromCatalogAssetId()
        {
            TileObjectSo floorSo = CreateNamedTestSo("Phase1IdentityFloor", TileLayer.Turf);

            CreateGameObject(out GameObject subsystemGo, out TileSubSystem tileSubSystem);
            TileResourceLoader loader = subsystemGo.AddComponent<TileResourceLoader>();
            loader.Catalog.Build(new[] { floorSo });
            ushort assetId = loader.Catalog.TryGetAssetId(floorSo);
            SetLoader(tileSubSystem, loader);

            CreateGameObject(out GameObject go, out PlacedTileObject placed);

            try
            {
                SubSystems.Register(tileSubSystem);
                SetSyncField(placed, "_syncAssetId", assetId);
                SetSyncField(placed, "_syncOriginX", 3);
                SetSyncField(placed, "_syncOriginY", 7);
                SetSyncField(placed, "_syncWorldOriginX", 3);
                SetSyncField(placed, "_syncWorldOriginY", 7);
                SetSyncField(placed, "_syncDirection", Direction.East);
                SetSyncField(placed, "_syncMapId", 2);

                MethodInfo applyIdentity = typeof(PlacedTileObject).GetMethod("ApplySyncedIdentity", BindingFlags.Instance | BindingFlags.NonPublic);
                applyIdentity.Invoke(placed, null);

                Assert.AreSame(floorSo, placed.tileObjectSO);
                Assert.AreEqual(new Vector2Int(3, 7), placed.Origin);
                Assert.AreEqual(new Vector2Int(3, 7), placed.WorldOrigin);
                Assert.AreEqual(Direction.East, placed.Direction);
                Assert.AreEqual(2, placed.MapId);
            }
            finally
            {
                SubSystems.Unregister(tileSubSystem);
            }
        }

        [Test]
        public void TryGetTileLocation_DoesNotCreateChunk()
        {
            TileMap map = CreateTileMap();

            bool found = map.TryGetTileLocation(TileLayer.Turf, new Vector3(4, 0, 4), out ITileLocation location);

            Assert.IsFalse(found);
            Assert.AreEqual(0, map.ChunkCount);
            Assert.IsNotNull(location);
            Assert.IsTrue(location.IsFullyEmpty());
        }

        [Test]
        public void CanBuild_DoesNotCreateChunk()
        {
            TileMap map = CreateTileMap();
            TileObjectSo turfSo = CreateTestSo(TileLayer.Turf);

            map.CanBuild(turfSo, new Vector3(0, 0, 0), Direction.North, false);

            Assert.AreEqual(0, map.ChunkCount);
        }

        [Test]
        public void GetOrCreateTileLocation_CreatesChunk()
        {
            TileMap map = CreateTileMap();

            map.GetOrCreateTileLocation(TileLayer.Turf, new Vector3(0, 0, 0));

            Assert.AreEqual(1, map.ChunkCount);
        }

        [Test]
        public void AtDirectionOf_WorksAcrossChunkBoundary()
        {
            PlacedTileObject southern = CreatePlacedTile(new Vector2Int(15, 0), new Vector2Int(15, 0), Direction.North);
            PlacedTileObject northern = CreatePlacedTile(new Vector2Int(15, 1), new Vector2Int(15, 1), Direction.North);

            Assert.IsTrue(northern.AtDirectionOf(southern, Direction.North));
            Assert.IsTrue(southern.AtDirectionOf(northern, Direction.South));
        }

        [Test]
        public void IsInFront_WorksAcrossChunkBoundary()
        {
            PlacedTileObject reference = CreatePlacedTile(new Vector2Int(15, 0), new Vector2Int(15, 0), Direction.North);
            PlacedTileObject inFront = CreatePlacedTile(new Vector2Int(15, 1), new Vector2Int(15, 1), Direction.North);

            Assert.IsTrue(inFront.IsInFront(reference));
            Assert.IsFalse(reference.IsInFront(inFront));
        }

        [Test]
        public void NeighbourAtDirectionOf_WorksAcrossChunkBoundary()
        {
            PlacedTileObject southern = CreatePlacedTile(new Vector2Int(15, 0), new Vector2Int(15, 0), Direction.North);
            PlacedTileObject northern = CreatePlacedTile(new Vector2Int(15, 1), new Vector2Int(15, 1), Direction.North);

            Assert.IsTrue(southern.NeighbourAtDirectionOf(northern, out Direction direction));
            Assert.AreEqual(Direction.North, direction);
        }

        [Test]
        public void TileQueryService_WorldToTile_MatchesGrid()
        {
            TileMap map = CreateTileMap();
            TileQueryService query = new TileQueryService(map);

            TileCoord coord = query.WorldToTile(new Vector3(12.4f, 0, 7.6f));

            Assert.AreEqual(12, coord.Grid.x);
            Assert.AreEqual(8, coord.Grid.y);
            Assert.AreEqual(0, coord.MapId);
        }

        private TileMap CreateTileMap()
        {
            TileMap map = TileMap.Create("Phase1TestMap");
            instantiated.Add(map.gameObject);
            return map;
        }

        private static TileObjectSo CreateTestSo(TileLayer layer) => CreateNamedTestSo("Phase1TestTile", layer);

        private static TileObjectSo CreateNamedTestSo(string name, TileLayer layer)
        {
            ObjectAssetReference prefabRef = ScriptableObject.CreateInstance<ObjectAssetReference>();
            prefabRef.name = name;

            TileObjectSo testSo = ScriptableObject.CreateInstance<TileObjectSo>();
            testSo.PrefabAsset = prefabRef;
            testSo.width = 1;
            testSo.height = 1;
            testSo.layer = layer;
            testSo.genericType = TileObjectGenericType.Floor;
            return testSo;
        }

        private PlacedTileObject CreatePlacedTile(Vector2Int worldOrigin, Vector2Int chunkOrigin, Direction direction)
        {
            CreateGameObject(out GameObject go, out PlacedTileObject placed);

            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_origin", chunkOrigin);
            SetPrivateField(placed, "_dir", direction);

            return placed;
        }

        private static void SetSyncField(PlacedTileObject placed, string fieldName, object value)
        {
            MethodInfo setter = typeof(PlacedTileObject).GetMethod($"sync___set_value_{fieldName}",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (setter != null)
            {
                setter.Invoke(placed, new[] { value, true });
                return;
            }

            SetPrivateField(placed, fieldName, value);
        }

        private static void SetLoader(TileSubSystem subsystem, TileResourceLoader loader)
        {
            MethodInfo setter = typeof(TileSubSystem).GetProperty("Loader")!.GetSetMethod(true);
            Assert.IsNotNull(setter, "TileSubSystem.Loader setter not found");
            setter.Invoke(subsystem, new object[] { loader });
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
