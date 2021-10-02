using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Engine.Inventory.UI
{
    public abstract class InventoryDisplayElement : MonoBehaviour, IDropHandler
    {
        public Inventory Inventory;

        /// <summary>
        /// Called when an containable is being dropped onto this display
        /// </summary>
        /// <param name="display"></param>
        public abstract void OnItemDrop(ItemDisplay display);
        
        /// <summary>
        /// Called when an containable is dragged and dropped outside
        /// </summary>
        /// <param name="containable">The dragged containable</param>
        public void DropItemOutside(IContainable containable)
        {
            Inventory.ClientDropItem(containable);
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