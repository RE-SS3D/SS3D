using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace SS3D.Engine.Inventory.UI
{
    /// <summary>
    /// A ui element to modify a container that contains one item
    /// </summary>
    public class SingleItemContainerSlot : InventoryDisplayElement
    {
        public ItemDisplay ItemDisplay;
        private AttachedContainer container;

        public AttachedContainer Container
        {
            get => container;
            set => UpdateContainer(value);
        }

        public void Start()
        {
            Assert.IsNotNull(ItemDisplay);
            if (Container != null)
            {
                UpdateContainer(Container);
            }
        }
        
        public override void OnItemDrop(ItemDisplay display)
        {
            if (!container.Container.Empty)
            {
                return;
            }

            display.DropAccepted = true;
            ItemDisplay.Item = display.Item;
            Inventory.ClientTransferItem(ItemDisplay.Item, Vector2Int.zero, Container);
        }

        public void ClickedOn()
        {
            Inventory.ClientInteractWithSingleSlot(container);
        }

        private void UpdateDisplay()
        {
            ItemDisplay.Item = container.Container.Items.FirstOrDefault();
        }

        private void UpdateContainer(AttachedContainer newContainer)
        {
            if (container == newContainer)
            {
                return;
            }
            
            if (container != null)
            {
                container.Container.ContentsChanged -= ContainerContentsChanged;
            }
            
            newContainer.Container.ContentsChanged += ContainerContentsChanged;
            container = newContainer;
        }

        private void ContainerContentsChanged(Container _, IEnumerable<Item> items, Container.ContainerChangeType changeType)
        {
            if (changeType != Engine.Inventory.Container.ContainerChangeType.Move)
            {
                UpdateDisplay();
            }
        }
    }
}