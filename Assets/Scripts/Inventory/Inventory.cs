using System.Linq;
using Mirror;
using UnityEngine;

public abstract class Inventory : NetworkBehaviour
{
    protected InventoryUi ui;

    [Command]
    public void CmdMoveItem(GameObject OriginSlotObject, SlotTypes origin, GameObject targetSlotObject,
        SlotTypes target,
        GameObject itemObject)
    {
        RpcMoveItem(OriginSlotObject, origin, targetSlotObject, target, itemObject);
    }

    [ClientRpc]
    public void RpcMoveItem(GameObject OriginSlotObject, SlotTypes origin, GameObject targetSlotObject,
        SlotTypes target,
        GameObject itemObject)
    {
//        ItemSlot originSlot = OriginSlotObject.GetComponent<ItemSlot>();
//        ItemSlot targetSlot = targetSlotObject.GetComponent<ItemSlot>();

        Item item = itemObject.GetComponent<Item>();
        ItemSlot originSlot = ui.GetSlots().First(s => s.slotType == origin);
        ItemSlot targetSlot = ui.GetSlots().First(s => s.slotType == target && s.uiItem == null);

        item.MoveVisual(targetSlot.gameObject);
        targetSlot.uiItem = originSlot.uiItem;
        originSlot.uiItem = null;
    }

    public void AddItem(Item item)
    {
    }
}