using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.Containers;
using UnityEngine;
using UnityEngine.EventSystems;
using SS3D.Logging;

namespace SS3D.Systems.Inventory.UI
{
    public abstract class InventoryDisplayElementAlt : MonoBehaviour, IDropHandler
    {
        public InventoryAlt Inventory;

        /// <summary>
        /// Called when an item is being dropped onto this display
        /// </summary>
        /// <param name="display"></param>
        public abstract void OnItemDisplayDrop(ItemDisplayAlt display);

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
            if (eventData.button != PointerEventData.InputButton.Left) return;
            GameObject drag = eventData.pointerDrag;
            if (drag == null)
            {
                return;
            }

            ItemDisplayAlt display = drag.GetComponent<ItemDisplayAlt>();
            if (display == null)
            {
                Punpun.Warning(this, "dragging on null display");
                return;
            }

            OnItemDisplayDrop(display);
        }
    }
}