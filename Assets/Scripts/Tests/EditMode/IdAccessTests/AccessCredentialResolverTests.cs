using NUnit.Framework;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items.Generic;
using UnityEngine;

namespace EditorTests
{
    public class AccessCredentialResolverTests
    {
        [Test]
        public void TryResolveBoundRecord_FindsDirectCardInIdentificationContainer()
        {
            IdAccessSubSystem subsystem = IdAccessTestFixtures.CreateRegisteredIdAccess(out GameObject subsystemObject);
            CrewRecord record = IdAccessTestFixtures.CreateEngineerRecord(subsystem);
            IDCard card = IdAccessTestFixtures.CreateBoundCard(record);

            AttachedContainer idContainer = IdAccessTestFixtures.CreateContainer(ContainerType.Identification);
            idContainer.AddItem(card);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { idContainer });

            try
            {
                bool resolved = AccessCredentialResolver.TryResolveBoundRecord(
                    inventory,
                    out CrewRecordId recordId,
                    out IDCard resolvedCard);

                Assert.IsTrue(resolved);
                Assert.AreEqual(record.Id, recordId);
                Assert.AreSame(card, resolvedCard);
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(
                    inventory.gameObject,
                    idContainer.gameObject,
                    card.gameObject);
                IdAccessTestFixtures.DestroyRegistered(subsystem, subsystemObject);
            }
        }

        [Test]
        public void TryResolveBoundRecord_FindsCardInHandContainer()
        {
            IdAccessSubSystem subsystem = IdAccessTestFixtures.CreateRegisteredIdAccess(out GameObject subsystemObject);
            CrewRecord record = IdAccessTestFixtures.CreateEngineerRecord(subsystem);
            IDCard card = IdAccessTestFixtures.CreateBoundCard(record);

            AttachedContainer handContainer = IdAccessTestFixtures.CreateContainer(ContainerType.Hand);
            handContainer.AddItem(card);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(
                System.Array.Empty<AttachedContainer>(),
                new[] { handContainer });

            try
            {
                bool resolved = AccessCredentialResolver.TryResolveBoundRecord(
                    inventory,
                    out CrewRecordId recordId,
                    out IDCard resolvedCard);

                Assert.IsTrue(resolved);
                Assert.AreEqual(record.Id, recordId);
                Assert.AreSame(card, resolvedCard);
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(
                    inventory.gameObject,
                    handContainer.gameObject,
                    card.gameObject);
                IdAccessTestFixtures.DestroyRegistered(subsystem, subsystemObject);
            }
        }

        [Test]
        public void TryResolveBoundRecord_FindsCardInsertedInPda()
        {
            IdAccessSubSystem subsystem = IdAccessTestFixtures.CreateRegisteredIdAccess(out GameObject subsystemObject);
            CrewRecord record = IdAccessTestFixtures.CreateEngineerRecord(subsystem);
            IDCard card = IdAccessTestFixtures.CreateBoundCard(record);
            PDA pda = IdAccessTestFixtures.CreatePdaWithInsertedCard(card);

            AttachedContainer pocket = IdAccessTestFixtures.CreateContainer(ContainerType.Pocket);
            pocket.AddItem(pda);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { pocket });

            try
            {
                bool resolved = AccessCredentialResolver.TryResolveBoundRecord(
                    inventory,
                    out CrewRecordId recordId,
                    out IDCard resolvedCard);

                Assert.IsTrue(resolved);
                Assert.AreEqual(record.Id, recordId);
                Assert.AreSame(card, resolvedCard);
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(
                    inventory.gameObject,
                    pocket.gameObject,
                    pda.gameObject,
                    card.gameObject);
                IdAccessTestFixtures.DestroyRegistered(subsystem, subsystemObject);
            }
        }

        [Test]
        public void TryResolveBoundRecord_SkipsUnboundCard()
        {
            GameObject cardObject = new("UnboundCard");
            IDCard card = cardObject.AddComponent<IDCard>();

            AttachedContainer container = IdAccessTestFixtures.CreateContainer(ContainerType.Identification);
            container.AddItem(card);
            HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { container });

            try
            {
                bool resolved = AccessCredentialResolver.TryResolveBoundRecord(
                    inventory,
                    out CrewRecordId recordId,
                    out IDCard resolvedCard);

                Assert.IsFalse(resolved);
                Assert.IsTrue(recordId.IsNone);
                Assert.IsNull(resolvedCard);
            }
            finally
            {
                IdAccessTestFixtures.DestroyObjects(inventory.gameObject, container.gameObject, cardObject);
            }
        }
    }
}
