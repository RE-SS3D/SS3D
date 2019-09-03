using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class HumanInventoryUI : InventoryUi
{
    [SerializeField]
    private HumanInventoryUISlots humanSlots;

    [SerializeField]
    private UiItem uiItemPrefab;

    public void Initialize(HumanInventory inv, HumanAttachmentPoints attachmentPoints)
    {
        Inventory = inv;

        SlotSetup(humanSlots.slotLeftHand, attachmentPoints.LeftHand.transform);
        SlotSetup(humanSlots.slotRightHand, attachmentPoints.RightHand.transform);

        SlotSetup(humanSlots.slotHelmet, attachmentPoints.Helmet.transform);
        SlotSetup(humanSlots.slotGloves, attachmentPoints.Gloves.transform);
        SlotSetup(humanSlots.slotEars, attachmentPoints.Ears.transform);
        SlotSetup(humanSlots.slotSuitStorage, attachmentPoints.SuitStorage.transform);
        SlotSetup(humanSlots.slotVest, attachmentPoints.Vest.transform);
        SlotSetup(humanSlots.slotMask, attachmentPoints.Mask.transform);
        SlotSetup(humanSlots.slotShoes, attachmentPoints.Shoes.transform);
        SlotSetup(humanSlots.slotGlasses, attachmentPoints.Glasses.transform);
        SlotSetup(humanSlots.slotShirt, attachmentPoints.Shirt.transform);

        SlotSetup(humanSlots.slotCard, attachmentPoints.Card.transform);
        SlotSetup(humanSlots.slotBelt, attachmentPoints.Belt.transform);
        SlotSetup(humanSlots.slotBackpack, attachmentPoints.Backpack.transform);
        SlotSetup(humanSlots.slotPocketLeft, attachmentPoints.PocketLeft.transform);
        SlotSetup(humanSlots.slotPocketRight, attachmentPoints.PocketRight.transform);
    }

    private void SlotSetup(ItemSlot slot, Transform attachmentPoint)
    {
        NetworkServer.Spawn(slot.gameObject);
        slot.physicalItemLocation = attachmentPoint;
        humanSlots.SlotsList.Add(slot);
    }

    public override List<ItemSlot> GetSlots()
    {
        return humanSlots.SlotsList;
    }

    public void SwitchActiveHand(Hands hand)
    {
        if (hand == Hands.Left)
        {
            humanSlots.LeftHandActiveMarker.gameObject.SetActive(true);
            humanSlots.RightHandActiveMarker.gameObject.SetActive(false);
            return;
        }

        humanSlots.LeftHandActiveMarker.gameObject.SetActive(false);
        humanSlots.RightHandActiveMarker.gameObject.SetActive(true);
    }

    public void ClearSlotUiItem(ItemSlot slot)
    {
        UiItem item = slot.GetComponentInChildren<UiItem>();
        if (item) Destroy(item.gameObject);
    }

    public void SetSlotUiItem(ItemSlot slot, Item item)
    {
        UiItem uiItem = Instantiate(uiItemPrefab, slot.transform);
        slot.uiItem = uiItem;
        uiItem.Initialize(item);
    }

    public void ToggleBodyPanel()
    {
        humanSlots.bodyPanel.transform.localScale =
            humanSlots.bodyPanel.transform.localScale == Vector3.zero ? Vector3.one : Vector3.zero;
    }
}