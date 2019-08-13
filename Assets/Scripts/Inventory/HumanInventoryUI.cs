using UnityEngine;

public class HumanInventoryUI : MonoBehaviour
{
    [SerializeField]
    private HumanInventoryUISlots slots;

    [SerializeField]
    private HumanInventory inventory;

    [SerializeField]
    private UiItem uiItemPrefab;

    public void Initialize(HumanInventory inv, HumanAttachmentPoints attachmentPoints)
    {
        inventory = inv;

        slots.slotLeftHand.physicalItemLocation = attachmentPoints.LeftHand.transform;
        slots.slotRightHand.physicalItemLocation = attachmentPoints.RightHand.transform;

        slots.slotHelmet.physicalItemLocation = attachmentPoints.Helmet.transform;
        slots.slotGloves.physicalItemLocation = attachmentPoints.Gloves.transform;
        slots.slotEars.physicalItemLocation = attachmentPoints.Ears.transform;
        slots.slotSuitStorage.physicalItemLocation = attachmentPoints.SuitStorage.transform;
        slots.slotVest.physicalItemLocation = attachmentPoints.Vest.transform;
        slots.slotMask.physicalItemLocation = attachmentPoints.Mask.transform;
        slots.slotShoes.physicalItemLocation = attachmentPoints.Shoes.transform;
        slots.slotGlasses.physicalItemLocation = attachmentPoints.Glasses.transform;
        slots.slotShirt.physicalItemLocation = attachmentPoints.Shirt.transform;

        slots.slotCard.physicalItemLocation = attachmentPoints.Card.transform;
        slots.slotBelt.physicalItemLocation = attachmentPoints.Belt.transform;
        slots.slotBackpack.physicalItemLocation = attachmentPoints.Backpack.transform;
        slots.slotPocketLeft.physicalItemLocation = attachmentPoints.PocketLeft.transform;
        slots.slotPocketRight.physicalItemLocation = attachmentPoints.PocketRight.transform;
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
        slot.UpdateVisualLocation(uiItem.Item.gameObject);
    }

    public void ToggleBodyPanel()
    {
        slots.bodyPanel.transform.localScale =
            slots.bodyPanel.transform.localScale == Vector3.zero ? Vector3.one : Vector3.zero;
    }
}