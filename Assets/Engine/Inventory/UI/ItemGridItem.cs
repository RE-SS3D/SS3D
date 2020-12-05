using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Engine.Inventory.UI
{
    public class ItemGridItem : MonoBehaviour
    {
        public Image Image;
        
        private Item item;

        /// <summary>
        /// The container item associated with this grid item
        /// </summary>
        public Item Item
        {
            get => item;
            set => UpdateItem(value);
        }

        public void UpdateItem(Item newItem)
        {
            Image.sprite = newItem.InventorySprite;
            item = newItem;
        }
    }
}