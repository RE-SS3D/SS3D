using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Tests;
using System.Reflection;
using UnityEngine;

namespace EditorTests
{
    public class TileMapPhase1Tests : EditModeTest
    {
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

        private static TileObjectSo CreateTestSo(TileLayer layer)
        {
            TileObjectSo testSo = ScriptableObject.CreateInstance<TileObjectSo>();
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

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
