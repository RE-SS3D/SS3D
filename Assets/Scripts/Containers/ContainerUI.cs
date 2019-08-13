using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContainerUI : MonoBehaviour
{
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

    public bool visible;

    private Transform containerTransform;

    private void OnEnable()
    {
        visible = false;
        canvasGroup.alpha = 0;
    }

    public void Initialize(int size, int columns, Transform container, List<Transform> spots)
    {
        Initialize(size, columns, container, new List<Item>(), spots);
    }

    public void Initialize(int size, int columns, Transform container, List<Item> items,
        List<Transform> visualItemSpots)
    {
        bounds.anchoredPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, container.position);
        containerTransform = container;
        slotContainer.constraintCount = columns;

        for (int i = 0; i < size; i++)
        {
            ItemSlot slot = Instantiate(SlotPrefab, slotContainer.transform);

            // Items on spawn
            if (i < items.Count)
            {
                UiItem uiItem = Instantiate(uiItemPrefab, slot.transform);
                uiItem.Initialize(items[i]);
                slot.UpdateVisualLocation(uiItem.Item.gameObject);
            }

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
}