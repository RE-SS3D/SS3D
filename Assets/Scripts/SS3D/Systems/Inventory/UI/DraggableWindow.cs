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

        public void OnBeginDrag(PointerEventData eventData)
        {
            _panelRectTransform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_panelRectTransform, eventData.position, eventData.pressEventCamera, out _pointerOffset);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_panelRectTransform == null)
            {
                return;
            }

            Vector2 localPointerPosition;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition))
            {
                return;
            }

            _panelRectTransform.localPosition = localPointerPosition - _pointerOffset;

            ClampToWindow();

            Vector2 clampedPosition = _panelRectTransform.localPosition;

            float canvasWidth = _canvasRectTransform.rect.width;
            float panelWidth = _panelRectTransform.rect.width;

            float canvasHeight = _canvasRectTransform.rect.height;
            float panelHeight = _panelRectTransform.rect.height;

            if (_clampedToRight)
            {
                clampedPosition.x = (canvasWidth * 0.5f) - (panelWidth * (1 - _panelRectTransform.pivot.x));
            }
            else if (_clampedToLeft)
            {
                clampedPosition.x = (-canvasWidth * 0.5f) + (panelWidth * _panelRectTransform.pivot.x);
            }

            if (_clampedToTop)
            {
                clampedPosition.y = (canvasHeight * 0.5f) - (panelHeight * (1 - _panelRectTransform.pivot.y));
            }
            else if (_clampedToBottom)
            {
                clampedPosition.y = (-canvasHeight * 0.5f) + (panelHeight * _panelRectTransform.pivot.y);
            }

            _panelRectTransform.localPosition = clampedPosition;
        }

        protected void Start()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                _canvasRectTransform = canvas.transform as RectTransform;
                _panelRectTransform = transform as RectTransform;
            }

            _clampedToLeft = false;
            _clampedToRight = false;
            _clampedToTop = false;
            _clampedToBottom = false;
        }

        private void ClampToWindow()
        {
            Vector3[] canvasCorners = new Vector3[4];
            Vector3[] panelRectCorners = new Vector3[4];
            _canvasRectTransform.GetWorldCorners(canvasCorners);
            _panelRectTransform.GetWorldCorners(panelRectCorners);

            if (panelRectCorners[2].x > canvasCorners[2].x)
            {
                if (!_clampedToRight)
                {
                    _clampedToRight = true;
                }
            }
            else if (_clampedToRight)
            {
                _clampedToRight = false;
            }
            else if (panelRectCorners[0].x < canvasCorners[0].x)
            {
                if (!_clampedToLeft)
                {
                    _clampedToLeft = true;
                }
            }
            else if (_clampedToLeft)
            {
                _clampedToLeft = false;
            }

            if (panelRectCorners[2].y > canvasCorners[2].y)
            {
                if (!_clampedToTop)
                {
                    _clampedToTop = true;
                }
            }
            else if (_clampedToTop)
            {
                _clampedToTop = false;
            }
            else if (panelRectCorners[0].y < canvasCorners[0].y)
            {
                if (!_clampedToBottom)
                {
                    _clampedToBottom = true;
                }
            }
            else if (_clampedToBottom)
            {
                _clampedToBottom = false;
            }
        }
    }
}