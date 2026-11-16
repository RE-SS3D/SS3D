using NUnit.Framework;
using SS3D.Systems.Inventory.Items;

namespace EditorTests
{
    /// <summary>
    /// D5 of 2026-11_construction-mvp1: consuming a stack spends whole units, deletes the item at zero, and a
    /// merge never exceeds the stack limit. <see cref="Item.TryConsume"/> and container merges delegate here.
    /// </summary>
    public class ItemStackConsumeTests
    {
        [Test]
        public void ConsumeToZero_DeletesTheItem()
        {
            Assert.IsTrue(ItemStackRules.TryResolveConsume(3, 3, out int remaining, out bool delete));
            Assert.AreEqual(0, remaining);
            Assert.IsTrue(delete);
        }

        [Test]
        public void ConsumePart_KeepsTheRest()
        {
            Assert.IsTrue(ItemStackRules.TryResolveConsume(20, 2, out int remaining, out bool delete));
            Assert.AreEqual(18, remaining);
            Assert.IsFalse(delete);
        }

        [Test]
        public void ConsumeMoreThanHeld_SpendsNothing()
        {
            Assert.IsFalse(ItemStackRules.TryResolveConsume(1, 2, out int remaining, out bool delete));
            Assert.AreEqual(1, remaining);
            Assert.IsFalse(delete);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ConsumeNonPositive_IsRefused(int amount)
        {
            Assert.IsFalse(ItemStackRules.TryResolveConsume(5, amount, out int remaining, out _));
            Assert.AreEqual(5, remaining);
        }

        [TestCase(30, 20, 50, true)]
        [TestCase(30, 21, 50, false)]
        [TestCase(1, 1, 1, false)]
        public void Merge_RespectsTheLimit(int existing, int incoming, int max, bool expected)
        {
            Assert.AreEqual(expected, ItemStackRules.CanMerge(existing, incoming, max));
        }

        [TestCase(0, 50, 1)]
        [TestCase(80, 50, 50)]
        [TestCase(20, 50, 20)]
        [TestCase(5, 1, 1)]
        public void StackCount_IsClampedToTheLimit(int count, int max, int expected)
        {
            Assert.AreEqual(expected, ItemStackRules.ClampCount(count, max));
        }
    }
}
