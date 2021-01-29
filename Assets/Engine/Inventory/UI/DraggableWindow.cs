using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SS3D.Engine.Interactions.UI
{
    public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        public void Start()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) {
                canvasRectTransform = canvas.transform as RectTransform;
                panelRectTransform = transform as RectTransform;
            }
            clampedToLeft = false;
            clampedToRight = false;
            clampedToTop = false;
            clampedToBottom = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            panelRectTransform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, eventData.position, eventData.pressEventCamera, out pointerOffset);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (panelRectTransform == null)
                return;

            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition)) {
                panelRectTransform.localPosition = localPointerPosition - pointerOffset;

                ClampToWindow();

                Vector2 clampedPosition = panelRectTransform.localPosition;
                if (clampedToRight) clampedPosition.x = canvasRectTransform.rect.width * 0.5f - panelRectTransform.rect.width * (1 - panelRectTransform.pivot.x);
                else if (clampedToLeft) {
                    clampedPosition.x = -canvasRectTransform.rect.width * 0.5f + panelRectTransform.rect.width * panelRectTransform.pivot.x;
                }

                if (clampedToTop) clampedPosition.y = canvasRectTransform.rect.height * 0.5f - panelRectTransform.rect.height * (1 - panelRectTransform.pivot.y);
                else if (clampedToBottom) {
                    clampedPosition.y = -canvasRectTransform.rect.height * 0.5f + panelRectTransform.rect.height * panelRectTransform.pivot.y;
                }
                panelRectTransform.localPosition = clampedPosition;
            }
        }

        void ClampToWindow()
        {
            Vector3[] canvasCorners = new Vector3[4];
            Vector3[] panelRectCorners = new Vector3[4];
            canvasRectTransform.GetWorldCorners(canvasCorners);
            panelRectTransform.GetWorldCorners(panelRectCorners);

            if (panelRectCorners[2].x > canvasCorners[2].x) {
                if (!clampedToRight) clampedToRight = true;
            }
            else if (clampedToRight) {
                clampedToRight = false;
            }
            else if (panelRectCorners[0].x < canvasCorners[0].x) {
                if (!clampedToLeft) clampedToLeft = true;
            }
            else if (clampedToLeft) {
                clampedToLeft = false;
            }

            if (panelRectCorners[2].y > canvasCorners[2].y) {
                if (!clampedToTop) clampedToTop = true;
            }
            else if (clampedToTop) {
                clampedToTop = false;
            }
            else if (panelRectCorners[0].y < canvasCorners[0].y) {
                if (!clampedToBottom) clampedToBottom = true;
            }
            else if (clampedToBottom) {
                clampedToBottom = false;
            }
        }

        private Vector2 pointerOffset;
        private RectTransform canvasRectTransform;
        private RectTransform panelRectTransform;
        private bool clampedToLeft;
        private bool clampedToRight;
        private bool clampedToTop;
        private bool clampedToBottom;
    }
}