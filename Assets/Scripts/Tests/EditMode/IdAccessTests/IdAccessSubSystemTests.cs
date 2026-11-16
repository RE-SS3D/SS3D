using NUnit.Framework;
using SS3D.Systems.IdAccess;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items.Generic;
using UnityEngine;

namespace EditorTests
{
    public class IdAccessSubSystemTests
    {
        [Test]
        public void CheckAccess_PassesWhenCredentialHasRequiredLevel()
        {
            IdAccessSubSystem subsystem = IdAccessTestFixtures.CreateRegisteredIdAccess(out GameObject subsystemObject);

            try
            {
                CrewRecord record = IdAccessTestFixtures.CreateEngineerRecord(subsystem);
                IDCard card = IdAccessTestFixtures.CreateBoundCard(record);

                AttachedContainer container = IdAccessTestFixtures.CreateContainer(ContainerType.Identification);
                container.AddItem(card);
                HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { container });

                AccessCheckResult result = subsystem.CheckAccess(
                    inventory,
                    AccessMask.FromLevels(AccessLevel.Engineering),
                    device: null);

                Assert.IsTrue(result.Passed);
                Assert.AreEqual(record.Id, result.CredentialRecordId);
                Assert.IsTrue(result.CredentialAccess.HasAll(AccessMask.FromLevels(AccessLevel.Engineering)));

                IdAccessTestFixtures.DestroyObjects(inventory.gameObject, container.gameObject, card.gameObject);
            }
            finally
            {
                IdAccessTestFixtures.DestroyRegistered(subsystem, subsystemObject);
            }
        }

        [Test]
        public void CheckAccess_FailsWhenCredentialLacksRequiredLevel()
        {
            IdAccessSubSystem subsystem = IdAccessTestFixtures.CreateRegisteredIdAccess(out GameObject subsystemObject);

            try
            {
                CrewRecord record = IdAccessTestFixtures.CreateCivilianRecord(subsystem);
                IDCard card = IdAccessTestFixtures.CreateBoundCard(record);

                AttachedContainer container = IdAccessTestFixtures.CreateContainer(ContainerType.Identification);
                container.AddItem(card);
                HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { container });

                AccessCheckResult result = subsystem.CheckAccess(
                    inventory,
                    AccessMask.FromLevels(AccessLevel.Engineering),
                    device: null);

                Assert.IsFalse(result.Passed);
                Assert.AreEqual(AccessCheckFailureReason.InsufficientAccess, result.FailureReason);

                IdAccessTestFixtures.DestroyObjects(inventory.gameObject, container.gameObject, card.gameObject);
            }
            finally
            {
                IdAccessTestFixtures.DestroyRegistered(subsystem, subsystemObject);
            }
        }

        [Test]
        public void CheckAccess_FailsWhenNoCredentialOnPerson()
        {
            IdAccessSubSystem subsystem = IdAccessTestFixtures.CreateRegisteredIdAccess(out GameObject subsystemObject);

            try
            {
                HumanInventory inventory = IdAccessTestFixtures.CreateInventory(System.Array.Empty<AttachedContainer>());

                AccessCheckResult result = subsystem.CheckAccess(
                    inventory,
                    AccessMask.FromLevels(AccessLevel.Engineering),
                    device: null);

                Assert.IsFalse(result.Passed);
                Assert.AreEqual(AccessCheckFailureReason.NoCredential, result.FailureReason);

                IdAccessTestFixtures.DestroyObjects(inventory.gameObject);
            }
            finally
            {
                IdAccessTestFixtures.DestroyRegistered(subsystem, subsystemObject);
            }
        }

        [Test]
        public void CheckAccess_PassesAfterRecordAccessIsUpdated()
        {
            IdAccessSubSystem subsystem = IdAccessTestFixtures.CreateRegisteredIdAccess(out GameObject subsystemObject);

            try
            {
                CrewRecord record = IdAccessTestFixtures.CreateCivilianRecord(subsystem);
                IDCard card = IdAccessTestFixtures.CreateBoundCard(record);

                AttachedContainer container = IdAccessTestFixtures.CreateContainer(ContainerType.Identification);
                container.AddItem(card);
                HumanInventory inventory = IdAccessTestFixtures.CreateInventory(new[] { container });

                AccessCheckResult denied = subsystem.CheckAccess(
                    inventory,
                    AccessMask.FromLevels(AccessLevel.Engineering),
                    device: null);
                Assert.IsFalse(denied.Passed);

                record.Access = AccessPresets.Engineer;

                AccessCheckResult granted = subsystem.CheckAccess(
                    inventory,
                    AccessMask.FromLevels(AccessLevel.Engineering),
                    device: null);
                Assert.IsTrue(granted.Passed);

                IdAccessTestFixtures.DestroyObjects(inventory.gameObject, container.gameObject, card.gameObject);
            }
            finally
            {
                IdAccessTestFixtures.DestroyRegistered(subsystem, subsystemObject);
            }
        }
    }
}
