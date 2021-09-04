using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using SS3D.Engine.Examine;
using SS3D.Engine.Input;

namespace SS3D.Engine.Inventory.UI
{
    /// <summary>
    /// Shows an item and allows actions such as dragging
    /// </summary>
    public class ItemDisplay : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler
    {
        public Image ItemImage;
        [NonSerialized]
        public bool DropAccepted;
        [NonSerialized]
        public Vector3 OldPosition;

        protected InventoryDisplayElement InventoryDisplayElement;
        
        [SerializeField]
        private Item item;
        private Transform oldParent;
        private Vector3 startMousePosition;
        private Vector3 startPosition;
        private Image slotImage;


        public Item Item
        {
            get => item;
            set
            {
                item = value;
                UpdateDisplay();
            }
        }

        public void Start()
        {
            slotImage = GetComponent<Image>();
            if (item != null)
            {
                UpdateDisplay();
            } 
        }
        
        public virtual void OnDropAccepted(){}
        
        public void OnPointerDown(PointerEventData eventData)
        {
            startPosition = transform.position;
            startMousePosition = InputHelper.inp.Pointer.Position.ReadValue<Vector2>();
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            // WOW Unity, this is some amazing UI stuff
            var pointerDownHandler = transform.parent.GetComponentInParent<IPointerClickHandler>();
            pointerDownHandler?.OnPointerClick(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            oldParent = transform.parent;
            if (InventoryDisplayElement == null)
            {
                InventoryDisplayElement = oldParent.GetComponentInParent<InventoryDisplayElement>();
            }
            
            OldPosition = GetComponent<RectTransform>().localPosition;
            Vector3 tempPosition = transform.position;
            transform.SetParent(transform.root, false);
            transform.position = tempPosition;
            
            slotImage.raycastTarget = false;
            DropAccepted = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 mouse = InputHelper.inp.Pointer.Position.ReadValue<Vector2>();
            Vector3 diff = new Vector3(mouse.x, mouse.y) - startMousePosition;
            transform.position = startPosition + diff;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            slotImage.raycastTarget = true;
            
            if (DropAccepted)
            {
                OnDropAccepted();
                return;
            }
            
            transform.SetParent(oldParent, false);
            GetComponent<RectTransform>().localPosition = OldPosition;

            GameObject o = eventData.pointerCurrentRaycast.gameObject;
            if (o == null)
            {
                GetComponentInParent<InventoryDisplayElement>().DropItemOutside(Item);   
            }
        }
        
        private void UpdateDisplay()
        {
            ItemImage.sprite = Item != null ? Item.InventorySprite : null;
            
            Color imageColor = ItemImage.color;
            imageColor.a = ItemImage.sprite != null ? 255 : 0;
            ItemImage.color = imageColor;
        }

    }
}