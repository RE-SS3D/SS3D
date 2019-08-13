using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Transform ReturnParent;

    private ItemSlot lastSlot;

    [SerializeField]
    private Image image;

    public void OnBeginDrag(PointerEventData eventData)
    {
        ReturnParent = transform.parent.transform;
        image.raycastTarget = false;

        lastSlot = GetComponentInParent<ItemSlot>();
        transform.SetParent(GetComponentInParent<Canvas>().transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            transform.SetParent(ReturnParent);
            lastSlot.uiItem.Item.visual.GetComponentInParent<HumanInventory>().DropItem();
            return;
        }

        transform.SetParent(ReturnParent);
        image.raycastTarget = true;

        lastSlot.Clear();

        RectTransform rt = ((RectTransform) transform);
        rt.offsetMin = new Vector2(5, rt.offsetMin.y);
        rt.offsetMax = new Vector2(-5, rt.offsetMax.y);
        rt.offsetMax = new Vector2(rt.offsetMax.x, -5);
        rt.offsetMin = new Vector2(rt.offsetMin.x, 5);
    }
}