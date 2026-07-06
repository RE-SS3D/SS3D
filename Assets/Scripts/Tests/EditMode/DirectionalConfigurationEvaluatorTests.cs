using NUnit.Framework;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using SS3D.Systems.Tile.Connections.AdjacencyTypes;
using SS3D.Tests;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace EditorTests
{
    public class DirectionalConfigurationEvaluatorTests : EditModeTest
    {
        [Test]
        public void Evaluate_NoNeighbours_ReturnsOShape()
        {
            PlacedTileObject self = CreateDirectionalTile(new Vector2Int(5, 5), Direction.North);
            DirectionnalShapeResolver resolver = CreateResolver();

            DirectionalAdjacencyResult result = DirectionalConfigurationEvaluator.Evaluate(
                self,
                new List<PlacedTileObject>(),
                DefaultNeighbourState,
                resolver);

            Assert.AreEqual(AdjacencyShape.O, result.Shape);
            Assert.AreEqual(0, result.ConnectionCount);
            Assert.IsNull(result.FirstNeighbour);
            Assert.IsNull(result.SecondNeighbour);
        }

        [Test]
        public void Evaluate_IConfiguration_TwoSideNeighboursFacingSameWay()
        {
            PlacedTileObject self = CreateDirectionalTile(new Vector2Int(5, 5), Direction.North);
            PlacedTileObject west = CreateDirectionalTile(new Vector2Int(4, 5), Direction.North);
            PlacedTileObject east = CreateDirectionalTile(new Vector2Int(6, 5), Direction.North);
            DirectionnalShapeResolver resolver = CreateResolver();

            DirectionalAdjacencyResult result = DirectionalConfigurationEvaluator.Evaluate(
                self,
                new List<PlacedTileObject> { west, east },
                DefaultNeighbourState,
                resolver);

            Assert.AreEqual(AdjacencyShape.I, result.Shape);
            Assert.AreEqual(2, result.ConnectionCount);
            Assert.IsNotNull(result.FirstNeighbour);
            Assert.IsNotNull(result.SecondNeighbour);
        }

        [Test]
        public void Evaluate_URight_SingleNeighbourOnRight()
        {
            PlacedTileObject self = CreateDirectionalTile(new Vector2Int(5, 5), Direction.North);
            PlacedTileObject east = CreateDirectionalTile(new Vector2Int(6, 5), Direction.North);
            DirectionnalShapeResolver resolver = CreateResolver();

            DirectionalAdjacencyResult result = DirectionalConfigurationEvaluator.Evaluate(
                self,
                new List<PlacedTileObject> { east },
                DefaultNeighbourState,
                resolver);

            Assert.AreEqual(AdjacencyShape.URight, result.Shape);
            Assert.AreEqual(1, result.ConnectionCount);
            Assert.AreEqual(east, result.FirstNeighbour);
            Assert.IsNull(result.SecondNeighbour);
        }

        [Test]
        public void Evaluate_ULeft_SingleNeighbourOnLeft()
        {
            PlacedTileObject self = CreateDirectionalTile(new Vector2Int(5, 5), Direction.North);
            PlacedTileObject west = CreateDirectionalTile(new Vector2Int(4, 5), Direction.North);
            DirectionnalShapeResolver resolver = CreateResolver();

            DirectionalAdjacencyResult result = DirectionalConfigurationEvaluator.Evaluate(
                self,
                new List<PlacedTileObject> { west },
                DefaultNeighbourState,
                resolver);

            Assert.AreEqual(AdjacencyShape.ULeft, result.Shape);
            Assert.AreEqual(1, result.ConnectionCount);
            Assert.AreEqual(west, result.FirstNeighbour);
        }

        [Test]
        public void NeighbourAlreadyFullyConnected_ReturnsTrueWhenFrozen()
        {
            PlacedTileObject self = CreateDirectionalTile(new Vector2Int(5, 5), Direction.North);
            PlacedTileObject otherA = CreateDirectionalTile(new Vector2Int(0, 0), Direction.North);
            PlacedTileObject otherB = CreateDirectionalTile(new Vector2Int(1, 0), Direction.North);
            PlacedTileObject frozen = CreateDirectionalTile(new Vector2Int(6, 5), Direction.North);

            var frozenState = new DirectionalNeighbourState(
                frozen,
                Direction.North,
                AdjacencyShape.I,
                connectionCount: 2,
                otherA,
                otherB);

            Assert.IsTrue(DirectionalConfigurationEvaluator.NeighbourAlreadyFullyConnected(self, frozenState));
        }

        [Test]
        public void NeighbourAlreadyFullyConnected_ReturnsFalseWhenSelfIsPartner()
        {
            PlacedTileObject self = CreateDirectionalTile(new Vector2Int(5, 5), Direction.North);
            PlacedTileObject partner = CreateDirectionalTile(new Vector2Int(6, 5), Direction.North);

            var state = new DirectionalNeighbourState(
                partner,
                Direction.North,
                AdjacencyShape.URight,
                connectionCount: 2,
                self,
                null);

            Assert.IsFalse(DirectionalConfigurationEvaluator.NeighbourAlreadyFullyConnected(self, state));
        }

        [Test]
        public void Evaluate_SkipsFullyConnectedNeighbour()
        {
            PlacedTileObject self = CreateDirectionalTile(new Vector2Int(5, 5), Direction.North);
            PlacedTileObject west = CreateDirectionalTile(new Vector2Int(4, 5), Direction.North);
            PlacedTileObject east = CreateDirectionalTile(new Vector2Int(6, 5), Direction.North);
            PlacedTileObject frozenPartnerA = CreateDirectionalTile(new Vector2Int(7, 5), Direction.North);
            PlacedTileObject frozenPartnerB = CreateDirectionalTile(new Vector2Int(6, 6), Direction.North);
            DirectionnalShapeResolver resolver = CreateResolver();

            DirectionalAdjacencyResult result = DirectionalConfigurationEvaluator.Evaluate(
                self,
                new List<PlacedTileObject> { west, east },
                tile =>
                {
                    if (tile == east)
                    {
                        return new DirectionalNeighbourState(
                            east,
                            Direction.North,
                            AdjacencyShape.I,
                            2,
                            frozenPartnerA,
                            frozenPartnerB);
                    }

                    return DefaultNeighbourState(tile);
                },
                resolver);

            Assert.AreEqual(AdjacencyShape.ULeft, result.Shape);
            Assert.AreEqual(1, result.ConnectionCount);
            Assert.AreEqual(west, result.FirstNeighbour);
        }

        [Test]
        public void Connector_Recompute_UsesEvaluatorForOShape()
        {
            TileMap map = TileMap.Create("DirectionalEvaluatorConnectorTest");
            instantiated.Add(map.gameObject);

            PlacedTileObject placed = CreateDirectionalTile(new Vector2Int(5, 5), Direction.North);
            placed.gameObject.AddComponent<MeshFilter>();
            DirectionalAdjacencyConnector connector = placed.gameObject.AddComponent<DirectionalAdjacencyConnector>();
            connector.AdjacencyResolver = CreateResolver();
            SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());
            RegisterOnMap(map, placed, TileLayer.FurnitureBase, new Vector3(5, 0, 5));

            map.AdjacencyEngine.QueueCascadeFrom(placed);
            map.AdjacencyEngine.ProcessQueue();

            FieldInfo shapeField = typeof(DirectionalAdjacencyConnector).GetField("_pendingShape",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(AdjacencyShape.O, shapeField.GetValue(connector));
        }

        private static DirectionalNeighbourState DefaultNeighbourState(PlacedTileObject tile)
        {
            return new DirectionalNeighbourState(tile, tile.Direction, AdjacencyShape.O, 0, null, null);
        }

        private static DirectionnalShapeResolver CreateResolver()
        {
            return new DirectionnalShapeResolver
            {
                o = CreateMesh("o"),
                uLeft = CreateMesh("uLeft"),
                uRight = CreateMesh("uRight"),
                i = CreateMesh("i"),
                lIn = CreateMesh("lIn"),
                lOut = CreateMesh("lOut"),
            };
        }

        private static Mesh CreateMesh(string name)
        {
            var mesh = new Mesh { name = name };
            return mesh;
        }

        private PlacedTileObject CreateDirectionalTile(Vector2Int worldOrigin, Direction direction)
        {
            CreateGameObject(out GameObject go, out PlacedTileObject placed);
            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = TileObjectGenericType.Booth;
            so.specificType = TileObjectSpecificType.None;
            so.layer = TileLayer.FurnitureBase;
            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_dir", direction);
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
