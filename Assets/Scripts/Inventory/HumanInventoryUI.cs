using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class HumanInventoryUI : NetworkBehaviour
{
    [SerializeField]
    private HumanInventoryUISlots slots;

    [SerializeField]
    private HumanInventory inventory;

    [SerializeField]
    private UiItem uiItemPrefab;

    public void Initialize(HumanInventory inv)
    {
        inventory = inv;
    }

    public HumanInventoryUISlots GetSlots()
    {
        return slots;
    }

    public void SwitchActiveHand(int activeHand)
    {
        if (activeHand == 0)
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
        slot.PutItemInSlot(uiItem);
    }

    public void ToggleBodyPanel()
    {
        slots.bodyPanel.transform.localScale =
            slots.bodyPanel.transform.localScale == Vector3.zero ? Vector3.one : Vector3.zero;
    }
}