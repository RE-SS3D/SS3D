using NUnit.Framework;
using SS3D.Systems.Examine;
using SS3D.Systems.Inventory.Containers;

namespace EditorTests
{
    public class CharacterLootUtilityTests
    {
        [Test]
        public void IsLootableReturnsFalseForNullInventory()
        {
            Assert.IsFalse(CharacterLootUtility.IsLootable(null));
        }

        [Test]
        public void IsOtherCharacterReturnsFalseWhenEitherNull()
        {
            Assert.IsFalse(CharacterLootUtility.IsOtherCharacter(null, null));
        }
    }

    public class CharacterExamineSlotResolveTests
    {
        [Test]
        public void TryGetItemInSlotReturnsFalseForNullInventory()
        {
            Assert.IsFalse(CharacterExamineContentBuilder.TryGetItemInSlot(
                null, CharacterExamineSlot.Belt, out _));
        }

        [Test]
        public void SlotContainerMapCoversEquipmentSlots()
        {
            Assert.IsTrue(CharacterExamineSlotContainerMap.TryGetContainerTypes(
                CharacterExamineSlot.Belt, out ContainerType primary, out ContainerType secondary));
            Assert.AreEqual(ContainerType.Belt, primary);
            Assert.AreEqual(ContainerType.None, secondary);
        }

        [Test]
        public void SlotContainerMapRejectsHandSlots()
        {
            Assert.IsFalse(CharacterExamineSlotContainerMap.TryGetContainerTypes(
                CharacterExamineSlot.HandLeft, out _, out _));
        }
    }
}
