using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class ContainerUI : InventoryUi
{
    private List<ItemSlot> itemSlots = new List<ItemSlot>();

    public bool visible;

    [Header("References")]
    [SerializeField]
    private RectTransform bounds;

    [SerializeField]
    private GridLayoutGroup slotContainer;

    [SerializeField]
    private ItemSlot SlotPrefab;

    [SerializeField]
    private UiItem uiItemPrefab;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private Transform containerTransform;

    private void OnEnable()
    {
        visible = false;
        canvasGroup.alpha = 0;
    }

    public void Initialize(int size, int columns, Transform container, List<Transform> spots, Inventory inv)
    {
        Initialize(size, columns, container, new List<Item>(), spots, inv);
    }

    public void Initialize(int size, int columns, Transform container, List<Item> items,
        List<Transform> visualItemSpots, Inventory inv)
    {
        bounds.anchoredPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, container.position);
        containerTransform = container;
        slotContainer.constraintCount = columns;

        Inventory = inv;

        for (int i = 0; i < size; i++)
        {
            ItemSlot slot = Instantiate(SlotPrefab, slotContainer.transform);
            slot.Initialize(null, this);
            itemSlots.Add(slot);

            NetworkServer.Spawn(slot.gameObject);
            
            // Items on spawn
//            if (i < items.Count)
//            {
//                UiItem uiItem = Instantiate(uiItemPrefab, slot.transform);
//                uiItem.Initialize(items[i]);
//            }

            if (i < visualItemSpots.Count) slot.physicalItemLocation = visualItemSpots[i];
        }
    }


    public void ToggleVisible()
    {
        visible = !visible;
        if (visible)
        {
            canvasGroup.alpha = 1;
        }
        else
        {
            canvasGroup.alpha = 0;
        }
    }

    private void Update()
    {
        if (visible)
        {
            bounds.anchoredPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, containerTransform.position);
        }
    }

    public override List<ItemSlot> GetSlots()
    {
        return itemSlots;
    }
}