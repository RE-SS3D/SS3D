using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SS3D.Systems;
using SS3D.Systems.Inventory.Items;

namespace EditorTests
{
    public class ItemTests
    {
        #region Tests

        /// <summary>
        /// Test to confirm that the item has it's starting traits after creation
        /// </summary>
        [Test]
        public void CreatedItemsShouldHaveStartingTraits()
        {
            // ARRANGE
            List<Trait> traits = new();

            // ACT
            Item item = createItemWithRandomTraits(ref traits);

            // ASSERT
            foreach (Trait trait in traits)
            {
                Assert.True(item.HasTrait(trait));
            }

        }

        /// <summary>
        /// Test to confirm you can add traits to an item after it has been created
        /// </summary>
        [Test]
        public void ShouldBeAbleToAddTraitsToItem()
        {
            // ARRANGE
            Item item = createItem();

            // ACT
            Trait trait = ScriptableObject.CreateInstance<Trait>();
            trait.Name = "Trait1";

            item.AddTrait(trait);

            // ASSERT
            Assert.True(item.HasTrait(trait));

        }

        /// <summary>
        /// Test to confirm that the HasTrait function doesn't return true when the item 
        /// doesn't have the required trait
        /// </summary>
        [Test]
        public void ShouldNotHaveUnaddedTraits()
        {
            // ARRANGE
            Item item = createItem();

            // ACT
            Trait trait = ScriptableObject.CreateInstance<Trait>();
            trait.Name = "Trait1";

            // ASSERT
            Assert.False(item.HasTrait(trait));

        }
        #endregion

        #region Helper functions
        /// <summary>
        /// Creates an Item with Random Traits for testing
        /// </summary>
        /// <param name="traits"></param>
        /// <returns></returns>
        private static Item createItemWithRandomTraits(ref List<Trait> traits)
        {
            // Create sample traits
            Trait trait1 = ScriptableObject.CreateInstance<Trait>();
            trait1.Name = "Trait 1";

            Trait trait2 = ScriptableObject.CreateInstance<Trait>();
            trait1.Name = "Trait 2";

            traits = new List<Trait>() { trait1, trait2 };

            // Apply the traits to the item and return it
            var go = new GameObject();
            var item = go.AddComponent<Item>();
            item.Init("TestItem", 1f, traits);
            return item;
        }

        /// <summary>
        /// Creates an Item with the given Traits for testing
        /// </summary>
        /// <param name="traits"></param>
        /// <returns></returns>
        private static Item createItemWithTraits(List<Trait> traits)
        {
            var go = new GameObject();
            var item = go.AddComponent<Item>();
            item.Init("TestItem", 1f, traits);
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
            item.Init("TestItem", 1f, new List<Trait>());
            return item;
        }
        #endregion
    }
}
