using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollRectNoDragging : ScrollRect
{
    // This is just so you can't drag the UI around with your mouse, it causes visual glitches
    public override void OnBeginDrag(PointerEventData eventData) { }
    public override void OnDrag(PointerEventData eventData) { }
    public override void OnEndDrag(PointerEventData eventData) { }
}
