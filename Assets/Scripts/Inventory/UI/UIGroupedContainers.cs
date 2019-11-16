using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * A component for rendering multiple seperate containers together as one group.
 * Will complain if there aren't enough item slots for a given container type.
 */
public class UIGroupedContainers : UIAbstractContainer
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
    [SerializeField] private ItemSlot[]       uiSlots;

    // Runtime
    public override bool Highlight { get; set; }

    public void Start()
    {
        foreach(var uiSlot in uiSlots)
            uiSlot.slot.slotInteractor = this;
    }

    protected override void RenderContainers(GameObject newOwner, List<Container> newContainers)
    {
        slots.Clear();

        bool[] isSet = new bool[uiSlots.Length];

        foreach (var container in newContainers)
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
                    // TODO: Perform this warning only when actually useful
                    // Debug.LogWarning("Nowhere to display slottype " + slotType.ToString() + " from " + container.containerName + " from " + owner.name + " in Grouped Container " + name);
                    continue;
                }

                isSet[slotIndex] = true;

                slots.Add(new SlotInfo(container, i, uiSlots[slotIndex].slot));
                uiSlots[slotIndex].slot.Item = container.GetItem(i);
            }
        }

        // Disable all unused slots
        for (int i = 0; i < uiSlots.Length; ++i)
            uiSlots[i].slot.enabled = isSet[i];
    }

    protected override void RenderContainer(Container container)
    {
        // As we dont ever have to instantiate objects, it should be fast enough to just do a complete rerender
        RenderContainers(owner, containers);
    }

    // Used to store delegate used for performance reasons
    private readonly Container.ItemList.SyncListChanged OnContainerChange;
}
