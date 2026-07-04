using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Tests;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EditorTests
{
    public class AdjacencyEngineTests : EditModeTest
    {
        private static readonly DoorConnectionRule DoorRule = new();
        [Test]
        public void DoorConnectionRule_ConnectsAdjacentWall()
        {
            PlacedTileObject door = CreateDoorTile(new Vector2Int(5, 5), Direction.North);
            PlacedTileObject wall = CreateWallTile(new Vector2Int(4, 5));

            Assert.IsTrue(DoorRule.IsConnected(door, wall));
        }

        [Test]
        public void WallConnectionRule_ConnectsDoorOnLeftOrRight()
        {
            TileMap map = TileMap.Create("WallDoorRuleTest");
            instantiated.Add(map.gameObject);

            PlacedTileObject door = CreateDoorTile(new Vector2Int(5, 5), Direction.North);
            PlacedTileObject wall = CreateWallTile(new Vector2Int(4, 5));
            RegisterOnMap(map, door, TileLayer.Turf, new Vector3(5, 0, 5));
            RegisterOnMap(map, wall, TileLayer.Turf, new Vector3(4, 0, 5));

            var rule = new WallConnectionRule(map);

            Assert.IsTrue(rule.IsConnected(wall, door));
        }

        [Test]
        public void WallConnectionRule_RejectsDoorInFront()
        {
            TileMap map = TileMap.Create("WallDoorFrontRuleTest");
            instantiated.Add(map.gameObject);

            PlacedTileObject door = CreateDoorTile(new Vector2Int(5, 5), Direction.North);
            PlacedTileObject wall = CreateWallTile(new Vector2Int(5, 6));
            RegisterOnMap(map, door, TileLayer.Turf, new Vector3(5, 0, 5));
            RegisterOnMap(map, wall, TileLayer.Turf, new Vector3(5, 0, 6));

            var rule = new WallConnectionRule(map);

            Assert.IsFalse(rule.IsConnected(wall, door));
        }

        [Test]
        public void MultiAdjacencyConnector_FansOutSetAdjacencyConnections()
        {
            CreateGameObject(out GameObject root, out PlacedTileObject placed);
            MultiAdjacencyConnector multi = root.AddComponent<MultiAdjacencyConnector>();

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = TileObjectGenericType.Wall;
            so.specificType = TileObjectSpecificType.Steel;
            so.layer = TileLayer.Turf;
            SetPrivateField(placed, "_tileObjectSo", so);

            CreateGameObject(out GameObject childA, out AdvancedAdjacencyConnector connectorA);
            CreateGameObject(out GameObject childB, out AdvancedAdjacencyConnector connectorB);
            childA.transform.SetParent(root.transform);
            childB.transform.SetParent(root.transform);

            SetPrivateField(multi, "_connectors", new List<GameObject> { childA, childB });

            multi.SetAdjacencyConnections(0b00001010);

            FieldInfo pendingField = typeof(AdvancedAdjacencyConnector).GetField("_pendingEngineConnections",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(0b00001010, pendingField.GetValue(connectorA));
            Assert.AreEqual(0b00001010, pendingField.GetValue(connectorB));
        }

        [Test]
        public void AdvancedConnector_UsesTypeMatchingConnectionRule()
        {
            PlacedTileObject self = CreatePlacedTile(TileObjectGenericType.Wall, TileObjectSpecificType.None);
            PlacedTileObject neighbour = CreatePlacedTileWithConnector(TileObjectGenericType.Wall, TileObjectSpecificType.None);

            CreateGameObject(out GameObject connectorGo, out AdvancedAdjacencyConnector connector);
            connectorGo.transform.SetParent(self.transform);

            Assert.IsTrue(connector.ConnectionRule.IsConnected(self, neighbour));
        }

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

        private PlacedTileObject CreateBarePlacedTile(TileObjectGenericType genericType, TileObjectSpecificType specificType)
        {
            CreateGameObject(out GameObject go, out PlacedTileObject placed);
            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = genericType;
            so.specificType = specificType;
            so.layer = TileLayer.Turf;
            SetPrivateField(placed, "_tileObjectSo", so);
            return placed;
        }

        private PlacedTileObject CreatePlacedTile(TileObjectGenericType genericType, TileObjectSpecificType specificType)
        {
            PlacedTileObject placed = CreateBarePlacedTile(genericType, specificType);
            placed.gameObject.AddComponent<SimpleAdjacencyConnector>();
            SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());

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
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            FieldInfo field = target.GetType().GetField(fieldName, flags);
            Assert.IsNotNull(field, $"Field {fieldName} not found on {target.GetType().Name}");
            field.SetValue(target, value);
        }
    }
}
