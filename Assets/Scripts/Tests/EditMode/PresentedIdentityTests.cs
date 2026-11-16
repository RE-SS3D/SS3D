using NUnit.Framework;
using SS3D.Systems.Examine;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items.Generic;
using UnityEngine;

namespace EditorTests
{
    /// <summary>
    /// The presented identity (examine.md §5): a card in the ID slot, bare or in a worn PDA, or a card
    /// in a hand. Pocketed cards and held PDAs do not count.
    /// </summary>
    public class PresentedIdentityTests
    {
        private static IDCard CreateCard(string name = "Ana Ruiz")
        {
            IDCard card = new GameObject("IDCard").AddComponent<IDCard>();
            card.ServerBind(new CrewRecord
            {
                Id = new CrewRecordId(7),
                Name = name,
                JobName = "Engineer",
                Department = Department.Engineering,
            });
            return card;
        }

        [Test]
        public void BareCardInIdSlotPresents()
        {
            IDCard card = CreateCard();
            AttachedContainer idSlot = IdAccessTestFixtures.CreateContainer(ContainerType.Identification);
            idSlot.AddItem(card);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { idSlot });
            try
            {
                Assert.IsTrue(PresentedIdentity.TryResolve(inventory, out PresentedIdentity identity));
                Assert.AreEqual("Ana Ruiz", identity.Name);
                Assert.AreEqual("Engineer", identity.Job);
                Assert.AreEqual("<k=dept:engineering>Engineer</k>", identity.TaggedJob);
                Assert.AreEqual("Ana Ruiz", PresentedIdentity.DisplayName(inventory));
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(inventory.gameObject, idSlot.gameObject, card.gameObject);
            }
        }

        [Test]
        public void CardInsidePdaInIdSlotPresents()
        {
            IDCard card = CreateCard();
            PDA pda = IdAccessTestFixtures.CreatePdaWithInsertedCard(card);
            AttachedContainer idSlot = IdAccessTestFixtures.CreateContainer(ContainerType.Identification);
            idSlot.AddItem(pda);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { idSlot });
            try
            {
                Assert.IsTrue(PresentedIdentity.TryResolve(inventory, out PresentedIdentity identity));
                Assert.AreEqual("Ana Ruiz", identity.Name);
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(inventory.gameObject, idSlot.gameObject, pda.gameObject, card.gameObject);
            }
        }

        /// <summary>Clients never run the server-side insert, so the PDA must find its own container.</summary>
        [Test]
        public void PdaFindsItsCardWithoutServerInsert()
        {
            IDCard card = CreateCard();
            GameObject pdaObject = new("PDA");
            PDA pda = pdaObject.AddComponent<PDA>();
            AttachedContainer pdaContainer = pdaObject.AddComponent<AttachedContainer>();
            pdaContainer.Init(new Vector2Int(1, 1), null);
            pdaContainer.AddItem(card);
            try
            {
                Assert.AreSame(card, pda.GetInsertedIdCard());
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(pdaObject, card.gameObject);
            }
        }

        [Test]
        public void CardInPocketDoesNotPresent()
        {
            IDCard card = CreateCard();
            AttachedContainer pocket = IdAccessTestFixtures.CreateContainer(ContainerType.Pocket);
            pocket.AddItem(card);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { pocket });
            try
            {
                Assert.IsFalse(PresentedIdentity.TryResolve(inventory, out _));
                Assert.AreNotEqual("Ana Ruiz", PresentedIdentity.DisplayName(inventory));
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(inventory.gameObject, pocket.gameObject, card.gameObject);
            }
        }

        [Test]
        public void CardHeldInHandPresentsButHeldPdaDoesNot()
        {
            IDCard heldCard = CreateCard("Held Card");
            IDCard pdaCard = CreateCard("Pda Card");
            PDA pda = IdAccessTestFixtures.CreatePdaWithInsertedCard(pdaCard);

            AttachedContainer leftHand = IdAccessTestFixtures.CreateContainer(ContainerType.Hand);
            AttachedContainer rightHand = IdAccessTestFixtures.CreateContainer(ContainerType.Hand);
            rightHand.AddItem(pda);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(
                System.Array.Empty<AttachedContainer>(), new[] { leftHand, rightHand });
            try
            {
                Assert.IsFalse(PresentedIdentity.TryResolve(inventory, out _), "a held PDA does not present its card");

                leftHand.AddItem(heldCard);
                Assert.IsTrue(PresentedIdentity.TryResolve(inventory, out PresentedIdentity identity));
                Assert.AreEqual("Held Card", identity.Name);
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(
                    inventory.gameObject, leftHand.gameObject, rightHand.gameObject,
                    pda.gameObject, pdaCard.gameObject, heldCard.gameObject);
            }
        }

        [Test]
        public void NoIdFallsBackToShortLook()
        {
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(System.Array.Empty<AttachedContainer>());
            try
            {
                Assert.IsFalse(PresentedIdentity.TryResolve(inventory, out _));
                Assert.IsFalse(string.IsNullOrWhiteSpace(PresentedIdentity.DisplayName(inventory)));
                Assert.IsFalse(PresentedIdentity.TryResolve(null, out _));
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(inventory.gameObject);
            }
        }

        [Test]
        public void SelfPresentingLineNamesTheCard()
        {
            string line = ExamineKeywordFormatter.StripTags(
                CharacterExamineDetail.PresentingLine(true, new PresentedIdentity("Marcus Voss", "Engineer", Department.Engineering)));
            Assert.AreEqual("You are presenting: Marcus Voss, Engineer.", line);
            Assert.AreEqual(
                "You are presenting no ID.",
                CharacterExamineDetail.PresentingLine(false, default));
        }
    }
}
