using System.Collections.Generic;
using NUnit.Framework;
using SS3D.Systems.Examine;

namespace EditorTests
{
    public class CharacterExamineContentBuilderTests
    {
        [Test]
        public void BuildSlotsReturnsAllFourteenSlotsEmptyForNullInventory()
        {
            IReadOnlyList<CharacterExamineSlotContent> slots = CharacterExamineContentBuilder.BuildSlots(null);

            Assert.AreEqual(15, slots.Count);
            foreach (CharacterExamineSlotContent content in slots)
            {
                Assert.IsNull(content.ItemIcon);
                Assert.IsNull(content.ItemName);
            }
        }

        [Test]
        public void BuildSlotsIncludesBothHandSlots()
        {
            IReadOnlyList<CharacterExamineSlotContent> slots = CharacterExamineContentBuilder.BuildSlots(null);

            bool hasLeft = false;
            bool hasRight = false;
            foreach (CharacterExamineSlotContent content in slots)
            {
                hasLeft |= content.Slot == CharacterExamineSlot.HandLeft;
                hasRight |= content.Slot == CharacterExamineSlot.HandRight;
            }

            Assert.IsTrue(hasLeft);
            Assert.IsTrue(hasRight);
        }

        [Test]
        public void TryGetVisibleIdentityReturnsFalseForNullInventory()
        {
            bool found = CharacterExamineContentBuilder.TryGetVisibleIdentity(null, out string name, out string job);

            Assert.IsFalse(found);
            Assert.IsNull(name);
            Assert.IsNull(job);
        }
    }
}
