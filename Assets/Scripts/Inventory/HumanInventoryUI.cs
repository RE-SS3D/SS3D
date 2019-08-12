using UnityEngine;

public class HumanInventoryUI : MonoBehaviour
{
    [SerializeField]
    private HumanInventoryUISlots slots;

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

    public void ToggleBodyPanel()
    {
        slots.bodyPanel.transform.localScale =
            slots.bodyPanel.transform.localScale == Vector3.zero ? Vector3.one : Vector3.zero;
    }

    public void SetHelmetSlotImage(Sprite sprite)
    {
        slots.itemHelmet.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetLeftHandSlotImage(Sprite sprite)
    {
        slots.itemLeftHand.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetRightHandSlotImage(Sprite sprite)
    {
        slots.itemRightHand.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetGlovesSlotImage(Sprite sprite)
    {
        slots.itemGloves.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetEarsSlotImage(Sprite sprite)
    {
        slots.itemEars.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetSuitStorageSlotImage(Sprite sprite)
    {
        slots.itemSuitStorage.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetVestSlotImage(Sprite sprite)
    {
        slots.itemVest.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetMaskSlotImage(Sprite sprite)
    {
        slots.itemMask.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetShoesSlotImage(Sprite sprite)
    {
        slots.itemShoes.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetGlassesSlotImage(Sprite sprite)
    {
        slots.itemGlasses.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetShirtSlotImage(Sprite sprite)
    {
        slots.itemShirt.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetCardSlotImage(Sprite sprite)
    {
        slots.itemCard.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetBeltSlotImage(Sprite sprite)
    {
        slots.itemBelt.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetBackpackSlotImage(Sprite sprite)
    {
        slots.itemBackpack.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetPocketLeftSlotImage(Sprite sprite)
    {
        slots.itemPocketLeft.sprite = sprite ? sprite : slots.defaultImage;
    }

    public void SetPocketRightSlotImage(Sprite sprite)
    {
        slots.itemPocketRight.sprite = sprite ? sprite : slots.defaultImage;
    }
}