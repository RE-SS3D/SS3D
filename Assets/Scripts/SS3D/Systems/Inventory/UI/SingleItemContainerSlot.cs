using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interfaces;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// SingleItemContainerSlot allows displaying the content of a container that contain a single item in the UI.
    /// It handles updating the inventory when an item is dropped on it, and it changes the displayed sprite accordingly.
    /// As of now, it's only use is displaying the content of the containers on the hands of the player in the UI slots.
    /// </summary>
    public class SingleItemContainerSlot : InventoryDisplayElement, IPointerClickHandler, ISlotProvider
    {
        public ItemDisplay ItemDisplay;

        public ContainerType ContainerType;

        /// <summary>
        /// The container displayed by this slot.
        /// </summary>
        private AttachedContainer _container;

        public AttachedContainer Container
        {
            get => _container;
            set => UpdateContainer(value);
        }

        public void Start()
        {
            Assert.IsNotNull(ItemDisplay);
            if (Container != null)
            {
                UpdateContainer(Container);
            }
            if(_container.Items.Count() > 0) 
            {
                ItemDisplay.Item =  _container.Items.First();
            }
        }

        public void OnDestroy()
        {
            Destroy(ItemDisplay);
        }

        /// <summary>
        /// When dragging and dropping an item sprite over this slot, update the inventory
        /// and the displayed sprite inside the slot.
        /// Does nothing if the slot already has an item.
        /// </summary>
        public override void OnItemDisplayDrop(ItemDisplay display)
        {
            if (!_container.Empty)
            {
                return;
            }

            if (!_container.CanContainItem(display.Item))
            {
                return;
            }

            // Can't put an item in its own container
            if (display.Item.GetComponentsInChildren<AttachedContainer>().AsEnumerable().Contains(Container))
            {
                return;
            }

            display.ShouldDrop = true;
			display.MakeVisible(false);
            Inventory.ClientTransferItem(display.Item, Vector2Int.zero, Container);
        }

        /// <summary>
        /// Change the displayed sprite inside the slot.
        /// </summary>
        private void UpdateDisplay()
        {
            var item = _container.Items.FirstOrDefault();
			ItemDisplay.Item = item;
			ItemDisplay.MakeVisible(true);
		}

        /// <summary>
        /// UpdateContainer modify the container that this slot display, replacing the old one with newContainer.
        /// </summary>
        private void UpdateContainer(AttachedContainer newContainer)
        {
            if (_container == newContainer)
            {
                return;
            }

            if (_container != null)
            {
                _container.OnContentsChanged -= ContainerContentsChanged;
            }

            newContainer.OnContentsChanged += ContainerContentsChanged;
            _container = newContainer;
        }

        private void ContainerContentsChanged(AttachedContainer _, Item oldItem, Item newItem, ContainerChangeType type)
        {
            if (type != ContainerChangeType.Move)
            {
                UpdateDisplay();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Inventory.ClientInteractWithContainerSlot(_container, new Vector2Int(0, 0));

			if(ContainerType == ContainerType.Hand)
			{
				Inventory.ActivateHand(_container);
			}   
        }

        public GameObject GetCurrentGameObjectInSlot()
        {
            if (ItemDisplay.Item == null)
            {
                return null;
            }
            else
            {
                return ItemDisplay.Item.gameObject;
            }
        }

    }
}