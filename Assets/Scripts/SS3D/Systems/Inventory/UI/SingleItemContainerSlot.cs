using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interfaces;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace SS3D.Systems.Inventory.UI
{
    /// <summary>
    /// SingleItemContainerSlot allows displaying the content of a container that contain a single item in the UI.
    /// It handles updating the inventory when an item is dropped on it, and it changes the displayed sprite accordingly.
    /// As of now, it's only use is displaying the content of the containers on the hands of the player in the UI slots.
    /// </summary>
    public class SingleItemContainerSlot : InventoryDisplayElement, IPointerClickHandler, ISlotProvider
    {
        [FormerlySerializedAs("ItemDisplay")]
        [SerializeField]
        private ItemDisplay _itemDisplay;

        [FormerlySerializedAs("ContainerType")]
        [SerializeField]
        private ContainerType _containerType;

        /// <summary>
        /// The container displayed by this slot.
        /// </summary>
        private AttachedContainer _container;

        public AttachedContainer Container
        {
            get => _container;
            set => UpdateContainer(value);
        }

        public ContainerType ContainerType => _containerType;

        public void OnPointerClick(PointerEventData eventData)
        {
            Inventory.ClientInteractWithContainerSlot(_container, new Vector2Int(0, 0));
        }

        public GameObject GetCurrentGameObjectInSlot() => _itemDisplay.Item == null ? null : _itemDisplay.Item.gameObject;

        /// <summary>
        /// When dragging and dropping an item sprite over this slot, update the inventory
        /// and the displayed sprite inside the slot.
        /// Does nothing if the slot already has an item.
        /// </summary>
        protected override void OnItemDisplayDrop(ItemDisplay display)
        {
            Item item = display.Item;

            if (!_container.CanContainItem(display.Item) || (item.Container != null && !item.Container.CanRemoveItem(item)))
            {
                _itemDisplay.ResetPositionAndParent();
                return;
            }

            if (item.Container != null && !item.Container.CanRemoveItem(item))
            {
                return;
            }

            // listen to container change and update display eventually.
            display.MakeVisible(false);
            Inventory.ClientTransferItem(display.Item, Vector2Int.zero, Container);
        }

        protected void Start()
        {
            Assert.IsNotNull(_itemDisplay);
            _itemDisplay.OnDragOutOfUI += HandleDragOutOfUI;
            if (Container != null)
            {
                UpdateContainer(Container);
            }

            if (_container.Items.Any())
            {
                _itemDisplay.Item = _container.Items.First();
            }
        }

        protected void OnDestroy()
        {
            Destroy(_itemDisplay);
        }

        private void HandleDragOutOfUI(object sender, EventArgs e)
        {
            DropItemOutside(_itemDisplay.Item);
            _itemDisplay.MakeVisible(false);
            _itemDisplay.ResetPositionAndParent();
        }

        /// <summary>
        /// Change the displayed sprite inside the slot.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_itemDisplay == null)
            {
                return;
            }

            Item item = _container.Items.FirstOrDefault();
            _itemDisplay.Item = item;
            _itemDisplay.MakeVisible(true);
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
                _container.OnClientContentsChanged -= ContainerContentsChanged;
            }

            newContainer.OnClientContentsChanged += ContainerContentsChanged;
            _container = newContainer;
        }

        private void ContainerContentsChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
            if (type != ContainerChangeType.Move)
            {
                UpdateDisplay();
            }
        }
    }
}
