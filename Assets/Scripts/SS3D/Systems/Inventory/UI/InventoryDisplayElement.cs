using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SS3D.Systems.Inventory.UI
{
    public abstract class InventoryDisplayElement : MonoBehaviour, IDropHandler
    {
        public IInventory Inventory { get; set; }

        public void OnDrop(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            GameObject drag = eventData.pointerDrag;
            if (drag == null)
            {
                return;
            }

            if (!drag.TryGetComponent(out ItemDisplay display))
            {
                Log.Warning(this, "dragging on null display");
                return;
            }

            OnItemDisplayDrop(display);
        }

        /// <summary>
        /// Called when an item is dragged and dropped outside
        /// </summary>
        /// <param name="item">The dragged item</param>
        protected void DropItemOutside(Item item)
        {
            Inventory.ClientDropItem(item);
        }

        /// <summary>
        /// Called when an item is being dropped onto this display
        /// </summary>
        /// <param name="display"></param>
        protected abstract void OnItemDisplayDrop(ItemDisplay display);
    }
}
