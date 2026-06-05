using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using SS3D.Systems.Inventory.Containers;
using System.Linq;
using SS3D.Systems.Inventory.Items;

namespace EditorTests
{
    public class StackableTests
    {
        #region Tests

        /// <summary>
        /// Item with a Stackable component should report IsStackable as true
        /// </summary>
        [Test]
        public void Item_ShouldBeStackable_WhenStackableComponentPresent()
        {
            Item item = CreateStackableItem(5, 1, "Coin");

            Assert.True(item.IsStackable);
        }

        /// <summary>
        /// Item without a Stackable component should report IsStackable as false
        /// </summary>
        [Test]
        public void Item_ShouldNotBeStackable_WithoutStackableComponent()
        {
            Item item = CreateItem("Coin");

            Assert.False(item.IsStackable);
        }

        /// <summary>
        /// CanAddToStack should return true when there is space for the given amount
        /// </summary>
        [Test]
        public void Stackable_CanAddToStack_WhenUnderMax()
        {
            Stackable stackable = CreateStackable(10, 3);

            Assert.True(stackable.CanAddToStack(5));
        }

        /// <summary>
        /// CanAddToStack should return false when the stack is at maximum capacity
        /// </summary>
        [Test]
        public void Stackable_CannotAddToStack_WhenAtMax()
        {
            Stackable stackable = CreateStackable(10, 10);

            Assert.False(stackable.CanAddToStack(1));
        }

        /// <summary>
        /// CanAddToStack should return false when adding would exceed the maximum
        /// </summary>
        [Test]
        public void Stackable_CannotAddToStack_WhenOverMax()
        {
            Stackable stackable = CreateStackable(10, 8);

            Assert.False(stackable.CanAddToStack(5));
        }

        /// <summary>
        /// AddToStack should return the overflow amount when adding more than available space
        /// </summary>
        [Test]
        public void Stackable_AddToStack_ReturnsOverflow()
        {
            Stackable stackable = CreateStackable(10, 8);
            int overflow = stackable.AddToStack(5);

            Assert.AreEqual(3, overflow);
            Assert.AreEqual(10, stackable.AmountInStack);
        }

        /// <summary>
        /// AddToStack should increase the amount in stack correctly
        /// </summary>
        [Test]
        public void Stackable_AddToStack_IncreasesAmount()
        {
            Stackable stackable = CreateStackable(10, 3);
            stackable.AddToStack(4);

            Assert.AreEqual(7, stackable.AmountInStack);
        }

        /// <summary>
        /// AddToStack should return zero overflow when adding exactly to max
        /// </summary>
        [Test]
        public void Stackable_AddToStack_NoOverflowWhenExactFit()
        {
            Stackable stackable = CreateStackable(10, 5);
            int overflow = stackable.AddToStack(5);

            Assert.AreEqual(0, overflow);
            Assert.AreEqual(10, stackable.AmountInStack);
        }

        /// <summary>
        /// RemoveFromStack should return the actual number of items removed
        /// </summary>
        [Test]
        public void Stackable_RemoveFromStack_ReturnsActualRemoved()
        {
            Stackable stackable = CreateStackable(10, 5);
            int removed = stackable.RemoveFromStack(3);

            Assert.AreEqual(3, removed);
            Assert.AreEqual(2, stackable.AmountInStack);
        }

        /// <summary>
        /// RemoveFromStack should decrease the amount in stack
        /// </summary>
        [Test]
        public void Stackable_RemoveFromStack_DecreasesAmount()
        {
            Stackable stackable = CreateStackable(10, 5);
            stackable.RemoveFromStack(2);

            Assert.AreEqual(3, stackable.AmountInStack);
        }

        /// <summary>
        /// RemoveFromStack should not reduce the stack below 1 item
        /// </summary>
        [Test]
        public void Stackable_RemoveFromStack_CannotGoBelowOne()
        {
            Stackable stackable = CreateStackable(10, 5);
            int removed = stackable.RemoveFromStack(10);

            Assert.AreEqual(4, removed);
            Assert.AreEqual(1, stackable.AmountInStack);
        }

        /// <summary>
        /// IsFull should be true when amount equals max
        /// </summary>
        [Test]
        public void Stackable_IsFull_WhenAtMax()
        {
            Stackable stackable = CreateStackable(5, 5);

            Assert.True(stackable.IsFull);
        }

        /// <summary>
        /// IsFull should be false when amount is below max
        /// </summary>
        [Test]
        public void Stackable_IsNotFull_WhenBelowMax()
        {
            Stackable stackable = CreateStackable(10, 3);

            Assert.False(stackable.IsFull);
        }

        /// <summary>
        /// IsFull should become true after filling the stack completely
        /// </summary>
        [Test]
        public void Stackable_IsFull_AfterFillingToMax()
        {
            Stackable stackable = CreateStackable(10, 3);
            stackable.AddToStack(7);

            Assert.True(stackable.IsFull);
        }

        /// <summary>
        /// SetAmountInStack should clamp the value between 1 and maxStack
        /// </summary>
        [Test]
        public void Stackable_SetAmountInStack_ClampsCorrectly()
        {
            Stackable stackable = CreateStackable(10, 5);
            stackable.SetAmountInStack(0);

            Assert.AreEqual(1, stackable.AmountInStack);

            stackable.SetAmountInStack(20);
            Assert.AreEqual(10, stackable.AmountInStack);

            stackable.SetAmountInStack(7);
            Assert.AreEqual(7, stackable.AmountInStack);
        }

        /// <summary>
        /// Container should merge stackable items of the same type into a single entry
        /// </summary>
        [Test]
        public void Container_ShouldMergeStackableItems_OfSameType()
        {
            AttachedContainer container = CreateContainer(new Vector2Int(10, 10), null);
            Item item1 = CreateStackableItem(10, 1, "Coin");
            Item item2 = CreateStackableItem(10, 1, "Coin");

            container.AddItem(item1);
            int itemCountBefore = container.ItemCount;
            container.AddItem(item2);

            Assert.AreEqual(itemCountBefore, container.ItemCount, "Item count should not increase when stacking");
            Assert.AreEqual(2, item1.GetComponent<Stackable>().AmountInStack, "Stack should have increased to 2");
            Assert.False(container.Items.Contains(item2), "Stacked item should not be in container items");
        }

        /// <summary>
        /// Container should not merge non-stackable items even if they have the same name
        /// </summary>
        [Test]
        public void Container_ShouldNotStackItems_WhenNotStackable()
        {
            AttachedContainer container = CreateContainer(new Vector2Int(10, 10), null);
            Item item1 = CreateItem("Coin");
            Item item2 = CreateItem("Coin");

            container.AddItem(item1);
            int itemCountBefore = container.ItemCount;
            container.AddItem(item2);

            Assert.AreEqual(itemCountBefore + 1, container.ItemCount, "Non-stackable items should not stack");
        }

        /// <summary>
        /// Container should not merge into an existing stack that is already full
        /// </summary>
        [Test]
        public void Container_ShouldNotMerge_WhenExistingStackFull()
        {
            AttachedContainer container = CreateContainer(new Vector2Int(10, 10), null);
            Item item1 = CreateStackableItem(5, 5, "Coin");
            Item item2 = CreateStackableItem(5, 3, "Coin");

            container.AddItem(item1);
            int itemCountBefore = container.ItemCount;
            container.AddItem(item2);

            Assert.AreEqual(itemCountBefore + 1, container.ItemCount, "Item count should increase when existing stack is full");
            Assert.AreEqual(5, item1.GetComponent<Stackable>().AmountInStack, "Existing full stack should remain unchanged");
        }

        /// <summary>
        /// Container should partially stack when the existing stack doesn't have room for all items
        /// </summary>
        [Test]
        public void Container_ShouldPartialStack_WhenExistingNotFullyAvailable()
        {
            AttachedContainer container = CreateContainer(new Vector2Int(10, 10), null);
            Item item1 = CreateStackableItem(10, 7, "Coin");
            Item item2 = CreateStackableItem(10, 5, "Coin");

            container.AddItem(item1);
            container.AddItem(item2);

            Assert.AreEqual(2, container.ItemCount, "Should have 2 items: stacked main + remainder");
            Assert.AreEqual(10, item1.GetComponent<Stackable>().AmountInStack, "First stack should be full (10)");
            Assert.AreEqual(2, item2.GetComponent<Stackable>().AmountInStack, "Item2 should have 2 remaining");
        }

        /// <summary>
        /// RemoveFromStack should return 0 when requesting 0 items, leaving the stack unchanged.
        /// This documents the floor behavior: the stack can never go below 1.
        /// </summary>
        [Test]
        public void Stackable_RemoveFromStack_WhenRequestingZero_ReturnsZero()
        {
            Stackable stackable = CreateStackable(10, 5);
            int removed = stackable.RemoveFromStack(0);

            Assert.AreEqual(0, removed);
            Assert.AreEqual(5, stackable.AmountInStack, "Stack amount should remain unchanged");
        }

        /// <summary>
        /// After a fully consumed stack merge, the container should show one less item slot
        /// and the incoming item's AmountInStack should drop to its remaining value.
        /// </summary>
        [Test]
        public void Container_PartialStack_UpdatesIncomingItemAmount()
        {
            AttachedContainer container = CreateContainer(new Vector2Int(10, 10), null);
            Item item1 = CreateStackableItem(10, 8, "Coin");
            Item item2 = CreateStackableItem(10, 5, "Coin");

            container.AddItem(item1);
            container.AddItem(item2);

            Assert.AreEqual(2, container.ItemCount, "Should have remaining item as separate entry");
            Assert.AreEqual(10, item1.GetComponent<Stackable>().AmountInStack, "Existing stack should be full");
            Assert.AreEqual(3, item2.GetComponent<Stackable>().AmountInStack, "Incoming should have 3 remaining after stacking 2 into existing");
        }

        /// <summary>
        /// Container should only stack items of the same type, not different types
        /// </summary>
        [Test]
        public void Container_ShouldNotStack_WhenDifferentType()
        {
            AttachedContainer container = CreateContainer(new Vector2Int(10, 10), null);
            Item item1 = CreateStackableItem(10, 1, "Coin");
            Item item2 = CreateStackableItem(10, 1, "MetalRod");

            container.AddItem(item1);
            int itemCountBefore = container.ItemCount;
            container.AddItem(item2);

            Assert.AreEqual(itemCountBefore + 1, container.ItemCount, "Different stackable types should not stack");
        }

        /// <summary>
        /// Multiple stacks of the same type should each fill independently
        /// </summary>
        [Test]
        public void Container_ShouldFillMultipleStacks_OfSameType()
        {
            AttachedContainer container = CreateContainer(new Vector2Int(10, 10), null);
            Item item1 = CreateStackableItem(5, 5, "Coin");
            Item item2 = CreateStackableItem(5, 5, "Coin");
            Item item3 = CreateStackableItem(5, 3, "Coin");

            container.AddItem(item1);
            container.AddItem(item2);
            container.AddItem(item3);

            Assert.AreEqual(3, container.ItemCount, "All three should be separate since both are full");
            Assert.AreEqual(5, item1.GetComponent<Stackable>().AmountInStack);
            Assert.AreEqual(5, item2.GetComponent<Stackable>().AmountInStack);
            Assert.AreEqual(3, item3.GetComponent<Stackable>().AmountInStack);
        }

        #endregion

        #region Helper functions

        private static AttachedContainer CreateContainer(Vector2Int size, Filter filter)
        {
            GameObject go = new GameObject();
            AttachedContainer container = go.AddComponent<AttachedContainer>();
            container.Init(size, filter);
            return container;
        }

        private static Item CreateItem(string name = "TestItem", float weight = 1f)
        {
            GameObject go = new GameObject();
            Item item = go.AddComponent<Item>();
            item.Init(name, weight, new List<Trait>());
            return item;
        }

        private static Item CreateStackableItem(int maxStack, int amountInStack, string name = "StackableItem")
        {
            GameObject go = new GameObject();
            Item item = go.AddComponent<Item>();
            item.Init(name, 1f, new List<Trait>());
            Stackable stackable = go.AddComponent<Stackable>();
            stackable.Init(maxStack, amountInStack);
            return item;
        }

        private static Stackable CreateStackable(int maxStack, int amountInStack)
        {
            GameObject go = new GameObject();
            Item item = go.AddComponent<Item>();
            Stackable stackable = go.AddComponent<Stackable>();
            stackable.Init(maxStack, amountInStack);
            return stackable;
        }

        #endregion
    }
}
