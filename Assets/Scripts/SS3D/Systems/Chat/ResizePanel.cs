using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Engine.Chat
{
	public class ResizePanel : MonoBehaviour, IPointerDownHandler, IDragHandler 
    {
        public Vector2 MinSize = new Vector2 (100, 100);
        public Vector2 MaxSize = new Vector2 (400, 400);
	
        [SerializeField] private RectTransform _panelRectTransform;
        
		private Vector2 _originalLocalPointerPosition;
		private Vector2 _originalSizeDelta;
	
		public void OnPointerDown(PointerEventData data) 
        {
			_originalSizeDelta = _panelRectTransform.sizeDelta;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRectTransform, data.position, data.pressEventCamera, out _originalLocalPointerPosition);
		}
	
		public void OnDrag(PointerEventData data)
        {
            if (_panelRectTransform == null)
            {
                return;
            }

			RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRectTransform, data.position, data.pressEventCamera, out Vector2 localPointerPosition);
			Vector3 offsetToOriginal = localPointerPosition - _originalLocalPointerPosition;
		
			Vector2 sizeDelta = _originalSizeDelta + new Vector2 (offsetToOriginal.x, -offsetToOriginal.y);
			sizeDelta = new Vector2 
            (
				Mathf.Clamp (sizeDelta.x, MinSize.x, MaxSize.x),
				Mathf.Clamp (sizeDelta.y, MinSize.y, MaxSize.y)
			);
		
			_panelRectTransform.sizeDelta = sizeDelta;
		}
	}
}