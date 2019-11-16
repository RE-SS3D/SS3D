using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

/**
 * This renders a container of variable length in a generic fashion
 */
public class UIGeneralContainer : UIAbstractContainer
{
    [SerializeField]
    private GameObject slotPrefab;

    public bool Show { get; set; }
    public override bool Highlight { get; set; } // TODO:

    protected override void RenderContainers(GameObject owner, List<Container> newContainers)
    {
        // Just completely remakes the slot list, as thats easiest to code.
        // TODO: Probably add a few more efficiencies in the future
        slots.Clear();

        int slotStartIndex = 0;
        foreach(var container in newContainers)
        {
            for(int i = 0; i < containers.Count; ++i)
            {
                var itemSlotObject = Instantiate(slotPrefab, GetPositionFromIndex(slotStartIndex + i), new Quaternion(), transform);
                var slot = itemSlotObject.GetComponent<UIItemSlot>();

                slots.Add(new SlotInfo(container, i, slot));
                slot.Item = container.GetItem(i);
                slot.slotInteractor = this;
            }
        }
    }

    /**
     * Called when any single container updates
     */
    protected override void RenderContainer(Container container) {
        // Note: This assumes container size doesn't change

        int slotIndex = slots.FindIndex(slot => slot.container == container);

        foreach (var item in container.GetItems())
        {
            if(slots[slotIndex].container != container)
                Debug.LogError("UIGeneralContainer.UpdateContainer can not yet handle container size changing.");

            slots[slotIndex].uiSlot.Item = item;

            slotIndex++;
        }

        if(slots[slotIndex].container == container)
            Debug.LogError("UIGeneralContainer.UpdateContainer was not meant to handle container size changing.");
    }

    private Vector2 GetPositionFromIndex(int i)
    {
        return new Vector2((i % 4) * 50f, (i / 4) * 50f);
    }

    private bool show = false;
}
