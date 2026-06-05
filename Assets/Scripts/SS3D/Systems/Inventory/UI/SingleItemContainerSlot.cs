using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interfaces;
using SS3D.Systems.Inventory.Items;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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
        /// Optional reference to a stack count label. If null, one is created dynamically.
        /// </summary>
        [SerializeField]
        private TMP_Text _stackCountLabel;

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

            if (_stackCountLabel == null && ItemDisplay != null)
            {
                _stackCountLabel = CreateStackCountLabel(ItemDisplay.transform);
            }

            if (_container.Items.Count() > 0)
            {
                UpdateStackCount(_container.Items.First());
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
            Item item = display.Item;

            if (!_container.CanContainItem(display.Item))
            {
                return;
            }
            if (item.Container != null && !item.Container.CanRemoveItem(item))
            {
                return;
            }
            // listen to container change and update display eventually.
            display.ShouldDrop = true;
			display.MakeVisible(false);
            Inventory.ClientTransferItem(display.Item, Vector2Int.zero, Container);
        }

        /// <summary>
        /// Change the displayed sprite inside the slot.
        /// </summary>
        private void UpdateDisplay()
        {
            if (ItemDisplay == null) return;

            var item = _container.Items.FirstOrDefault();
			ItemDisplay.Item = item;
			ItemDisplay.MakeVisible(true);

            UpdateStackCount(item);
		}

        /// <summary>
        /// Show or hide the stack count label based on the current item.
        /// Only displayed when the item has more than 1 in a stack.
        /// </summary>
        private void UpdateStackCount(Item item)
        {
            if (_stackCountLabel == null)
            {
                return;
            }

            if (item != null && item.IsStackable)
            {
                Stackable stackable = item.GetComponent<Stackable>();
                if (stackable != null && stackable.AmountInStack > 1)
                {
                    _stackCountLabel.text = $"x{stackable.AmountInStack}";
                    _stackCountLabel.gameObject.SetActive(true);
                    return;
                }
            }

            _stackCountLabel.gameObject.SetActive(false);
        }

        /// <summary>
        /// Creates a TextMeshPro label for displaying stack count, positioned at the bottom-right
        /// corner of the item display. Used when no label is assigned in the prefab.
        /// </summary>
        private static TMP_Text CreateStackCountLabel(Transform parent)
        {
            GameObject labelObj = new("StackCountLabel", typeof(RectTransform));
            labelObj.transform.SetParent(parent, false);
            TMP_Text label = labelObj.AddComponent<TextMeshProUGUI>();
            label.fontSize = 14;
            label.alignment = TextAlignmentOptions.BottomRight;
            label.color = Color.white;

            RectTransform rectTransform = label.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(1f, 0f);

            return label;
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
            UpdateDisplay();
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
