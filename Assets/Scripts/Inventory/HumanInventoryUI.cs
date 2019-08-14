using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class HumanInventoryUI : MonoBehaviour
{
    [SerializeField]
    private HumanInventoryUISlots slots;

    public HumanInventory Inventory;

    [SerializeField]
    private UiItem uiItemPrefab;

    public void Initialize(HumanInventory inv, HumanAttachmentPoints attachmentPoints)
    {
        Inventory = inv;

        SlotSetup(slots.slotLeftHand, attachmentPoints.LeftHand.transform);
        SlotSetup(slots.slotRightHand, attachmentPoints.RightHand.transform);

        SlotSetup(slots.slotHelmet, attachmentPoints.Helmet.transform);
        SlotSetup(slots.slotGloves, attachmentPoints.Gloves.transform);
        SlotSetup(slots.slotEars, attachmentPoints.Ears.transform);
        SlotSetup(slots.slotSuitStorage, attachmentPoints.SuitStorage.transform);
        SlotSetup(slots.slotVest, attachmentPoints.Vest.transform);
        SlotSetup(slots.slotMask, attachmentPoints.Mask.transform);
        SlotSetup(slots.slotShoes, attachmentPoints.Shoes.transform);
        SlotSetup(slots.slotGlasses, attachmentPoints.Glasses.transform);
        SlotSetup(slots.slotShirt, attachmentPoints.Shirt.transform);

        SlotSetup(slots.slotCard, attachmentPoints.Card.transform);
        SlotSetup(slots.slotBelt, attachmentPoints.Belt.transform);
        SlotSetup(slots.slotBackpack, attachmentPoints.Backpack.transform);
        SlotSetup(slots.slotPocketLeft, attachmentPoints.PocketLeft.transform);
        SlotSetup(slots.slotPocketRight, attachmentPoints.PocketRight.transform);
    }

    private void SlotSetup(ItemSlot slot, Transform attachmentPoint)
    {
        NetworkServer.Spawn(slot.gameObject);
        slot.physicalItemLocation = attachmentPoint;
    }

    public HumanInventoryUISlots GetSlots()
    {
        return slots;
    }

    public void SwitchActiveHand(Hands hand)
    {
        if (hand == Hands.Left)
        {
            slots.LeftHandActiveMarker.gameObject.SetActive(true);
            slots.RightHandActiveMarker.gameObject.SetActive(false);
            return;
        }

        slots.LeftHandActiveMarker.gameObject.SetActive(false);
        slots.RightHandActiveMarker.gameObject.SetActive(true);
    }

    public void ClearSlotUiItem(ItemSlot slot)
    {
        Destroy(slot.transform.GetChild(slot.transform.childCount - 1).gameObject);
    }

    public void SetSlotUiItem(ItemSlot slot, Item item)
    {
        UiItem uiItem = Instantiate(uiItemPrefab, slot.transform);
        slot.uiItem = uiItem;
        uiItem.Initialize(item);
    }

    public void ToggleBodyPanel()
    {
        slots.bodyPanel.transform.localScale =
            slots.bodyPanel.transform.localScale == Vector3.zero ? Vector3.one : Vector3.zero;
    }
}