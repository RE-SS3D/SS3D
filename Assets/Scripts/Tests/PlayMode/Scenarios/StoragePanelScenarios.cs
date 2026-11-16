using System.Collections;
using System.Linq;
using NUnit.Framework;
using SS3D.Core;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Items;
using SS3D.UI.StoragePanel;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SS3D.Tests
{
    /// <summary>Storage panel behaviour against a live round: what the open panel shows as containers change.</summary>
    [Category(TestCategories.Scenarios)]
    [Category(TestCategories.Construction)]
    public class StoragePanelScenarios : ScenarioFixture
    {
        private const string FloorTileWood = "edfdfa7aba0ad7fcfbb7bc5d0acadc5a";

        // Playtest 2026-11-16: a tile moved onto a stack of 5 in an open backpack merged (6 on reopening), but the
        // open panel kept showing 5.
        [UnityTest]
        public IEnumerator MergingIntoAStack_UpdatesTheOpenPanelsCount()
        {
            EmptyHands(Body);
            HumanInventory inventory = Body.GetComponent<HumanInventory>();
            Hand hand = Body.GetComponent<Hands>().SelectedHand;
            Assert.IsTrue(inventory.TryGetTypeContainer(ContainerType.Bag, 0, out AttachedContainer back), "a back slot");
            Item backpack = back.Items.FirstOrDefault();
            Assert.IsTrue(ItemStorage.TryGetStorageOn(backpack, out AttachedContainer storage), "the loadout backpack");

            ItemSubSystem items = SubSystems.Get<ItemSubSystem>();
            Item stack = items.SpawnItemInContainer(FloorTileWood, storage);
            stack.SetStackCount(5);
            Item extra = items.SpawnItemInContainer(FloorTileWood, hand.Container);
            extra.SetStackCount(1);

            StoragePanelHost host = SubSystems.Get<StoragePanelHost>();
            host.RequestOpenNear(inventory.containerViewer, storage, new Vector2(300f, 300f));
            yield return WaitFor(() => host.IsOpen(storage), "the backpack panel opens");
            yield return WaitFor(() => ShownCount(storage, stack) == "×5", "the panel shows the stack of 5");

            inventory.ClientTransferItem(extra, storage.PositionOf(stack), storage);
            yield return WaitFor(() => stack.StackCount == 6, "the tile merges into the stack");
            yield return WaitFor(() => ShownCount(storage, stack) == "×6", $"the open panel shows 6 (shows {ShownCount(storage, stack)})");

            host.RequestClose(inventory.containerViewer, storage);
            stack.Delete();
        }

        private static string ShownCount(AttachedContainer storage, Item stack)
        {
            StorageSlot slot = (storage.OpenPanel as VisualElement)?.Query<StorageSlot>().ToList().FirstOrDefault(s => s.BoundItem == stack);
            return slot?.Q<Label>(className: "storage-slot__stack-count")?.text;
        }
    }
}
