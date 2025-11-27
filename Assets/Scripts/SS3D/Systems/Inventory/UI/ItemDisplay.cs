using SS3D.Systems.Inventory.Items;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// ItemDisplay is in charge of displaying correctly an item sprite in the UI.
    /// It allows actions such as dragging the sprite around the screen.
    /// </summary>
    public class ItemDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler
    {
        public event EventHandler OnDragOutOfUI;

        [SerializeField]
        private Image _itemImage;

        private Vector3 _oldPosition;

        private Item _item;

        private Transform _oldParent;
        private Vector2 _startMousePosition;
        private Vector3 _startPosition;
        private Image _slotImage;
        private Outline _outlineInner;
        private Outline _outlineOuter;

        public Item Item
        {
            get => _item;
            set
            {
                _item = value;
                UpdateDisplay();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _startPosition = transform.position;
            _startMousePosition = Mouse.current.position.ReadValue();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            // Somehow, itemdisplay hides the other IPointerClickHandler in it's parent, so the event OnpointerClick is never
            // called, for exemple in SingleItemContainerSlot. That's why we need to call the events on the parent from there.
            IPointerClickHandler[] pointerDownHandlers = transform.parent.GetComponentsInParent<IPointerClickHandler>();
            foreach (IPointerClickHandler pointerHandler in pointerDownHandlers)
            {
                pointerHandler?.OnPointerClick(eventData);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Only allow to drag with a left click.
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _oldParent = transform.parent;
            _oldPosition = GetComponent<RectTransform>().localPosition;

            Vector3 tempPosition = transform.position;
            transform.SetParent(transform.root, false);
            transform.position = tempPosition;

            _slotImage.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Only allow to drag with a left click.
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            Vector3 diff = Mouse.current.position.ReadValue() - _startMousePosition;
            transform.position = _startPosition + diff;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            // Only allow to drag with a left click.
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _slotImage.raycastTarget = true;

            // If the raycast did not hit any element from the UI, drop the item out of the inventory.
            GameObject o = eventData.pointerCurrentRaycast.gameObject;
            if (o == null)
            {
                OnDragOutOfUI?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ResetPositionAndParent()
        {
            if (_oldParent)
            {
                transform.SetParent(_oldParent, false);
                GetComponent<RectTransform>().localPosition = _oldPosition;
            }
        }

        public void MakeVisible(bool visible)
        {
            Image[] images = GetComponentsInChildren<Image>();
            foreach (Image image in images)
            {
                image.enabled = visible;
            }
        }

        protected void Start()
        {
            _slotImage = GetComponent<Image>();
            if (!_outlineInner)
            {
                _outlineInner = _itemImage.gameObject.AddComponent<Outline>();
                _outlineInner.effectColor = new Color(0, 0, 0, 0.4f);
                _outlineInner.effectDistance = new Vector2(0.6f, 0.6f);
            }

            if (!_outlineOuter)
            {
                _outlineInner = _itemImage.gameObject.AddComponent<Outline>();
                _outlineInner.effectColor = new Color(0, 0, 0, 0.2f);
                _outlineInner.effectDistance = new Vector2(0.8f, 0.8f);
            }

            if (_item != null)
            {
                UpdateDisplay();
            }
        }

        private void UpdateDisplay()
        {
            if (_itemImage == null)
            {
                return;
            }

            _itemImage.sprite = Item != null ? Item.ItemSprite : null;

            Color imageColor = _itemImage.color;
            imageColor.a = _itemImage.sprite != null ? 255 : 0;
            _itemImage.color = imageColor;
        }
    }
}
