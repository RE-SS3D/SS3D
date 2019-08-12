using UnityEngine;
using UnityEngine.UI;

public class ContainerUI : MonoBehaviour
{
    [SerializeField]
    private RectTransform bounds;

    [SerializeField]
    private GridLayoutGroup slotContainer;

    [SerializeField]
    private RectTransform SlotPrefab;

    public bool visible;

    private Transform containerTransform;

    public void Initialize(int size, Transform container)
    {
        bounds.anchoredPosition = RectTransformUtility.WorldToScreenPoint(Camera.main, container.position);

        containerTransform = container;

        slotContainer.constraintCount = size >= 4 ? 4 : size;

        visible = true;

        for (int i = 0; i < size; i++)
        {
            Instantiate(SlotPrefab, slotContainer.transform);
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