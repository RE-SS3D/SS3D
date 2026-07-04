using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Tests;
using System.Reflection;
using UnityEngine;

namespace EditorTests
{
    public class AdjacencyEngineTests : EditModeTest
    {
        [Test]
        public void SimpleConnectionRule_ConnectsMatchingTypes()
        {
            PlacedTileObject self = CreatePlacedTile(TileObjectGenericType.Table, TileObjectSpecificType.None);
            PlacedTileObject neighbour = CreatePlacedTileWithConnector(TileObjectGenericType.Table, TileObjectSpecificType.None);

            var rule = new SimpleConnectionRule(TileObjectGenericType.Table, TileObjectSpecificType.None);

            Assert.IsTrue(rule.IsConnected(self, neighbour));
        }

        [Test]
        public void SimpleConnectionRule_RejectsMismatchedGenericType()
        {
            PlacedTileObject self = CreatePlacedTile(TileObjectGenericType.Table, TileObjectSpecificType.None);
            PlacedTileObject neighbour = CreatePlacedTileWithConnector(TileObjectGenericType.Wall, TileObjectSpecificType.None);

            var rule = new SimpleConnectionRule(TileObjectGenericType.Table, TileObjectSpecificType.None);

            Assert.IsFalse(rule.IsConnected(self, neighbour));
        }

        [Test]
        public void AdjacencyEngine_DeduplicatesQueuedTiles()
        {
            TileMap map = TileMap.Create("AdjacencyEngineTest");
            instantiated.Add(map.gameObject);

            var engine = map.AdjacencyEngine;
            CreateGameObject(out GameObject go, out PlacedTileObject placed);

            engine.QueueUpdate(placed);
            engine.QueueUpdate(placed);
            engine.ProcessQueue();
        }

        [Test]
        public void ComputeAdjacencyMap_SetsNorthConnectionForAdjacentNeighbour()
        {
            TileMap map = TileMap.Create("AdjacencyMapTest");
            instantiated.Add(map.gameObject);

            PlacedTileObject southern = CreatePlacedTileAt(new Vector2Int(5, 5), TileObjectGenericType.Floor, TileObjectSpecificType.None);
            PlacedTileObject northern = CreatePlacedTileAt(new Vector2Int(5, 6), TileObjectGenericType.Floor, TileObjectSpecificType.None);

            RegisterOnMap(map, southern, TileLayer.Turf, new Vector3(5, 0, 5));
            RegisterOnMap(map, northern, TileLayer.Turf, new Vector3(5, 0, 6));

            var rule = new SimpleConnectionRule(TileObjectGenericType.Floor, TileObjectSpecificType.None);
            AdjacencyMap adjacencyMap = AdjacencyEngine.ComputeAdjacencyMap(southern, rule, map);

            Assert.IsTrue(adjacencyMap.HasConnection(Direction.North));
            Assert.IsFalse(adjacencyMap.HasConnection(Direction.South));
        }

        private PlacedTileObject CreatePlacedTile(TileObjectGenericType genericType, TileObjectSpecificType specificType)
        {
            CreateGameObject(out GameObject go, out PlacedTileObject placed);
            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = genericType;
            so.specificType = specificType;
            so.layer = TileLayer.Turf;

            SetPrivateField(placed, "_tileObjectSo", so);
            go.AddComponent<SimpleAdjacencyConnector>();
            SetPrivateField(placed, "_connector", go.GetComponent<IAdjacencyConnector>());

            return placed;
        }

        private PlacedTileObject CreatePlacedTileWithConnector(TileObjectGenericType genericType, TileObjectSpecificType specificType)
        {
            return CreatePlacedTile(genericType, specificType);
        }

        private PlacedTileObject CreatePlacedTileAt(Vector2Int worldOrigin, TileObjectGenericType genericType, TileObjectSpecificType specificType)
        {
            PlacedTileObject placed = CreatePlacedTile(genericType, specificType);
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            placed.transform.position = new Vector3(worldOrigin.x, 0, worldOrigin.y);
            return placed;
        }

        private static void RegisterOnMap(TileMap map, PlacedTileObject placed, TileLayer layer, Vector3 worldPosition)
        {
            ITileLocation location = map.GetOrCreateTileLocation(layer, worldPosition);
            location.AddPlacedObject(placed, Direction.North);
            placed.transform.SetParent(map.transform);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(target, value);
        }
    }
}
