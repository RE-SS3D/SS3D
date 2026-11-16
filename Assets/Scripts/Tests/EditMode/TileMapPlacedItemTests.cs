using NUnit.Framework;
using SS3D.Data.AssetDatabases;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests.EditMode
{
    /// <summary>
    /// Bug report 000028: an item despawned outside the map (exploded minibomb) left a dead
    /// PlacedItemObject in TileMap's item list, so the map editor's delete ServerRpc threw and
    /// FishNet kicked the host.
    /// </summary>
    public sealed class TileMapPlacedItemTests
    {
        private readonly List<Object> _created = new();

        [SetUp]
        public void SetUp()
        {
            // PlaceItemObject warns or errors about a missing NetworkObject or TileSubSystem with no server running.
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object obj in _created)
            {
                if (obj != null)
                    Object.DestroyImmediate(obj);
            }

            _created.Clear();
            LogAssert.ignoreFailingMessages = false;
        }

        [Test]
        public void ClearItemObject_AfterNeighbourDestroyedExternally_DoesNotThrowAndClearsTarget()
        {
            TileMap map = TileMap.Create("placed-item-test");
            Track(map.gameObject);
            ItemObjectSo bombSo = CreateItemSo("TestMiniBomb");

            GameObject exploded = PlaceItem(map, bombSo, new Vector3(0f, 0f, 0f));
            PlaceItem(map, bombSo, new Vector3(0.3f, 0f, 0f));
            Assert.AreEqual(2, map.PlacedItems.Count);

            // Detonation despawns the GameObject directly, never through the map.
            Object.DestroyImmediate(exploded);

            Assert.DoesNotThrow(() => map.ClearItemObject(new Vector3(0.3f, 0f, 0f), bombSo));
            Assert.AreEqual(0, map.PlacedItems.Count, "dead entry pruned and survivor removed");
        }

        [Test]
        public void ClearItemObject_WithNoMatch_LeavesLiveItemsTracked()
        {
            TileMap map = TileMap.Create("placed-item-nomatch");
            Track(map.gameObject);
            ItemObjectSo bombSo = CreateItemSo("TestMiniBomb");

            PlaceItem(map, bombSo, Vector3.zero);
            map.ClearItemObject(new Vector3(10f, 0f, 10f), bombSo);

            Assert.AreEqual(1, map.PlacedItems.Count);
        }

        private GameObject PlaceItem(TileMap map, ItemObjectSo so, Vector3 position)
        {
            GameObject item = Track(new GameObject(so.NameString));
            map.PlaceItemObject(position, Quaternion.identity, so, item);
            return item;
        }

        private ItemObjectSo CreateItemSo(string name)
        {
            ObjectAssetReference prefabRef = Track(ScriptableObject.CreateInstance<ObjectAssetReference>());
            prefabRef.name = name;
            ItemObjectSo so = Track(ScriptableObject.CreateInstance<ItemObjectSo>());
            so.PrefabAsset = prefabRef;
            return so;
        }

        private T Track<T>(T obj) where T : Object
        {
            _created.Add(obj);
            return obj;
        }
    }
}
