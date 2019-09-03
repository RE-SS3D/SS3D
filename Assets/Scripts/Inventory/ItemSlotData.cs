using System;
using UnityEngine;

[Serializable]
public class ItemSlotData
{
    public UiItem uiItem;
    public SlotTypes slotType;
    public Transform physicalItemLocation;
    public InventoryUi inventoryUi;

    public ItemSlotData(UiItem uiItem, SlotTypes slotType, Transform physicalItemLocation, InventoryUi inventoryUi)
    {
        this.uiItem = uiItem;
        this.slotType = slotType;
        this.physicalItemLocation = physicalItemLocation;
        this.inventoryUi = inventoryUi;
    }
}