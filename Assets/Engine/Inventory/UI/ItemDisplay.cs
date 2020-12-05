using UnityEngine;
using UnityEngine.UI;

namespace SS3D.Engine.Inventory.UI
{
    public class ItemDisplay : MonoBehaviour
    {
        public Image ItemImage;
        
        [SerializeField]
        private Item item;

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
            if (item != null)
            {
                UpdateDisplay();
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