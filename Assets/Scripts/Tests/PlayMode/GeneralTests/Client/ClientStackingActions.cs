using System.Collections;
using System.Linq;
using FishNet;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Data.Generated;
using SS3D.Networking;
using SS3D.Systems.Entities;
using SS3D.Systems.Interactions;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.TestTools;

namespace SS3D.Tests
{
    public class ClientStackingActions : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndSetInGame(NetworkType.Client);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return TestHelpers.FinishAndExitRound();
            KillAllBuiltExecutables();
        }

        [UnityTest]
        public IEnumerator ClientCanStackSteelSheetsInsideToolbox()
        {
            HumanInventory inventory = GetLocalInventory();
            Assert.That(inventory.Hands.HandContainers.Count, Is.GreaterThanOrEqualTo(2));

            AttachedContainer toolboxHand = inventory.Hands.HandContainers[0];
            AttachedContainer sourceHand = inventory.Hands.HandContainers[1];
            InteractionController interactionController = inventory.GetComponent<InteractionController>();

            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            itemSystem.CmdSpawnItemInContainerById(Items.ToolboxBlue, toolboxHand);
            itemSystem.CmdSpawnItemInContainerById(Items.SteelSheet, sourceHand);

            yield return WaitUntilContainerItemCount(toolboxHand, 1, "Toolbox did not appear.");
            yield return WaitUntilContainerItemCount(sourceHand, 1, "Source steel sheet did not appear.");

            Item toolboxItem = toolboxHand.Items.First();
            AttachedContainer toolboxContainer = toolboxItem.GetComponentsInChildren<AttachedContainer>(true)
                .First(container => container != toolboxHand && container.Size.x > 1);
            Hand sourceHandObject = inventory.Hands.PlayerHands.First(hand => hand.Container == sourceHand);

            inventory.Hands.CmdSetActiveHand(sourceHand);
            yield return WaitUntilSelectedHand(inventory.Hands, sourceHand);
            interactionController.InteractInHand(toolboxContainer.ContainerInteractive.gameObject, sourceHandObject.gameObject);
            yield return new WaitForSeconds(0.5f);
            inventory.ClientInteractWithContainerSlot(toolboxContainer, Vector2Int.zero);

            yield return WaitUntilContainerItemCount(sourceHand, 0, "Source hand still held the first steel sheet.");
            yield return WaitUntilContainerItemCount(toolboxContainer, 1, "First steel sheet did not move into toolbox.");

            Item destinationItem = toolboxContainer.Items.First();
            Assert.That(destinationItem.TryGetStackable(out Stackable destinationStack), Is.True);

            itemSystem.CmdSpawnItemInContainerById(Items.SteelSheet, sourceHand);
            yield return WaitUntilContainerItemCount(sourceHand, 1, "Second source steel sheet did not appear.");

            Item secondSourceItem = sourceHand.Items.First();
            Assert.That(secondSourceItem.TryGetStackable(out _), Is.True);
            inventory.ClientInteractWithContainerSlot(toolboxContainer, Vector2Int.zero);

            yield return WaitUntilContainerItemCount(sourceHand, 0, "Source hand still held the transferred second steel sheet.");
            yield return WaitUntilStackAmount(destinationStack, 2, "Destination steel sheet stack did not reach amount 2.");
        }

        protected override bool UseMockUpInputs()
        {
            return true;
        }

        private static HumanInventory GetLocalInventory()
        {
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            entitySystem.TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out Entity entity);
            return entity.GetComponent<HumanInventory>();
        }

        private static IEnumerator WaitUntilSelectedHand(Hands hands, AttachedContainer expectedContainer, float timeout = 5f)
        {
            float startTime = Time.time;
            while (hands.SelectedHand.Container != expectedContainer && Time.time < startTime + timeout)
            {
                yield return null;
            }

            Assert.That(hands.SelectedHand.Container, Is.EqualTo(expectedContainer));
        }

        private static IEnumerator WaitUntilContainerItemCount(AttachedContainer container, int expectedCount, string failureMessage, float timeout = 5f)
        {
            float startTime = Time.time;
            while (container.ItemCount != expectedCount && Time.time < startTime + timeout)
            {
                yield return null;
            }

            Assert.That(container.ItemCount, Is.EqualTo(expectedCount), failureMessage);
        }

        private static IEnumerator WaitUntilStackAmount(Stackable stackable, int expectedAmount, string failureMessage, float timeout = 5f)
        {
            float startTime = Time.time;
            while (stackable.Amount != expectedAmount && Time.time < startTime + timeout)
            {
                yield return null;
            }

            Assert.That(stackable.Amount, Is.EqualTo(expectedAmount), failureMessage);
        }

    }
}
