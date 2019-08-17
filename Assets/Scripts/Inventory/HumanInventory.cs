using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class HumanInventory : Inventory
{
    private Hands activeHand = Hands.Left;

    [SerializeField]
    private HumanInventoryUI humanInventoryUiPrefab;

    private HumanInventoryUI inventoryUI;

    [SerializeField]
    private HumanAttachmentPoints attachmentPoints;

    private void Start()
    {
        inventoryUI = Instantiate(humanInventoryUiPrefab);
        inventoryUI.Initialize(this, attachmentPoints);

        ui = inventoryUI;

        if (!isLocalPlayer)
        {
            Destroy(inventoryUI.GetComponent<CanvasScaler>());
            Destroy(inventoryUI.GetComponent<GraphicRaycaster>());
            Destroy(inventoryUI.GetComponent<Canvas>());
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        if (Input.GetButtonDown("SwapActive"))
        {
            activeHand = activeHand == Hands.Left ? Hands.Right : Hands.Left;
            inventoryUI.SwitchActiveHand(activeHand);
        }

        if (Input.GetButtonDown("DropActive"))
        {
            RemoveItem(GetActiveHandSlot());
        }

        if (Input.GetButtonDown("Click"))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100))
            {
                Item item = hit.collider.GetComponent<Item>();
                if (item != null) AddItem(item);
            }
        }
    }

    public ItemSlot GetActiveHandSlot()
    {
        return activeHand == Hands.Left
            ? inventoryUI.GetSlots().Find(s => s.slotType == SlotTypes.LeftHand)
            : inventoryUI.GetSlots().Find(s => s.slotType == SlotTypes.RightHand);
    }

//    [Command]
//    public void CmdMoveItem(SlotTypes origin, GameObject itemObject, SlotTypes type)
//    {
//        RpcMoveItem(origin, itemObject, type);
//    }

//    [ClientRpc]
//    public void RpcMoveItem(SlotTypes origin, GameObject itemObject, SlotTypes target)
//    {
//        Item item = itemObject.GetComponent<Item>();
//        ItemSlot targetSlot = inventoryUI.GetSlots().SlotsArray.First(s => s.slotType == target);
//        ItemSlot originSlot = inventoryUI.GetSlots().SlotsArray.First(s => s.slotType == origin);
//
//        item.MoveVisual(targetSlot.gameObject);
//        targetSlot.uiItem = originSlot.uiItem;
//        originSlot.uiItem = null;
//    }

    [Command]
    private void CmdGrab(GameObject itemObject, Hands hand)
    {
        RpcShareGrab(itemObject, gameObject, hand);
    }

    [ClientRpc]
    private void RpcShareGrab(GameObject itemObject, GameObject owner, Hands hand)
    {
        Item item = itemObject.GetComponent<Item>();

        ItemSlot selectedHand = hand == Hands.Left
            ? owner.GetComponent<HumanInventory>().inventoryUI.GetSlots().Find(s => s.slotType == SlotTypes.LeftHand)
            : owner.GetComponent<HumanInventory>().inventoryUI.GetSlots().Find(s => s.slotType == SlotTypes.RightHand);

        item.CreateVisual(selectedHand.physicalItemLocation);
        item.MoveVisual(selectedHand.gameObject);
        item.HideOriginal();
    }

    private void AddItem(Item item)
    {
        if (!item.Held && item.compatibleSlots.HasFlag(SlotTypes.LeftHand | SlotTypes.RightHand))
        {
            ItemSlot slot = GetActiveHandSlot();
            if (!slot.uiItem)
            {
                CmdGrab(item.gameObject, activeHand);
            }

            if (activeHand == Hands.Left)
            {
                if (!inventoryUI.GetSlots().Find(s => s.slotType == SlotTypes.LeftHand).uiItem)
                {
                    CmdGrab(item.gameObject, activeHand);
                    inventoryUI.SetSlotUiItem(slot, item);
                }
            }
            else
            {
                if (!inventoryUI.GetSlots().Find(s => s.slotType == SlotTypes.RightHand).uiItem)
                    CmdGrab(item.gameObject, activeHand);
                inventoryUI.SetSlotUiItem(inventoryUI.GetSlots().Find(s => s.slotType == SlotTypes.RightHand), item);
            }
        }
    }

    public void RemoveItem(ItemSlot slot)
    {
        if (slot.uiItem)
        {
            CmdRemoveItem(slot.uiItem.Item.gameObject);
            inventoryUI.ClearSlotUiItem(slot);
        }
    }

    [Command]
    private void CmdRemoveItem(GameObject itemObject)
    {
        Item item = itemObject.GetComponent<Item>();
        item.RpcDrop();
    }
}