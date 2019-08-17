using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlot : NetworkBehaviour, IDropHandler
{
    public UiItem uiItem;
    public SlotTypes slotType;

    public Transform physicalItemLocation;

    private HumanInventoryUI humanInventoryUi;
    public InventoryUi inventoryUi;

    private void Start()
    {
        if (!inventoryUi) inventoryUi = GetComponentInParent<InventoryUi>();
//        humanInventoryUi = GetComponentInParent<HumanInventoryUI>();
    }

    public void Initialize(ItemSlotData data, InventoryUi ui)
    {
        inventoryUi = ui;
//        uiItem = data.uiItem;
//        slotType = data.slotType;
//        physicalItemLocation = data.physicalItemLocation;
//        humanInventoryUi = data.inventoryUi;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Draggable draggable = eventData.pointerDrag.GetComponent<Draggable>();
        if (draggable && !GetComponentInChildren<Draggable>())
        {
            UiItem uiItem = draggable.GetComponent<UiItem>();

            if (uiItem.Item.compatibleSlots.HasFlag(slotType))
            {
                inventoryUi.Inventory.CmdMoveItem(
                    draggable.LastSlot.gameObject, draggable.LastSlot.slotType,
                    gameObject, slotType,
                    uiItem.Item.gameObject);
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