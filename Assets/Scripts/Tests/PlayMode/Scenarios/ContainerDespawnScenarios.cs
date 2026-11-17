using System.Collections;
using System.Linq;
using FishNet;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data;
using SS3D.Data.Generated;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    /// <summary>Containers whose contents are despawned in the tick they changed.</summary>
    [Category(TestCategories.Scenarios)]
    public class ContainerDespawnScenarios : ScenarioFixture
    {
        // Guards: an item stored and despawned in one tick leaves its prefab asset alone on the host (AttachedContainer.HandleStoredItemsChanged, Item.IsSceneInstance). FishNet writes an unspawned item as its PrefabId, so the host's client reads back the asset · Not cheaper because: needs FishNet's real SyncList write and the host's client read
        [UnityTest]
        public IEnumerator ItemStoredAndDespawnedInOneTick_LeavesItsPrefabAlone()
        {
            Item prefab = Assets.Get<Item>(AssetDatabases.Items, Items.FacewearGasMask);
            bool prefabKinematic = prefab.GetComponent<Rigidbody>().isKinematic;
            bool[] prefabRenderers = prefab.GetComponentsInChildren<Renderer>(true).Select(r => r.enabled).ToArray();

            ItemSubSystem items = SubSystems.Get<ItemSubSystem>();
            Item backpack = items.SpawnItem(Items.Backpack, Body.transform.position + Vector3.right, Quaternion.identity);
            Assert.IsTrue(ItemStorage.TryGetStorageOn(backpack, out AttachedContainer storage), "the backpack has storage");
            yield return Hold(0.3f);

            // Round end does this to every loadout: contents and holder go in the tick the contents arrived.
            Item mask = items.SpawnItemInContainer(Items.FacewearGasMask, storage);
            InstanceFinder.ServerManager.Despawn(mask.GameObject);
            InstanceFinder.ServerManager.Despawn(backpack.GameObject);
            yield return Hold(0.3f);

            Assert.AreEqual(prefabKinematic, prefab.GetComponent<Rigidbody>().isKinematic, "The prefab is not frozen");
            CollectionAssert.AreEqual(prefabRenderers, prefab.GetComponentsInChildren<Renderer>(true).Select(r => r.enabled).ToArray(),
                "The prefab is not hidden");
            Assert.IsNull(prefab.LocalHolder, "No container claims the prefab");
        }
    }
}
