﻿using System.Collections.Generic;
using System.Linq;
using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Storage.Interfaces;
using SS3D.Systems.Storage.Items;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace SS3D.Systems.Storage.UI
{
    /// <summary>
    /// A ui element to modify a container that contains one item
    /// </summary>
    public class SingleItemContainerSlot : InventoryDisplayElement, IPointerClickHandler, ISlotProvider
    {
        public ItemDisplay ItemDisplay;
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
        }
        
        public override void OnItemDrop(ItemDisplay display)
        {
            if (!_container.Container.Empty)
            {
                return;
            }

            display.DropAccepted = true;
            ItemDisplay.Item = display.Item;
            Inventory.ClientTransferItem(ItemDisplay.Item, Vector2Int.zero, Container);
        }

        private void UpdateDisplay()
        {
            ItemDisplay.Item = _container.Container.Items.FirstOrDefault();
        }

        private void UpdateContainer(AttachedContainer newContainer)
        {
            if (_container == newContainer)
            {
                return;
            }
            
            if (_container != null)
            {
                _container.Container.OnContentsChanged -= ContainerContentsChanged;
            }
            
            newContainer.Container.OnContentsChanged += ContainerContentsChanged;
            _container = newContainer;
        }

        private void ContainerContentsChanged(Container _, IEnumerable<Item> items, IEnumerable<Item> newItems, ContainerChangeType type)
        {
            if (type != ContainerChangeType.Move)
            {
                UpdateDisplay();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Inventory.ClientInteractWithSingleSlot(_container);

            // When receiving a click on one of the hands of the UI, change the current active hand with the one clicked.
            if (eventData.pointerPress.name is "HandRight(Clone)" or "HandLeft(Clone)")
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