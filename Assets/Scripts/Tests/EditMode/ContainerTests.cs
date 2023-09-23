using NUnit.Framework;
using SS3D.Systems;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EditorTests
{
    public class ContainerTests
    {
        /// <summary>
        /// Test to confirm containers can have items stored in them.
        /// </summary>
        [Test]
        public void ShouldBeAbleToStoreItems()
        {
            // ARRANGE
            AttachedContainer container = CreateContainer(new Vector2Int(10, 10), null);
            Item item = CreateItem();

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
            Item item1 = CreateItem();
            Item item2 = CreateItem();
            Item item3 = CreateItem();
            Item item4 = CreateItem();
            Item item5 = CreateItem();

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
            Item item1 = CreateItem();
            Item item2 = CreateItem();

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
        /// Test to confirm containers can filter items correctly.
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
            filter.AcceptedTraits = new List<Trait>() { acceptedTrait };
            filter.DeniedTraits = new List<Trait>() { deniedTrait };

            AttachedContainer container = CreateContainer(new Vector2Int(10, 10), filter);

            Item acceptedItem = CreateItemWithTrait(acceptedTrait);
            Item neutralItem = CreateItemWithTrait(neutralTrait);
            Item deniedItem = CreateItemWithTrait(deniedTrait);

            // ACT
            container.AddItem(acceptedItem);
            container.AddItem(neutralItem);
            container.AddItem(deniedItem);

            // ASSERT
            Assert.True(container.Items.Contains(acceptedItem), "should always accept items with Accepted Traits");
            Assert.False(container.Items.Contains(neutralItem), "should only accept items with Accepted Traits");
            Assert.False(container.Items.Contains(deniedItem), "should never accept items with Denied Traits");
        }

        /// <summary>
        /// Creates a Container with a given size and filter.
        /// </summary>
        /// <param name="filter"></param>
        private static AttachedContainer CreateContainer(Vector2Int size, Filter filter)
        {
            GameObject go = new GameObject();
            AttachedContainer container = go.AddComponent<AttachedContainer>();
            container.Init(size, filter);
            return container;
        }

        /// <summary>
        /// Creates an Item with a single trait.
        /// </summary>
        private static Item CreateItemWithTrait(Trait trait)
        {
            GameObject go = new GameObject();
            Item item = go.AddComponent<Item>();
            item.Init(trait.Name, 1f, new List<Trait>() { trait });
            return item;
        }

        /// <summary>
        /// Creates an Item without traits.
        /// </summary>
        private static Item CreateItem(string name = "TestItem", float weight = 1f)
        {
            GameObject go = new GameObject();
            Item item = go.AddComponent<Item>();
            item.Init(name, weight, new List<Trait>());
            return item;
        }
    }
}