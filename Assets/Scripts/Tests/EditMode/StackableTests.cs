using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EditorTests
{
    public class StackableTests
    {
        private HashSet<GameObject> _sceneRootsBeforeTest;

        [SetUp]
        public void SetUp()
        {
            _sceneRootsBeforeTest = SceneManager.GetActiveScene()
                .GetRootGameObjects()
                .ToHashSet();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (!_sceneRootsBeforeTest.Contains(gameObject))
                {
                    Object.DestroyImmediate(gameObject);
                }
            }
        }

        [Test]
        public void CompatibleItemsMerge()
        {
            Stackable destination = CreateStackableItem("Cable", 5);
            Stackable source = CreateStackableItem("Cable", 5);
            AttachedContainer sourceContainer = CreateContainer(new Vector2Int(1, 1));
            sourceContainer.AddItem(source.Item);

            int moved = destination.MergeFrom(source);

            Assert.AreEqual(1, moved);
            Assert.AreEqual(2, destination.Amount);
            Assert.True(destination.StackContainer.Items.Contains(source.Item));
        }

        [Test]
        public void IncompatibleItemsDoNotMerge()
        {
            Stackable destination = CreateStackableItem("Cable", 5);
            Stackable source = CreateStackableItem("Glass", 5);
            AttachedContainer sourceContainer = CreateContainer(new Vector2Int(1, 1));
            sourceContainer.AddItem(source.Item);

            int moved = destination.MergeFrom(source);

            Assert.AreEqual(0, moved);
            Assert.AreEqual(1, destination.Amount);
            Assert.True(sourceContainer.Items.Contains(source.Item));
        }

        [Test]
        public void MergeCapsAtMaxStack()
        {
            Stackable destination = CreateStackableItem("Cable", 3);
            Stackable source = CreateStackableItem("Cable", 5, 3);
            AttachedContainer sourceContainer = CreateContainer(new Vector2Int(1, 1));
            sourceContainer.AddItem(source.Item);

            int moved = destination.MergeFrom(source);

            Assert.AreEqual(2, moved);
            Assert.AreEqual(3, destination.Amount);
            Assert.True(destination.IsFull);
        }

        [Test]
        public void PartialMergeLeavesLeftoversInSource()
        {
            Stackable destination = CreateStackableItem("Cable", 4, 3);
            Stackable source = CreateStackableItem("Cable", 5, 3);
            AttachedContainer sourceContainer = CreateContainer(new Vector2Int(1, 1));
            sourceContainer.AddItem(source.Item);

            int moved = destination.MergeFrom(source);

            Assert.AreEqual(1, moved);
            Assert.AreEqual(4, destination.Amount);
            Assert.AreEqual(2, source.Amount);
            Assert.True(sourceContainer.Items.Contains(source.Item));
        }

        [Test]
        public void FullMergeRemovesSourceVisibleItemFromOriginalContainer()
        {
            Stackable destination = CreateStackableItem("Cable", 5);
            Stackable source = CreateStackableItem("Cable", 5, 2);
            AttachedContainer sourceContainer = CreateContainer(new Vector2Int(1, 1));
            sourceContainer.AddItem(source.Item);

            int moved = destination.MergeFrom(source);

            Assert.AreEqual(2, moved);
            Assert.False(sourceContainer.Items.Contains(source.Item));
            Assert.True(destination.StackContainer.Items.Contains(source.Item));
        }

        [Test]
        public void TakeOneReducesStackAmountByOne()
        {
            Stackable stackable = CreateStackableItem("Cable", 5, 3);

            Item taken = stackable.TakeOne();

            Assert.NotNull(taken);
            Assert.AreEqual(2, stackable.Amount);
            Assert.False(stackable.StackContainer.Items.Contains(taken));
        }

        [Test]
        public void SplitRemovesRequestedAmountFromStack()
        {
            Stackable stackable = CreateStackableItem("Cable", 5, 4);

            Item[] splitItems = stackable.Split(2);

            Assert.AreEqual(2, splitItems.Length);
            Assert.AreEqual(2, stackable.Amount);
            Assert.False(stackable.StackContainer.Items.Contains(splitItems[0]));
            Assert.False(stackable.StackContainer.Items.Contains(splitItems[1]));
        }

        [Test]
        public void VisualCopiesStayHiddenWhenStackItemIsHidden()
        {
            Stackable stackable = CreateStackableItem("Cable", 3);
            GameObject visualCopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visualCopy.transform.SetParent(stackable.transform);
            visualCopy.SetActive(false);
            stackable.Item.SetVisibility(false);

            stackable.StackContainer.AddItem(CreateItem("Cable"));
            stackable.Init(3, stackable.StackContainer, new[] { visualCopy });

            Assert.True(visualCopy.activeSelf);
            Assert.False(visualCopy.GetComponent<Renderer>().enabled);
            Assert.False(visualCopy.GetComponent<Collider>().enabled);
        }

        [Test]
        public void StoredItemsStayHiddenWhenStackItemIsShown()
        {
            Stackable stackable = CreateStackableItem("Cable", 3);
            Item storedItem = CreateRenderedItem("Cable");
            Renderer storedRenderer = storedItem.GetComponent<Renderer>();
            stackable.StackContainer.AddItem(storedItem);
            storedItem.SetVisibility(false);

            stackable.Item.SetVisibility(false);
            stackable.Item.SetVisibility(true);

            Assert.True(stackable.Item.IsVisible());
            Assert.False(storedRenderer.enabled);
        }

        public static Stackable CreateStackableItem(string itemName, int maxStack, int amount = 1)
        {
            GameObject go = new(itemName);
            Item item = go.AddComponent<Item>();
            item.Init(itemName, 1f, new List<Trait>());

            Stackable stackable = go.AddComponent<Stackable>();

            GameObject containerObject = new($"{itemName} Stack Container");
            containerObject.transform.SetParent(go.transform);
            AttachedContainer stackContainer = containerObject.AddComponent<AttachedContainer>();
            stackContainer.Init(new Vector2Int(maxStack - 1, 1), null);
            stackable.Init(maxStack, stackContainer);

            for (int i = 1; i < amount; i++)
            {
                Item contained = CreateItem(itemName);
                stackContainer.AddItem(contained);
            }

            return stackable;
        }

        public static AttachedContainer CreateContainer(Vector2Int size)
        {
            GameObject go = new();
            AttachedContainer container = go.AddComponent<AttachedContainer>();
            container.Init(size, null);
            return container;
        }

        public static Item CreateItem(string itemName)
        {
            GameObject go = new(itemName);
            Item item = go.AddComponent<Item>();
            item.Init(itemName, 1f, new List<Trait>());
            return item;
        }

        private static Item CreateRenderedItem(string itemName)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = itemName;
            Item item = go.AddComponent<Item>();
            item.Init(itemName, 1f, new List<Trait>());
            return item;
        }
    }
}
