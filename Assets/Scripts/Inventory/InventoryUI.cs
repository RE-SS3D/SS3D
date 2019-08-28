using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class InventoryUi : MonoBehaviour
{
    public Inventory Inventory;

    public abstract List<ItemSlot> GetSlots();
}