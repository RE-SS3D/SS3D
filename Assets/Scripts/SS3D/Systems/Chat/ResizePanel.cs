using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Engine.Chat
{
	public class ResizePanel : MonoBehaviour, IPointerDownHandler, IDragHandler 
    {
		public Vector2 minSize = new Vector2 (100, 100);
		public Vector2 maxSize = new Vector2 (400, 400);
	
		[SerializeField] private RectTransform panelRectTransform;
        
		private Vector2 _originalLocalPointerPosition;
		private Vector2 _originalSizeDelta;
	
		public void OnPointerDown(PointerEventData data) 
        {
			_originalSizeDelta = panelRectTransform.sizeDelta;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out _originalLocalPointerPosition);
		}
	
		public void OnDrag(PointerEventData data)
        {
            if (panelRectTransform == null)
            {
                return;
            }

			RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out Vector2 localPointerPosition);
			Vector3 offsetToOriginal = localPointerPosition - _originalLocalPointerPosition;
		
			Vector2 sizeDelta = _originalSizeDelta + new Vector2 (offsetToOriginal.x, -offsetToOriginal.y);
			sizeDelta = new Vector2 
            (
				Mathf.Clamp (sizeDelta.x, minSize.x, maxSize.x),
				Mathf.Clamp (sizeDelta.y, minSize.y, maxSize.y)
			);
		
			panelRectTransform.sizeDelta = sizeDelta;
		}
	}
}