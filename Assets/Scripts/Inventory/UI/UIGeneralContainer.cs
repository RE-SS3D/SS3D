using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/**
 * This renders a container of variable length in a generic fashion
 */
public class UIGeneralContainer : UIInventory.ContainerRenderer
{
    struct SlotInfo
    {
        public SlotInfo(Container container, UIItemSlot uiSlot)
        {
            this.container = container;
            this.uiSlot = uiSlot;
        }

        public Container container;
        public UIItemSlot uiSlot;
    }

    [SerializeField]
    private GameObject slotPrefab;

    // Implement UIInventory rendering
    public override void UpdateContainers(GameObject owner, List<Container> newContainers)
    {
        // Note: This code effectively has the same effect of entirely recreating
        // the entire inventory object with the new containers.
        // It's just done this way to save creating/deleting objects.

        // Find and remove old containers
        foreach (var container in containers)
        {
            if (newContainers.Contains(container))
                continue;

            container.onChange -= items => UpdateContainer(container, items);

            // Remove slots from here
            var deleteList = slots.Where(slot => slot.container == container);
            foreach (var slot in deleteList)
                Destroy(slot.uiSlot);
            slots = slots.Except(deleteList).ToList();
        }

        // Find and add new containers
        int slotIndex = 0;
        foreach (var container in newContainers)
        {
            if(containers.Contains(container))
            {
                slotIndex += container.Length();
                continue;
            }
            container.onChange += items => UpdateContainer(container, items);

            // Add all container slots
            for (int i = 0; i < container.Length(); ++i)
            {
                GameObject itemTile = Instantiate(slotPrefab, new Vector3(), new Quaternion());
                itemTile.transform.SetParent(transform, false);

                slots.Insert(i, new SlotInfo(container, itemTile.GetComponent<UIItemSlot>()));

                if(container.GetItem(i))
                    slots[i].uiSlot.SetSprite(container.GetItem(i).sprite);
            }
        }

        // All slots have been reconfigured, but they haven't been positioned correctly
        for (int i = 0; i < slots.Count; ++i)
        {
            int x = (i % 4) + 1;
            int y = i / 4;

            // TODO: Should figure out actual itemslot size rather than using a fixed 50f
            slots[i].uiSlot.transform.localPosition = new Vector3(x * 50f, y * 50f);
        }
        
        this.owner = owner;
        containers = newContainers;
    }

    /**
     * Called when any single container updates
     */
    private void UpdateContainer(Container container, IReadOnlyList<Item> items) {
        // Note: This assumes container size doesn't change

        int slotIndex = slots.FindIndex(slot => slot.container == container);

        foreach (var item in items)
        {
            if(slots[slotIndex].container != container)
                Debug.LogError("UIGeneralContainer.UpdateContainer was not meant to handle container size changing.");

            if (item)
                slots[slotIndex].uiSlot.SetSprite(item.sprite);
            slotIndex++;
        }

        if(slots[slotIndex].container == container)
            Debug.LogError("UIGeneralContainer.UpdateContainer was not meant to handle container size changing.");
    }

    public void ToggleShow()
    {
        show = !show;
    }

    private bool show = false;
    private List<SlotInfo> slots;
}
