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
    public class HostStackingActions : PlayModeTest
    {
        [UnitySetUp]
        public IEnumerator UnitySetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return LoadAndSetInGame(NetworkType.Host, 8f);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            LogAssert.ignoreFailingMessages = true;
            yield return TestHelpers.FinishAndExitRound();
        }

        [UnityTest]
        public IEnumerator HostCanStackSteelSheetsInsideToolbox()
        {
            HumanInventory inventory = null;
            yield return WaitUntilLocalInventoryReady(readyInventory => inventory = readyInventory);
            Assert.That(inventory.Hands.HandContainers.Count, Is.GreaterThanOrEqualTo(2));
            ClearHandContainers(inventory);

            AttachedContainer toolboxHand = inventory.Hands.HandContainers[0];
            AttachedContainer sourceHand = inventory.Hands.HandContainers[1];
            InteractionController interactionController = inventory.GetComponent<InteractionController>();

            ItemSubSystem itemSystem = SubSystems.Get<ItemSubSystem>();
            itemSystem.SpawnItemInContainer(Items.ToolboxBlue, toolboxHand);
            itemSystem.SpawnItemInContainer(Items.SteelSheet, sourceHand);

            Item toolboxItem = null;
            yield return WaitUntilContainerHasItem(
                toolboxHand,
                item => item.GetComponentsInChildren<AttachedContainer>(true)
                    .Any(container => container != toolboxHand && container.Size.x > 1),
                item => toolboxItem = item,
                "Toolbox did not appear.");
            yield return WaitUntilContainerHasItem(
                sourceHand,
                item => item.TryGetStackable(out _),
                _ => { },
                "Source steel sheet did not appear.");

            AttachedContainer toolboxContainer = null;
            yield return WaitUntilToolboxContainerAvailable(toolboxItem, toolboxHand, container => toolboxContainer = container);
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

            itemSystem.SpawnItemInContainer(Items.SteelSheet, sourceHand);
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

        private static IEnumerator WaitUntilLocalInventoryReady(System.Action<HumanInventory> setInventory, float timeout = 15f)
        {
            EntitySubSystem entitySystem = SubSystems.Get<EntitySubSystem>();
            float startTime = Time.time;
            HumanInventory inventory = null;
            while (inventory == null && Time.time < startTime + timeout)
            {
                Entity entity = null;
                if (InstanceFinder.IsHost && entitySystem.LastSpawned != null)
                {
                    entity = entitySystem.LastSpawned;
                }
                else
                {
                    entitySystem.TryGetOwnedEntity(InstanceFinder.ClientManager.Connection, out entity);
                }

                inventory = entity != null ? entity.GetComponent<HumanInventory>() : null;
                if (inventory != null && inventory.Hands.HandContainers.Count >= 2)
                {
                    break;
                }

                inventory = null;
                yield return null;
            }

            Assert.That(inventory, Is.Not.Null, "Local host inventory did not become ready.");
            setInventory(inventory);
        }

        private static void ClearHandContainers(HumanInventory inventory)
        {
            foreach (AttachedContainer handContainer in inventory.Hands.HandContainers)
            {
                foreach (Item item in handContainer.Items.ToList())
                {
                    handContainer.RemoveItem(item);
                }
            }
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

        private static IEnumerator WaitUntilToolboxContainerAvailable(
            Item toolboxItem,
            AttachedContainer handContainer,
            System.Action<AttachedContainer> setContainer,
            float timeout = 5f)
        {
            float startTime = Time.time;
            AttachedContainer toolboxContainer = null;
            while (toolboxContainer == null && Time.time < startTime + timeout)
            {
                toolboxContainer = toolboxItem.GetComponentsInChildren<AttachedContainer>(true)
                    .FirstOrDefault(container => container != handContainer && container.Size.x > 1);
                yield return null;
            }

            Assert.That(toolboxContainer, Is.Not.Null, "Toolbox container did not appear.");
            setContainer(toolboxContainer);
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

        private static IEnumerator WaitUntilContainerHasItem(
            AttachedContainer container,
            System.Func<Item, bool> predicate,
            System.Action<Item> setItem,
            string failureMessage,
            float timeout = 5f)
        {
            float startTime = Time.time;
            Item item = null;
            while (item == null && Time.time < startTime + timeout)
            {
                item = container.Items.FirstOrDefault(predicate);
                yield return null;
            }

            Assert.That(item, Is.Not.Null, failureMessage);
            setItem(item);
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
