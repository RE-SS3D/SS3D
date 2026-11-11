using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SS3D.Systems.Examine;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// Search shows only what's visible (examine.md §5): a conscious character's pockets read occupied
    /// or empty; a blind reach is slower than an open take (inventory-storage.md §9).
    /// </summary>
    public class SearchVisibilityTests
    {
        [Test]
        public void PocketReadsOccupiedOnlyUntilRevealed()
        {
            Item coin = new GameObject("Coin").AddComponent<Item>();
            coin.Init("coin", 0.1f, new List<SS3D.Systems.Trait>());
            AttachedContainer pocket = IdAccessTestFixtures.CreateContainer(ContainerType.Pocket);
            pocket.AddItem(coin);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { pocket });
            try
            {
                CharacterExamineSlotContent alert = PocketSlot(CharacterExamineContentBuilder.BuildSlots(inventory, revealHidden: false));
                Assert.IsTrue(alert.IsHidden, "a conscious target's pocket is hidden");
                Assert.IsTrue(alert.IsOccupied, "but reads as occupied");
                Assert.IsNull(alert.ItemName);

                CharacterExamineSlotContent revealed = PocketSlot(CharacterExamineContentBuilder.BuildSlots(inventory, revealHidden: true));
                Assert.IsFalse(revealed.IsHidden);
                Assert.AreEqual("coin", revealed.ItemName);
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(inventory.gameObject, pocket.gameObject, coin.gameObject);
            }
        }

        [Test]
        public void EmptyPocketStaysEmptyWhenHidden()
        {
            AttachedContainer pocket = IdAccessTestFixtures.CreateContainer(ContainerType.Pocket);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { pocket });
            try
            {
                CharacterExamineSlotContent slot = PocketSlot(CharacterExamineContentBuilder.BuildSlots(inventory, revealHidden: false));
                Assert.IsFalse(slot.IsHidden);
                Assert.IsFalse(slot.IsOccupied);
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(inventory.gameObject, pocket.gameObject);
            }
        }

        [Test]
        public void OnlyPocketsAndSuitStorageHide()
        {
            Assert.IsTrue(CharacterExamineContentBuilder.IsHiddenWhileAlert(CharacterExamineSlot.Pocket));
            Assert.IsTrue(CharacterExamineContentBuilder.IsHiddenWhileAlert(CharacterExamineSlot.SuitStorage));
            Assert.IsFalse(CharacterExamineContentBuilder.IsHiddenWhileAlert(CharacterExamineSlot.Belt));
            Assert.IsFalse(CharacterExamineContentBuilder.IsHiddenWhileAlert(CharacterExamineSlot.HandLeft));
        }

        [Test]
        public void BlindTakeIsSlowerThanOpenTakeIsSlowerThanLooting()
        {
            float looting = CharacterLootUtility.TakeSeconds(victimIncapacitated: true, blind: false);
            float open = CharacterLootUtility.TakeSeconds(victimIncapacitated: false, blind: false);
            float blind = CharacterLootUtility.TakeSeconds(victimIncapacitated: false, blind: true);
            Assert.Less(looting, open);
            Assert.Less(open, blind);
            Assert.AreEqual(looting, CharacterLootUtility.TakeSeconds(victimIncapacitated: true, blind: true),
                "an incapacitated body has nothing hidden");
        }

        [Test]
        public void WarningNamesTheItemOnlyForAnOpenTake()
        {
            Assert.AreEqual("Someone is reaching into your pockets!", TakeFromCharacterWarning.Text(blind: true, null));
            Item wrench = new GameObject("Wrench").AddComponent<Item>();
            wrench.Init("Wrench", 1f, new List<SS3D.Systems.Trait>());
            try
            {
                Assert.AreEqual("Someone is trying to take your wrench!", TakeFromCharacterWarning.Text(blind: false, wrench));
            }
            finally
            {
                Object.DestroyImmediate(wrench.gameObject);
            }
        }

        private static CharacterExamineSlotContent PocketSlot(IReadOnlyList<CharacterExamineSlotContent> slots) =>
            slots.Single(s => s.Slot == CharacterExamineSlot.Pocket);
    }
}
