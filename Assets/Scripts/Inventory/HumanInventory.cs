using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class HumanInventory : NetworkBehaviour
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

        if (!isLocalPlayer)
        {
            Destroy(inventoryUI.GetComponent<CanvasScaler>());
            Destroy(inventoryUI.GetComponent<GraphicRaycaster>());
            Destroy(inventoryUI.GetComponent<Canvas>());
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("SwapActive"))
        {
            activeHand = activeHand == Hands.Left ? Hands.Right : Hands.Left;
            inventoryUI.SwitchActiveHand(activeHand);
        }

        if (Input.GetButtonDown("DropActive"))
        {
            DropItem();

        }

        if (Input.GetButtonDown("Click"))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100))
            {
                Item item = hit.collider.GetComponent<Item>();
                if (item != null) PickUp(item);
            }
        }
    }

    public ItemSlot GetActiveHandSlot()
    {
        return activeHand == Hands.Left ? inventoryUI.GetSlots().slotLeftHand : inventoryUI.GetSlots().slotRightHand;
    }

    [Command]
    private void CmdGrab(GameObject itemObject, Hands hand)
    {
        RpcShareGrab(itemObject, gameObject, hand);
    }

    [ClientRpc]
    private void RpcShareGrab(GameObject itemObject, GameObject owner, Hands hand)
    {
        Item item = itemObject.GetComponent<Item>();

        Transform selectedHand = hand == Hands.Left
            ? owner.GetComponent<HumanInventory>().inventoryUI.GetSlots().slotLeftHand.physicalItemLocation
            : owner.GetComponent<HumanInventory>().inventoryUI.GetSlots().slotRightHand.physicalItemLocation;

        item.CreateVisual(selectedHand);
        item.HideOriginal();
    }


    private void PickUp(Item item)
    {
        if (!item.Held && item.compatibleSlots.HasFlag(SlotTypes.Hand))
        {
            ItemSlot slot = GetActiveHandSlot();
            if (!slot.uiItem)
            {
                CmdGrab(item.gameObject, activeHand);
            }

            if (activeHand == Hands.Left)
            {
                if (!inventoryUI.GetSlots().slotLeftHand.uiItem)
                {
                    CmdGrab(item.gameObject, activeHand);
                    inventoryUI.SetSlotUiItem(slot, item);
                }
            }
            else
            {
                if (!inventoryUI.GetSlots().slotRightHand.uiItem)
                    CmdGrab(item.gameObject, activeHand);
                inventoryUI.SetSlotUiItem(inventoryUI.GetSlots().slotRightHand, item);
            }
        }
    }

    public void DropItem()
    {
        CmdDrop(GetActiveHandSlot().uiItem.Item.gameObject);
        inventoryUI.ClearSlotUiItem(GetActiveHandSlot());
    }

    [Command]
    private void CmdDrop(GameObject itemObject)
    {
//        RpcShareDrop(itemObject);
        Item item = itemObject.GetComponent<Item>();
        item.RpcRelease();
    }
}