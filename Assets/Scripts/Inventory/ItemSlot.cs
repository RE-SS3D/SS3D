using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : NetworkBehaviour, IDropHandler
{
    public UiItem uiItem;
    public SlotTypes slotType;
    public Transform physicalItemLocation;
    private HumanInventoryUI humanInventoryUi;

    private void Awake()
    {
        humanInventoryUi = GetComponentInParent<HumanInventoryUI>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();
        if (draggable && !GetComponentInChildren<Draggable>())
        {
            UiItem uiItem = draggable.GetComponent<UiItem>();

            if (uiItem.Item.compatibleSlots.HasFlag(slotType))
            {
                humanInventoryUi.Inventory.CmdMoveItem(draggable.LastSlot.slotType, uiItem.Item.gameObject, slotType);
                draggable.LastSlot = this;
            }
            else
            {
                return;
            }

            draggable.ReturnParent = transform;
        }
    }
}