using System.Collections.Generic;
using System.Reflection;
using FishNet.Object.Synchronizing;
using NUnit.Framework;
using SS3D.Systems;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// A move between two containers (pocket to toolbox) syncs two network objects, so a client can run the new
    /// container's add before the old container's remove. The late remove must leave the item hidden, frozen and
    /// parented where the add put it (playtest: a gas mask dragged from a pocket into a toolbox hung visible
    /// under the toolbox).
    /// </summary>
    public class ContainerMoveOrderTests
    {
        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                Object.DestroyImmediate(go);
            }

            _spawned.Clear();
        }

        [Test]
        public void LateRemoveFromTheOldContainer_LeavesTheItemInTheNewOne()
        {
            AttachedContainer pocket = CreateContainer("Pocket", attachItems: false);
            AttachedContainer toolbox = CreateContainer("Toolbox", attachItems: true);
            Item mask = CreateItem(out MeshRenderer renderer, out BoxCollider collider);

            Sync(pocket, SyncListOperation.Add, default, new StoredItem(mask, Vector2Int.zero));
            Sync(toolbox, SyncListOperation.Add, default, new StoredItem(mask, Vector2Int.zero));
            Sync(pocket, SyncListOperation.RemoveAt, new StoredItem(mask, Vector2Int.zero), default);

            Assert.IsFalse(renderer.enabled, "Still hidden inside the toolbox");
            Assert.IsFalse(collider.enabled, "Still frozen inside the toolbox");
            Assert.AreEqual(toolbox.transform, mask.transform.parent, "Still parented to the toolbox");
            Assert.AreEqual(toolbox, mask.LocalHolder);
        }

        [Test]
        public void RemoveThenAdd_StillEndsInTheNewContainer()
        {
            AttachedContainer pocket = CreateContainer("Pocket", attachItems: false);
            AttachedContainer toolbox = CreateContainer("Toolbox", attachItems: true);
            Item mask = CreateItem(out MeshRenderer renderer, out BoxCollider collider);

            Sync(pocket, SyncListOperation.Add, default, new StoredItem(mask, Vector2Int.zero));
            Sync(pocket, SyncListOperation.RemoveAt, new StoredItem(mask, Vector2Int.zero), default);
            Sync(toolbox, SyncListOperation.Add, default, new StoredItem(mask, Vector2Int.zero));

            Assert.IsFalse(renderer.enabled);
            Assert.IsFalse(collider.enabled);
            Assert.AreEqual(toolbox.transform, mask.transform.parent);
        }

        [Test]
        public void RemoveIntoTheWorld_ShowsAndReleasesTheItem()
        {
            AttachedContainer toolbox = CreateContainer("Toolbox", attachItems: true);
            Item mask = CreateItem(out MeshRenderer renderer, out BoxCollider collider);

            Sync(toolbox, SyncListOperation.Add, default, new StoredItem(mask, Vector2Int.zero));
            Sync(toolbox, SyncListOperation.RemoveAt, new StoredItem(mask, Vector2Int.zero), default);

            Assert.IsTrue(renderer.enabled);
            Assert.IsTrue(collider.enabled);
            Assert.IsNull(mask.transform.parent);
            Assert.IsNull(mask.LocalHolder);
        }

        private static void Sync(AttachedContainer container, SyncListOperation op, StoredItem oldItem, StoredItem newItem)
        {
            MethodInfo handler = typeof(AttachedContainer).GetMethod("HandleStoredItemsChanged",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(handler, "AttachedContainer.HandleStoredItemsChanged");
            handler.Invoke(container, new object[] { op, 0, oldItem, newItem, false });
        }

        private AttachedContainer CreateContainer(string name, bool attachItems)
        {
            GameObject go = Track(new GameObject(name));
            AttachedContainer container = go.AddComponent<AttachedContainer>();
            container.Init(new Vector2Int(4, 2), null);
            SetField(container, "_attachItems", attachItems);
            return container;
        }

        private Item CreateItem(out MeshRenderer renderer, out BoxCollider collider)
        {
            GameObject go = Track(new GameObject("GasMask"));
            renderer = go.AddComponent<MeshRenderer>();
            collider = go.AddComponent<BoxCollider>();
            Item item = go.AddComponent<Item>();
            item.Init("GasMask", 1f, new List<Trait>());
            return item;
        }

        private GameObject Track(GameObject go)
        {
            _spawned.Add(go);
            return go;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{fieldName}");
            field.SetValue(target, value);
        }
    }
}
