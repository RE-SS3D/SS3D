using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A component for rendering multiple seperate containers together as one group.
 * Will complain if there aren't enough item slots for a given container type.
 */
public class UIGroupedContainers : UIInventory.ContainerRenderer
{
    // Matches a container type (expected to hold 1 item) to a slot
    [Serializable]
    public struct ItemSlot
    {
        // Type of slot this UI view connects to
        public Container.SlotType type;
        // The UI view to display in
        public UIItemSlot slot;
    }

    // Editor properties
    [SerializeField] private Container.Type   containerType; // TODO:
    [SerializeField] private ItemSlot[]       uiSlots;

    public UIGroupedContainers()
    {
        OnContainerChange = () => UpdateContainer();
    }

    public override void UpdateContainers(GameObject owner, List<Container> containers)
    {
        bool[] isSet = new bool[uiSlots.Length];

        foreach (var container in containers)
        {
            for (int i = 0; i < container.Length(); ++i)
            {
                var slotType = container.GetSlot(i);

                // Get the first available slot to place the item info in
                int slotIndex = Array.FindIndex(uiSlots, slot => slot.type == slotType);
                while (slotIndex != -1 && isSet[slotIndex])
                    slotIndex = Array.FindIndex(uiSlots, slotIndex + 1, slot => slot.type == slotType);

                if (slotIndex == -1)
                {
                    // Debug.LogWarning("Nowhere to display slottype " + slotType.ToString() + " from " + container.containerName + " from " + owner.name + " in Grouped Container " + name);
                    continue;
                }

                isSet[slotIndex] = true;
                if (container.GetItem(i))
                    uiSlots[slotIndex].slot.SetSprite(container.GetItem(i).sprite);
                else
                    uiSlots[slotIndex].slot.SetSprite(null);
            }
        }

        // Disable all unused slots
        for(int i = 0; i < uiSlots.Length; ++i)
        {
            if(isSet[i])
                uiSlots[i].slot.SetActive(true);
            else
                uiSlots[i].slot.SetActive(false);
        }

        // Swap over all event subscriptions
        foreach (var container in this.containers)
            if (!containers.Contains(container))
                container.onChange -= OnContainerChange;
        foreach (var container in containers)
            if (!this.containers.Contains(container))
                container.onChange += OnContainerChange;

        this.owner = owner;
        this.containers = containers;
    }

    private void UpdateContainer()
    {
        // Can't be bothered doing a specific update here.
        UpdateContainers(owner, containers);
    }
    // Used to store delegate used for performance reasons
    private readonly Container.OnChange OnContainerChange;
}
