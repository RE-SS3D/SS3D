using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IDropHandler
{
    public UiItem uiItem;

    public SlotTypes slotType;

    public Transform physicalItemLocation;

    public void OnDrop(PointerEventData eventData)
    {
        Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();
        if (draggable && !GetComponentInChildren<Draggable>())
        {
            UiItem item = draggable.GetComponent<UiItem>();

            if (item.Item.compatibleSlots.HasFlag(slotType))
            {
                PutItemInSlot(item);
            }
            else
            {
                return;
            }

            draggable.ReturnParent = transform;
        }
    }

    public void PutItemInSlot(UiItem item)
    {
        if (physicalItemLocation)
        {
            item.Item.Retrieve();
            item.Item.Hold(physicalItemLocation);
        }
        else
        {
            item.Item.Store();
        }
    }

    public void Clear()
    {
        uiItem = null;
    }
}