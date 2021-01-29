using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Engine.Inventory.UI
{
    public abstract class InventoryDisplayElement : MonoBehaviour, IDropHandler
    {
        public Inventory Inventory;

        /// <summary>
        /// Called when an item is being dropped onto this display
        /// </summary>
        /// <param name="display"></param>
        public abstract void OnItemDrop(ItemDisplay display);
        
        /// <summary>
        /// Called when an item is dragged and dropped outside
        /// </summary>
        /// <param name="item">The dragged item</param>
        public void DropItemOutside(Item item)
        {
            Inventory.ClientDropItem(item);
        }

        public void OnDrop(PointerEventData eventData)
        {
            GameObject drag = eventData.pointerDrag;
            if (drag == null)
            {
                return;
            }

            var display = drag.GetComponent<ItemDisplay>();
            if (display == null)
            {
                return;
            }
            
            OnItemDrop(display);
        }
    }
}