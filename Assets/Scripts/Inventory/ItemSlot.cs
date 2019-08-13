using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : NetworkBehaviour, IDropHandler
{
    public UiItem uiItem;
    public SlotTypes slotType;
    public Transform physicalItemLocation;
    private HumanInventory humanInventory;

    public void OnDrop(PointerEventData eventData)
    {
        Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();
        if (draggable && !GetComponentInChildren<Draggable>())
        {
            UiItem uiItem = draggable.GetComponent<UiItem>();

            if (uiItem.Item.compatibleSlots.HasFlag(slotType))
            {
                RpcMoveItem(uiItem.Item.gameObject, gameObject);
            }
            else
            {
                return;
            }

            draggable.ReturnParent = transform;
        }
    }

    [Command]
    public void CmdMoveItem(GameObject itemObject, GameObject target)
    {
        RpcMoveItem(itemObject, target);
    }

    [ClientRpc]
    public void RpcMoveItem(GameObject itemObject, GameObject target)
    {
        Item item = itemObject.GetComponent<Item>();
        ItemSlot slot = target.GetComponent<ItemSlot>();

        item.MoveVisual(slot.gameObject);
    }

    public void UpdateVisualLocation(GameObject itemObject)
    {
        Item item = itemObject.GetComponent<Item>();
        if (physicalItemLocation)
        {
            CmdMoveItem(itemObject, gameObject);
        }
    }

    public void Clear()
    {
        uiItem = null;
    }
}