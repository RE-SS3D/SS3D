using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SS3D.Systems;
using SS3D.Systems.Inventory.Containers;
using System.Linq;
using SS3D.Systems.Inventory.Items;
using UnityEngine.SceneManagement;

namespace EditorTests
{
    public class ContainerTests
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

        #region Tests
        /// <summary>
        /// Test to confirm containers can have items stored in them
        /// </summary>
        [Test]
        public void ShouldBeAbleToStoreItems()
        {
			// ARRANGE
			AttachedContainer container = CreateContainer(new Vector2Int(10,10), null);
			Item item = createItem();

            // ACT
            container.AddItem(item);

            // ASSERT
            Assert.True(container.ItemCount == 1);
            Assert.True(container.Items.Contains(item));
        }

        /// <summary>
        /// Container should no longer be able to accept new items when it has been filled up.
        /// </summary>
        [Test]
        public void ShouldRunOutOfSpaceAsItemsAreAdded()
        {
            // ARRANGE
            Vector2Int containerSize = new Vector2Int(2, 2);  // Container is only large enough for four of the items.
			AttachedContainer container = CreateContainer(containerSize, null);
			Item item1 = createItem();
            Item item2 = createItem();
            Item item3 = createItem();
            Item item4 = createItem();
			Item item5 = createItem();

			// ACT
			container.AddItem(item1);
            container.AddItem(item2);
            container.AddItem(item3);
            container.AddItem(item4);
			container.AddItem(item5);


			// ASSERT
			Assert.True(container.ItemCount == 4);
            Assert.True(container.Items.Contains(item1));
            Assert.True(container.Items.Contains(item2));
            Assert.True(container.Items.Contains(item3));
            Assert.True(container.Items.Contains(item4));
			Assert.False(container.Items.Contains(item5));
		}

        /// <summary>
        /// Removing items from container should free up space.
        /// </summary>
        [Test]
        public void RemovingItemFromContainerShouldFreeUpSpaceForOtherItems()
        {
            // ARRANGE
            Vector2Int containerSize = new Vector2Int(1, 1);  // Container is only large enough for one of the items.
			AttachedContainer container = CreateContainer(containerSize, null);
			Item item1 = createItem();
            Item item2 = createItem();
            
            // Preload container with first item, and confirm that it cannot accept the second item (because it is full after the first)
            container.AddItem(item1);
            container.AddItem(item2);
            Assert.True(container.Items.Contains(item1));
            Assert.False(container.Items.Contains(item2));

            // ACT
            container.RemoveItem(item1);
            container.AddItem(item2);

            // ASSERT
            Assert.True(container.ItemCount == 1);
            Assert.False(container.Items.Contains(item1));
            Assert.True(container.Items.Contains(item2));
        }

        /// <summary>
        /// Test to confirm containers can filter items correctly
        /// </summary>
        [Test]
        public void ShouldBeAbleToStoreOnlyFilteredItems()
        {
            // ARRANGE
            Trait acceptedTrait = ScriptableObject.CreateInstance<Trait>();
            acceptedTrait.Name = "Accepted Trait";

            Trait neutralTrait = ScriptableObject.CreateInstance<Trait>();
            neutralTrait.Name = "Neutral Trait";

            Trait deniedTrait = ScriptableObject.CreateInstance<Trait>();
            deniedTrait.Name = "Denied Trait";

            Filter filter = ScriptableObject.CreateInstance<Filter>();
            filter.acceptedTraits = new List<Trait>() { acceptedTrait };
            filter.deniedTraits = new List<Trait>() { deniedTrait };

			AttachedContainer container = CreateContainer(new Vector2Int(10,10), filter);
            
            Item acceptedItem = createItemWithTrait(acceptedTrait);
            Item neutralItem = createItemWithTrait(neutralTrait);
            Item deniedItem = createItemWithTrait(deniedTrait);


            // ACT
            container.AddItem(acceptedItem);
            container.AddItem(neutralItem);
            container.AddItem(deniedItem);

            // ASSERT
            Assert.True(container.Items.Contains(acceptedItem), "should always accept items with Accepted Traits");
            Assert.False(container.Items.Contains(neutralItem), "should only accept items with Accepted Traits");
            Assert.False(container.Items.Contains(deniedItem), "should never accept items with Denied Traits");
        }

        [Test]
        public void OccupiedSlotsRejectNormalNonStackableMoves()
        {
            AttachedContainer source = CreateContainer(new Vector2Int(1, 1), null);
            AttachedContainer destination = CreateContainer(new Vector2Int(1, 1), null);
            Item sourceItem = createItem("Source");
            Item destinationItem = createItem("Destination");
            source.AddItem(sourceItem);
            destination.AddItem(destinationItem);

            bool transferred = source.TransferItemToOther(sourceItem, Vector2Int.zero, destination);

            Assert.False(transferred);
            Assert.True(source.Items.Contains(sourceItem));
            Assert.True(destination.Items.Contains(destinationItem));
        }

        [Test]
        public void OccupiedSlotsAcceptCompatibleStackMerges()
        {
            AttachedContainer source = CreateContainer(new Vector2Int(1, 1), null);
            AttachedContainer destination = CreateContainer(new Vector2Int(1, 1), null);
            Stackable sourceStack = StackableTests.CreateStackableItem("Cable", 5);
            Stackable destinationStack = StackableTests.CreateStackableItem("Cable", 5);
            source.AddItem(sourceStack.Item);
            destination.AddItem(destinationStack.Item);

            bool transferred = source.TransferItemToOther(sourceStack.Item, Vector2Int.zero, destination);

            Assert.True(transferred);
            Assert.False(source.Items.Contains(sourceStack.Item));
            Assert.True(destination.Items.Contains(destinationStack.Item));
            Assert.AreEqual(2, destinationStack.Amount);
        }

        [Test]
        public void TransferToContainerMergesWithCompatibleStackWhenNoSlotIsSpecified()
        {
            AttachedContainer source = CreateContainer(new Vector2Int(1, 1), null);
            AttachedContainer destination = CreateContainer(new Vector2Int(1, 1), null);
            Stackable sourceStack = StackableTests.CreateStackableItem("Cable", 5);
            Stackable destinationStack = StackableTests.CreateStackableItem("Cable", 5);
            source.AddItem(sourceStack.Item);
            destination.AddItem(destinationStack.Item);

            bool transferred = source.TransferItemToOther(sourceStack.Item, destination);

            Assert.True(transferred);
            Assert.False(source.Items.Contains(sourceStack.Item));
            Assert.AreEqual(2, destinationStack.Amount);
        }

        [Test]
        public void SpawnedStackItemMergesIntoFullContainerStack()
        {
            AttachedContainer destination = CreateContainer(new Vector2Int(1, 1), null);
            Stackable destinationStack = StackableTests.CreateStackableItem("Cable", 5);
            Stackable spawnedStack = StackableTests.CreateStackableItem("Cable", 5);
            destination.AddItem(destinationStack.Item);

            bool added = destination.AddItem(spawnedStack.Item);

            Assert.True(added);
            Assert.AreEqual(2, destinationStack.Amount);
            Assert.False(destination.Items.Contains(spawnedStack.Item));
            Assert.True(destinationStack.StackContainer.Items.Contains(spawnedStack.Item));
            Assert.AreEqual(destinationStack.StackContainer, spawnedStack.Item.Container);
        }

        [Test]
        public void StackMergeHonorsSourceRemovalConditions()
        {
            AttachedContainer source = CreateContainer(new Vector2Int(1, 1), null);
            source.gameObject.AddComponent<DenyRemoveStorageCondition>();
            AttachedContainer destination = CreateContainer(new Vector2Int(1, 1), null);
            Stackable sourceStack = StackableTests.CreateStackableItem("Cable", 5, 2);
            Stackable destinationStack = StackableTests.CreateStackableItem("Cable", 5);
            source.AddItem(sourceStack.Item);
            destination.AddItem(destinationStack.Item);

            bool transferred = source.TransferItemToOther(sourceStack.Item, Vector2Int.zero, destination);

            Assert.False(transferred);
            Assert.True(source.Items.Contains(sourceStack.Item));
            Assert.AreEqual(2, sourceStack.Amount);
            Assert.AreEqual(1, destinationStack.Amount);
        }

        [Test]
        public void TransferWithinSameContainerMovesItemToEmptySlot()
        {
            AttachedContainer container = CreateContainer(new Vector2Int(2, 1), null);
            Item item = createItem("Source");
            container.AddItemPosition(item, Vector2Int.zero);

            bool transferred = container.TransferItemToOther(item, new Vector2Int(1, 0), container);

            Assert.True(transferred);
            Assert.AreEqual(new Vector2Int(1, 0), container.PositionOf(item));
            Assert.AreEqual(container, item.Container);
        }

        [Test]
        public void FailedTransferLeavesSourceContainerUnchanged()
        {
            AttachedContainer source = CreateContainer(new Vector2Int(1, 1), null);
            AttachedContainer destination = CreateContainer(new Vector2Int(1, 1), null);
            Item sourceItem = createItem("Source");
            Item destinationItem = createItem("Destination");
            source.AddItem(sourceItem);
            destination.AddItem(destinationItem);

            bool transferred = source.TransferItemToOther(sourceItem, Vector2Int.zero, destination);

            Assert.False(transferred);
            Assert.AreEqual(source, sourceItem.Container);
            Assert.True(source.Items.Contains(sourceItem));
        }
        #endregion

        #region Helper functions

        /// <summary>
        /// Creates a Container with a given size and filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static AttachedContainer CreateContainer(Vector2Int size, Filter filter)
        {
			GameObject go = new GameObject();
			AttachedContainer container = go.AddComponent<AttachedContainer>();
			container.Init(size, filter);
            return container;
        }

        /// <summary>
        /// Creates an Item with a single trait
        /// </summary>
        /// <param name="traits"></param>
        /// <returns></returns>
        private static Item createItemWithTrait(Trait trait)
        {
            var go = new GameObject();
            var item = go.AddComponent<Item>();
            item.Init(trait.Name, 1f, new List<Trait>() { trait });
            return item;
        }

        /// <summary>
        /// Creates an Item without traits
        /// </summary>
        /// <param name="traits"></param>
        /// <returns></returns>
        private static Item createItem(string name = "TestItem", float weight = 1f)
        {
            var go = new GameObject();
            var item = go.AddComponent<Item>();
            item.Init(name, weight, new List<Trait>());
            return item;
        }

        private sealed class DenyRemoveStorageCondition : MonoBehaviour, IStorageCondition
        {
            public bool CanStore(AttachedContainer container, Item item)
            {
                return true;
            }

            public bool CanRemove(AttachedContainer container, Item item)
            {
                return false;
            }
        }

        #endregion
    }
}
