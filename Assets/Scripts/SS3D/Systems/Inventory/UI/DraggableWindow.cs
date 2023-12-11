using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Systems.Inventory.UI
{
    public class DraggableWindow : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        private Vector2 _pointerOffset;
        private RectTransform _canvasRectTransform;
        private RectTransform _panelRectTransform;
        private bool _clampedToLeft;
        private bool _clampedToRight;
        private bool _clampedToTop;
        private bool _clampedToBottom;

        public void Start()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null) {
                _canvasRectTransform = canvas.transform as RectTransform;
                _panelRectTransform = transform as RectTransform;
            }
            _clampedToLeft = false;
            _clampedToRight = false;
            _clampedToTop = false;
            _clampedToBottom = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _panelRectTransform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRectTransform, eventData.position, eventData.pressEventCamera, out _pointerOffset);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_panelRectTransform == null)
                return;

            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition)) {
                _panelRectTransform.localPosition = localPointerPosition - _pointerOffset;

                ClampToWindow();

                Vector2 clampedPosition = _panelRectTransform.localPosition;
                if (_clampedToRight) clampedPosition.x = _canvasRectTransform.rect.width * 0.5f - _panelRectTransform.rect.width * (1 - _panelRectTransform.pivot.x);
                else if (_clampedToLeft) {
                    clampedPosition.x = -_canvasRectTransform.rect.width * 0.5f + _panelRectTransform.rect.width * _panelRectTransform.pivot.x;
                }

                if (_clampedToTop) clampedPosition.y = _canvasRectTransform.rect.height * 0.5f - _panelRectTransform.rect.height * (1 - _panelRectTransform.pivot.y);
                else if (_clampedToBottom) {
                    clampedPosition.y = -_canvasRectTransform.rect.height * 0.5f + _panelRectTransform.rect.height * _panelRectTransform.pivot.y;
                }
                _panelRectTransform.localPosition = clampedPosition;
            }
        }

        void ClampToWindow()
        {
            Vector3[] canvasCorners = new Vector3[4];
            Vector3[] panelRectCorners = new Vector3[4];
            _canvasRectTransform.GetWorldCorners(canvasCorners);
            _panelRectTransform.GetWorldCorners(panelRectCorners);

            if (panelRectCorners[2].x > canvasCorners[2].x) {
                if (!_clampedToRight) _clampedToRight = true;
            }
            else if (_clampedToRight) {
                _clampedToRight = false;
            }
            else if (panelRectCorners[0].x < canvasCorners[0].x) {
                if (!_clampedToLeft) _clampedToLeft = true;
            }
            else if (_clampedToLeft) {
                _clampedToLeft = false;
            }

            if (panelRectCorners[2].y > canvasCorners[2].y) {
                if (!_clampedToTop) _clampedToTop = true;
            }
            else if (_clampedToTop) {
                _clampedToTop = false;
            }
            else if (panelRectCorners[0].y < canvasCorners[0].y) {
                if (!_clampedToBottom) _clampedToBottom = true;
            }
            else if (_clampedToBottom) {
                _clampedToBottom = false;
            }
        }
    }
}