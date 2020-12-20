using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Engine.Inventory.UI
{
    public class ItemGridItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerClickHandler
    {
        public Image Image;
        
        [NonSerialized]
        public bool DropHandled;
        
        private Item item;
        private Transform oldParent;
        private Vector3 oldPosition;
        
        private Vector3 startMousePosition;
        private Vector3 startPosition;
        private Image image;

        /// <summary>
        /// The container item associated with this grid item
        /// </summary>
        public Item Item
        {
            get => item;
            set => UpdateItem(value);
        }

        public void Start()
        {
            image = GetComponent<Image>();
        }

        public void UpdateItem(Item newItem)
        {
            Image.sprite = newItem.InventorySprite;
            item = newItem;
        }
        
        public void OnPointerDown(PointerEventData eventData)
        {
            startPosition = transform.position;
            startMousePosition = Input.mousePosition;

            
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
            oldPosition = GetComponent<RectTransform>().localPosition;
            Vector3 tempPosition = transform.position;
            transform.SetParent(transform.root, false);
            transform.position = tempPosition;
            
            image.raycastTarget = false;
            DropHandled = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector3 diff = Input.mousePosition - startMousePosition;
            transform.position = startPosition + diff;
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            image.raycastTarget = true;
            
            if (DropHandled)
            {
                return;
            }
            
            transform.SetParent(oldParent, false);
            GetComponent<RectTransform>().localPosition = oldPosition;

            GameObject o = eventData.pointerCurrentRaycast.gameObject;
            if (o == null)
            {
                GetComponentInParent<ItemGrid>().DropItemOutside(item);   
            }
        }
    }
}