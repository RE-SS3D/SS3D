using NUnit.Framework;
using SS3D.Systems.Furniture;
using SS3D.Systems.Furniture.Disposal;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Tile;
using SS3D.Systems.Tile.Connections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EditorTests.Disposal
{
    public class DisposalPipeTests
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
        public void StraightDisposalPipeRun_FormsSingleNetwork()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacedTileObject western = CreateDisposalPipeAt(new Vector2Int(5, 5));
            PlacedTileObject eastern = CreateDisposalPipeAt(new Vector2Int(6, 5));

            RegisterOnMap(context.Map, western, TileLayer.Disposal, new Vector3(5, 0, 5));
            RegisterOnMap(context.Map, eastern, TileLayer.Disposal, new Vector3(6, 0, 5));
            RecomputeAdjacency(context.Map, western, eastern);

            DisposalNetworkRegistry registry = new();
            registry.RebuildAll(context.Map);

            Assert.AreEqual(1, registry.NetworkCount);
            Assert.IsTrue(registry.TryGetNetworkForSegment(
                new DisposalSegmentKey(new TileCoord(context.Map.MapId, 5, 5), TileLayer.Disposal, TileObjectSpecificType.None),
                out DisposalNetworkId westernNetwork,
                out DisposalNetworkRecord record));
            Assert.IsTrue(registry.TryGetNetworkForSegment(
                new DisposalSegmentKey(new TileCoord(context.Map.MapId, 6, 5), TileLayer.Disposal, TileObjectSpecificType.None),
                out DisposalNetworkId easternNetwork,
                out _));
            Assert.AreEqual(westernNetwork.Value, easternNetwork.Value);
            Assert.AreEqual(2, record.Segments.Count);
        }

        [Test]
        public void RemovingMiddleSegment_SplitsNetwork()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacedTileObject western = CreateDisposalPipeAt(new Vector2Int(5, 5));
            PlacedTileObject center = CreateDisposalPipeAt(new Vector2Int(6, 5));
            PlacedTileObject eastern = CreateDisposalPipeAt(new Vector2Int(7, 5));

            RegisterOnMap(context.Map, western, TileLayer.Disposal, new Vector3(5, 0, 5));
            RegisterOnMap(context.Map, center, TileLayer.Disposal, new Vector3(6, 0, 5));
            RegisterOnMap(context.Map, eastern, TileLayer.Disposal, new Vector3(7, 0, 5));
            RecomputeAdjacency(context.Map, western, center, eastern);

            DisposalNetworkRegistry registry = new();
            registry.RebuildAll(context.Map);
            Assert.AreEqual(1, registry.NetworkCount);

            context.Map.GetOrCreateTileLocation(TileLayer.Disposal, new Vector3(6, 0, 5)).ClearAllPlacedObject();
            registry.RebuildAround(context.Map, new TileCoord(context.Map.MapId, 6, 5));

            Assert.AreEqual(2, registry.NetworkCount);
        }

        [Test]
        public void PipeUnderBin_ReportsBinAsTerminal_AndRouteReachesIt()
        {
            TileMapTestUtilities.MapContext context = TileMapTestUtilities.CreateContext(_instantiated);
            PlacedTileObject entry = CreateDisposalPipeAt(new Vector2Int(5, 5));
            PlacedTileObject link = CreateDisposalPipeAt(new Vector2Int(6, 5));

            RegisterOnMap(context.Map, entry, TileLayer.Disposal, new Vector3(5, 0, 5));
            RegisterOnMap(context.Map, link, TileLayer.Disposal, new Vector3(6, 0, 5));

            DisposalBin bin = CreateDisposalElement<DisposalBin>(new Vector2Int(5, 5));
            RegisterOnMap(context.Map, bin.GetComponent<PlacedTileObject>(), TileLayer.FurnitureBase, new Vector3(5, 0, 5));

            RecomputeAdjacency(context.Map, entry, link);

            DisposalNetworkWalkResult walk = DisposalPipeConnectivity.CollectNetwork(context.Map, entry);
            Assert.AreEqual(2, walk.Segments.Count);
            Assert.AreEqual(1, walk.Terminals.Count);
            Assert.AreSame(bin.GameObject, walk.Terminals[0].GameObject);

            Assert.IsTrue(DisposalPipeConnectivity.TryFindRoute(context.Map, link, bin, out List<PlacedTileObject> path));
            Assert.AreEqual(entry, path[path.Count - 1]);
        }

        [Test]
        public void DisposalBin_AcceptsSize_HonorsMaxSizeClassCeiling()
        {
            DisposalBin bin = CreateDisposalElement<DisposalBin>(new Vector2Int(3, 3));
            SetPrivateField(bin, "_maxSizeClass", SizeClass.Normal);

            Assert.IsTrue(bin.AcceptsSize(SizeClass.Tiny));
            Assert.IsTrue(bin.AcceptsSize(SizeClass.Normal));
            Assert.IsFalse(bin.AcceptsSize(SizeClass.Bulky));
            Assert.IsFalse(bin.AcceptsSize(SizeClass.Huge));
        }

        [Test]
        public void MainOutlet_GraceExpiry_SkipsItemsAlreadyMovedAway()
        {
            GameObject outletObject = new GameObject("MainOutlet");
            _instantiated.Add(outletObject);
            DisposalOutlet outlet = outletObject.AddComponent<DisposalOutlet>();
            SetPrivateField(outlet, "_targetDepartment", Department.None);
            SetPrivateField(outlet, "_graceWindowSeconds", 0.01f);

            GameObject itemObject = new GameObject("PendingItem");
            _instantiated.Add(itemObject);
            Item item = itemObject.AddComponent<Item>();
            itemObject.transform.position = outletObject.transform.position;

            outlet.OnItemArrived(item);

            // Simulate pickup / walk-away: item is no longer near the outlet.
            itemObject.transform.position = outletObject.transform.position + Vector3.right * 10f;

            MethodInfo resolve = typeof(DisposalOutlet).GetMethod(
                "ResolveUnclaimed",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(resolve);
            resolve.Invoke(outlet, new object[] { item });

            Assert.IsNotNull(item);
            Assert.IsNotNull(item.gameObject);
        }

        private PlacedTileObject CreateDisposalPipeAt(Vector2Int worldOrigin)
        {
            GameObject go = new GameObject($"DisposalPipe_{worldOrigin}");
            _instantiated.Add(go);

            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            go.AddComponent<DisposalPipeAdjacencyConnector>();

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = TileObjectGenericType.Disposal;
            so.specificType = TileObjectSpecificType.None;
            so.layer = TileLayer.Disposal;

            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_connector", placed.GetComponent<IAdjacencyConnector>());
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_origin", worldOrigin);
            SetPrivateField(placed, "_mapId", 0);
            placed.transform.position = new Vector3(worldOrigin.x, 0, worldOrigin.y);
            return placed;
        }

        private T CreateDisposalElement<T>(Vector2Int worldOrigin) where T : Component
        {
            GameObject go = new GameObject($"DisposalElement_{worldOrigin}");
            _instantiated.Add(go);

            PlacedTileObject placed = go.AddComponent<PlacedTileObject>();
            T element = go.AddComponent<T>();

            TileObjectSo so = ScriptableObject.CreateInstance<TileObjectSo>();
            so.genericType = TileObjectGenericType.None;
            so.specificType = TileObjectSpecificType.None;
            so.layer = TileLayer.FurnitureBase;

            SetPrivateField(placed, "_tileObjectSo", so);
            SetPrivateField(placed, "_worldOrigin", worldOrigin);
            SetPrivateField(placed, "_origin", worldOrigin);
            SetPrivateField(placed, "_mapId", 0);
            placed.transform.position = new Vector3(worldOrigin.x, 0, worldOrigin.y);
            return element;
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
                map.AdjacencyEngine.QueueCascadeFrom(segment);

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
