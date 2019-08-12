using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using UnityEngine;

public class HumanInventory : MonoBehaviour
{
    private int ActiveHandIndex = 0;

    [SerializeField]
    private HumanInventorySlots Slots;

    private HumanInventoryUI inventoryUI;

    public HumanInventoryUI HumanInventoryUiPrefab;

    private void Awake()
    {
        inventoryUI = Instantiate(HumanInventoryUiPrefab);
    }

    private void Update()
    {
        if (Input.GetButtonDown("SwapActive"))
        {
            ActiveHandIndex = ActiveHandIndex == 1 ? 0 : 1;
            inventoryUI.SwitchActiveHand(ActiveHandIndex);
        }

        if (Input.GetButtonDown("Fire2"))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 100))
            {
                Debug.Log(hit.collider.gameObject.name);
                Item item = hit.collider.GetComponent<Item>();
                if (item != null) PickUp(item);
            }
        }
    }

    private void PickUp(Item item)
    {
        if (ActiveHandIndex == 0)
        {
            Slots.LeftHand = item;
            inventoryUI.SetLeftHandSlotImage(item.Image);
            item.transform.SetParent(Slots.LeftHandTransform);
        }
        else
        {
            Slots.LeftHand = item;
            inventoryUI.SetRightHandSlotImage(item.Image);
            item.transform.SetParent(Slots.RightHandTransform);
        }
    }

    public void SetHelmetSlot(Item item)
    {
        Slots.Helmet = item;
        inventoryUI.SetHelmetSlotImage(item.Image);
    }

    public void SetLeftHandSlot(Item item)
    {
        Slots.LeftHand = item;
        inventoryUI.SetLeftHandSlotImage(item.Image);
    }

    public void SetRightHandSlot(Item item)
    {
        Slots.RightHand = item;
        inventoryUI.SetRightHandSlotImage(item.Image);
    }

    public void SetGlovesSlot(Item item)
    {
        Slots.Gloves = item;
        inventoryUI.SetGlovesSlotImage(item.Image);
    }

    public void SetEarsSlot(Item item)
    {
        Slots.Ears = item;
        inventoryUI.SetEarsSlotImage(item.Image);
    }

    public void SetSuitStorageSlot(Item item)
    {
        Slots.SuitStorage = item;
        inventoryUI.SetSuitStorageSlotImage(item.Image);
    }

    public void SetVestSlot(Item item)
    {
        Slots.Vest = item;
        inventoryUI.SetVestSlotImage(item.Image);
    }

    public void SetMaskSlot(Item item)
    {
        Slots.Mask = item;
        inventoryUI.SetMaskSlotImage(item.Image);
    }

    public void SetShoesSlot(Item item)
    {
        Slots.Shoes = item;
        inventoryUI.SetShoesSlotImage(item.Image);
    }

    public void SetGlassesSlot(Item item)
    {
        Slots.Glasses = item;
        inventoryUI.SetGlassesSlotImage(item.Image);
    }

    public void SetShirtSlot(Item item)
    {
        Slots.Shirt = item;
        inventoryUI.SetShirtSlotImage(item.Image);
    }

    public void SetCardSlot(Item item)
    {
        Slots.Card = item;
        inventoryUI.SetCardSlotImage(item.Image);
    }

    public void SetBeltSlot(Item item)
    {
        Slots.Belt = item;
        inventoryUI.SetBeltSlotImage(item.Image);
    }

    public void SetBackpackSlot(Item item)
    {
        Slots.Backpack = item;
        inventoryUI.SetBackpackSlotImage(item.Image);
    }

    public void SetPocketLeftSlot(Item item)
    {
        Slots.PocketLeft = item;
        inventoryUI.SetPocketLeftSlotImage(item.Image);
    }

    public void SetPocketRightSlot(Item item)
    {
        Slots.PocketLeft = item;
        inventoryUI.SetPocketRightSlotImage(item.Image);
    }
}