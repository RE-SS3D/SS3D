using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Engine.Inventory.UI
{
    /// <summary>
    /// A ui element to modify a container that contains one item
    /// </summary>
    [RequireComponent(typeof(ItemDisplay))]
    public class SingleItemContainerSlot : MonoBehaviour
    {
        public Inventory Inventory;
        
        private AttachedContainer container;
        private ItemDisplay itemDisplay;
        
        public AttachedContainer Container
        {
            get => container;
            set => UpdateContainer(value);
        }

        public void Start()
        {
            itemDisplay = GetComponent<ItemDisplay>();
            if (Container != null)
            {
                UpdateContainer(Container);
            }
        }

        public void ClickedOn()
        {
            Inventory.ClientInteractWithSingleSlot(container);
        }

        private void UpdateDisplay()
        {
            itemDisplay.Item = container.Container.Items.FirstOrDefault();
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