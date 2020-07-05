using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Content.Graphics.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class Draggable : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        public RectTransform menu;
        
        private Vector2 lastMousePosition;

        private void OnValidate()
        {
            if (menu == null)
            {
                menu = transform.parent.GetComponent<RectTransform>();
            }
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            lastMousePosition = eventData.position;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 diff = eventData.position - lastMousePosition;
            var position = menu.position;
            Vector3 newPosition = position + new Vector3(diff.x, diff.y, position.z);
            menu.position = newPosition;
            if (!IsRectInsideScreen(menu))
            {
                menu.position = position;
            }

            lastMousePosition = eventData.position;
        }

        private bool IsRectInsideScreen(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            Rect rect = new Rect(0,0,Screen.width, Screen.height);
            foreach(Vector3 corner in corners)
            {
                if(!rect.Contains(corner))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
