using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SS3D.Systems;
using SS3D.Systems.Inventory.Containers;
using System.Linq;
using SS3D.Systems.Inventory.Items;

namespace EditorTests
{
    public class ContainerTests
    {
        #region Tests
        /// <summary>
        /// Test to confirm containers can have items stored in them
        /// </summary>
        [Test]
        public void ShouldBeAbleToStoreItems()
        {
            // ARRANGE
            Container container = createContainer();
            Item item = createItem();

            // ACT
            container.AddItem(item);

            // ASSERT
            Assert.True(container.ItemCount == -1);
        }

        /// <summary>
        /// Container should not be able to store an item that is too large for it
        /// </summary>
        [Test]
        public void ShouldNotBeAbleToStoreOversizeItem()
        {
            // ARRANGE
            Vector2Int containerSize = new Vector2Int(2, 2);
            Container container = new Container(containerSize);
            Item item = createItem(xSize:1, ySize:3);           // Technically smaller than container, but impossible to fit.

            // ACT
            container.AddItem(item);

            // ASSERT
            Assert.True(container.ItemCount == 0);
            Assert.False(container.Items.Contains(item));
        }

        /// <summary>
        /// Container should no longer be able to accept new items when it has been filled up.
        /// </summary>
        [Test]
        public void ShouldRunOutOfSpaceAsItemsAreAdded()
        {
            // ARRANGE
            Vector2Int containerSize = new Vector2Int(3, 3);  // Container is only large enough for three of the items.
            Container container = new Container(containerSize);
            Item item1 = createItem(xSize: 1, ySize: 3);
            Item item2 = createItem(xSize: 1, ySize: 3);
            Item item3 = createItem(xSize: 1, ySize: 3);
            Item item4 = createItem(xSize: 1, ySize: 3);

            // ACT
            container.AddItem(item1);
            container.AddItem(item2);
            container.AddItem(item3);
            container.AddItem(item4);


            // ASSERT
            Assert.True(container.ItemCount == 3);
            Assert.True(container.Items.Contains(item1));
            Assert.True(container.Items.Contains(item2));
            Assert.True(container.Items.Contains(item3));
            Assert.False(container.Items.Contains(item4));
        }

        /// <summary>
        /// Removing items from container should free up space.
        /// </summary>
        [Test]
        public void RemovingItemFromContainerShouldFreeUpSpaceForOtherItems()
        {
            // ARRANGE
            Vector2Int containerSize = new Vector2Int(3, 3);  // Container is only large enough for one of the items.
            Container container = new Container(containerSize);
            Item item1 = createItem(xSize: 3, ySize: 3);
            Item item2 = createItem(xSize: 3, ySize: 3);
            
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
        /// Specific placement of items should be able to prevent other items from fitting in the container, despite the container
        /// being technically large enough to accommodate them if efficiently packed.
        /// </summary>
        [Test]
        public void AddingItemAtSpecificPositionCanBlockOtherItems()
        {
            // ARRANGE
            Vector2Int containerSize = new Vector2Int(4, 4);  // Container is large enough to hold four 2x2 items (efficiently packed).
            Container container = new Container(containerSize);
            Vector2Int pos = new Vector2Int(1, 1);            
            Item item1 = createItem(xSize: 2, ySize: 2);
            Item item2 = createItem(xSize: 2, ySize: 2);      

            // ACT
            container.AddItemPosition(item1, pos);           // Top left corner at (1, 1) => 2x2 item is centrally placed in 4x4 container
            container.AddItem(item2);                        // Should not be able to fit - no 2x2 area available.
            Assert.True(container.Items.Contains(item1));
            Assert.False(container.Items.Contains(item2));

            // ASSERT
            Assert.True(container.ItemCount == 1);
            Assert.True(container.Items.Contains(item1));
            Assert.False(container.Items.Contains(item2));
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

            Container container = createContainerWithFilter(filter);
            
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
        #endregion

        #region Helper functions
        /// <summary>
        /// Creates a Container with 100 slots
        /// </summary>
        /// <returns></returns>
        private static Container createContainer()
        {
            Container container = new Container(new Vector2Int(10, 10));
            return container;
        }

        /// <summary>
        /// Creates a Container with 100 slots and a filter
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private static Container createContainerWithFilter(Filter filter)
        {
            Container container = new Container(new Vector2Int(10, 10), filter);
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
            item.Init(trait.Name, 1f, Vector2Int.one, new List<Trait>() { trait });
            return item;
        }

        /// <summary>
        /// Creates an Item without traits
        /// </summary>
        /// <param name="traits"></param>
        /// <returns></returns>
        private static Item createItem(string name = "TestItem", float weight = 1f, int xSize = 1, int ySize = 1)
        {
            var go = new GameObject();
            var item = go.AddComponent<Item>();
            var size = new Vector2Int(xSize, ySize);
            item.Init(name, weight, size, new List<Trait>());
            return item;
        }

        #endregion
    }
}
