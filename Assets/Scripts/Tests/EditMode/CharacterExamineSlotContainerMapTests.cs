using NUnit.Framework;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Containers;

namespace EditorTests
{
    public class CharacterExamineSlotContainerMapTests
    {
        [TestCase(CharacterExamineSlot.Head, ContainerType.Head, ContainerType.None)]
        [TestCase(CharacterExamineSlot.Eyes, ContainerType.Glasses, ContainerType.None)]
        [TestCase(CharacterExamineSlot.Face, ContainerType.Mask, ContainerType.None)]
        [TestCase(CharacterExamineSlot.Ears, ContainerType.EarLeft, ContainerType.EarRight)]
        [TestCase(CharacterExamineSlot.Suit, ContainerType.ExoSuit, ContainerType.None)]
        [TestCase(CharacterExamineSlot.SuitStorage, ContainerType.SuitStorage, ContainerType.None)]
        [TestCase(CharacterExamineSlot.Shirt, ContainerType.Jumpsuit, ContainerType.None)]
        [TestCase(CharacterExamineSlot.Gloves, ContainerType.GloveLeft, ContainerType.GloveRight)]
        [TestCase(CharacterExamineSlot.Back, ContainerType.Bag, ContainerType.None)]
        [TestCase(CharacterExamineSlot.Feet, ContainerType.ShoeLeft, ContainerType.ShoeRight)]
        [TestCase(CharacterExamineSlot.Belt, ContainerType.Belt, ContainerType.None)]
        [TestCase(CharacterExamineSlot.IdCard, ContainerType.Identification, ContainerType.None)]
        [TestCase(CharacterExamineSlot.Pocket, ContainerType.Pocket, ContainerType.None)]
        public void TryGetContainerTypesReturnsExpectedMapping(CharacterExamineSlot slot, ContainerType expectedPrimary, ContainerType expectedSecondary)
        {
            bool found = CharacterExamineSlotContainerMap.TryGetContainerTypes(slot, out ContainerType primary, out ContainerType secondary);

            Assert.IsTrue(found);
            Assert.AreEqual(expectedPrimary, primary);
            Assert.AreEqual(expectedSecondary, secondary);
        }

        [TestCase(CharacterExamineSlot.HandLeft)]
        [TestCase(CharacterExamineSlot.HandRight)]
        public void TryGetContainerTypesReturnsFalseForHandSlots(CharacterExamineSlot slot)
        {
            bool found = CharacterExamineSlotContainerMap.TryGetContainerTypes(slot, out ContainerType primary, out ContainerType secondary);

            Assert.IsFalse(found);
            Assert.AreEqual(ContainerType.None, primary);
            Assert.AreEqual(ContainerType.None, secondary);
        }
    }
}
