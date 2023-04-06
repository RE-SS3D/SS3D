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
            Assert.True(container.Items.Contains(item));
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
        private static Item createItem()
        {
            var go = new GameObject();
            var item = go.AddComponent<Item>();
            item.Init("TestItem", 1f, Vector2Int.one, new List<Trait>());
            return item;
        }
        #endregion
    }
}
