using System;
using System.Collections;
using System.Collections.Generic;
using Coimbra;
using FishNet;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interfaces;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.UI
{
    public class ItemGrid : InventoryDisplayElement, IPointerClickHandler, IDropHandler, ISlotProvider
    {
        /// <summary>
        /// A prefab for each slot in this grid
        /// </summary>
        public GameObject ItemSlotPrefab;
        /// <summary>
        /// A prefab for displaying items inside the container
        /// </summary>
        public GameObject ItemDisplayPrefab;
        /// <summary>
        /// The container this grid displays
        /// </summary>
        public AttachedContainer AttachedContainer;

        private GridLayoutGroup _gridLayout;
        private readonly List<ItemGridItem> _gridItems = new();

        public void RemoveGridItem(ItemGridItem item)
        {
            _gridItems.Remove(item);
        }

        private void Start()
        {
            if (_gridLayout == null)
            {
                _gridLayout = GetComponentInChildren<GridLayoutGroup>();
            }

            Transform parent = _gridLayout.transform;
            Vector2Int containerSize = AttachedContainer.Size;
            int count = containerSize.x * containerSize.y;
            for (int i = 0; i < count; i++)
            {
                Instantiate(ItemSlotPrefab, parent);
            }

            if (AttachedContainer.ItemCount > 0)
            {
                StartCoroutine(DisplayInitialItems());
            }

            AttachedContainer.OnContentsChanged += ContainerOnContentsChanged;
        }

        private void OnDestroy()
        {
            AttachedContainer.OnContentsChanged -= ContainerOnContentsChanged;
        }

        /// <summary>
        /// Create item displays for items already contained in the container when viewing it. 
        /// </summary>
        private IEnumerator DisplayInitialItems()
        {
            // For some reason, has to be delayed to end of frame to work.
            yield return new WaitForEndOfFrame();
            foreach (Item item in AttachedContainer.Items)
            {
                Vector2Int position = AttachedContainer.PositionOf(item);
                CreateItemDisplay(item, position);
            }
        }

        /// <summary>
        /// When the container change, change the display of items inside it.
        /// Either add a display, remove a display or move a display to another slot.
        /// </summary>
        private void ContainerOnContentsChanged(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type)
        {
			Vector2Int position = new Vector2Int(0, 0);

			switch (type)
            {
                case ContainerChangeType.Add:

                    if (newItem == null) return;
					position = container.PositionOf(newItem);
					CreateItemDisplay(newItem, position);
					break;

                case ContainerChangeType.Remove:

                        if (oldItem == null) return;
                        for (int i = 0; i < _gridItems.Count; i++)
                        {
                            ItemGridItem gridItem = _gridItems[i];
                            if (gridItem.Item != oldItem)
                            {
                                continue;
                            }
                            _gridItems.RemoveAt(i);
                            gridItem.gameObject.Dispose(true);
                            break;
                        }
						break;

                case ContainerChangeType.Move:

                    if (newItem == null) return;
                    foreach (ItemGridItem gridItem in _gridItems)
                    {
                        if (gridItem.Item == newItem)
                        {
                            position = container.PositionOf(newItem);
                            MoveToSlot(gridItem, position);
                            break;
                        }
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        /// <summary>
        /// Get a slot position given a screen position, without rounding
        /// </summary>
        public Vector2 GetSlotPositionExact(Vector2 screenPosition)
        {
            Vector3[] corners = new Vector3[4];
            _gridLayout.GetComponent<RectTransform>().GetWorldCorners(corners);
            Vector2 localPoint = new Vector2(screenPosition.x - corners[1].x, corners[1].y - screenPosition.y);
            Vector3 scale = _gridLayout.transform.localToWorldMatrix.lossyScale;
            Vector2 cellSize = _gridLayout.cellSize;
			// TODO : for optimal precision, should include spacing between cells ?
            float x = localPoint.x / (cellSize.x * scale.x);
            float y = localPoint.y / (cellSize.y * scale.y);

            return new Vector2(x, y);
        }

        /// <summary>
        /// Get a slot position given a screen position
        /// </summary>
        public Vector2Int GetSlotPosition(Vector2 screenPosition)
        {
            Vector2 exact = GetSlotPositionExact(screenPosition);
            return new Vector2Int(Mathf.FloorToInt(exact.x), Mathf.FloorToInt(exact.y));
        }

		/// <summary>
		/// Get the dimension of the grid in pixels, including cell sizes and spacing.
		/// </summary>
        public Vector2 GetGridDimensions()
        {
            if (_gridLayout == null)
            {
                _gridLayout = GetComponentInChildren<GridLayoutGroup>();
            }

            Vector2Int size = AttachedContainer.Size;
            float x = size.x * _gridLayout.cellSize.x + size.x * _gridLayout.spacing.x;
            float y = size.y * _gridLayout.cellSize.y + size.y * _gridLayout.spacing.y;
            return new Vector2(x, y);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            GameObject clicked = eventData.rawPointerPress;
            if (clicked == null)
            {
                return;
            }


            Transform parent = clicked.transform.parent;
            if (parent == _gridLayout.transform || parent == transform)
            {
                Vector2Int slotPosition = GetSlotPosition(eventData.position);
                Inventory.ClientInteractWithContainerSlot(AttachedContainer, slotPosition);
            }
        }

        /// <summary>
        /// When an item display is dropped on this grid, this compute in which slot of the grid the sprite should be displayed.
        /// Does nothing if the area of drop is not free. Does nothing if the mouse is outside the slots.
		/// This use the mouse position to decide in which slot the sprite should go.
        /// </summary>
        /// <param name="display"></param>
        public override void OnItemDisplayDrop(ItemDisplay display)
        {
            Item item = display.Item;

			Vector3 mousePosition = Input.mousePosition;
            Vector2 position = GetSlotPositionExact(mousePosition);

            Vector2Int slot = new(Mathf.RoundToInt(position.x - 1 / 2f), Mathf.RoundToInt(position.y - 1 / 2f));

			if (!AttachedContainer.CanContainItemAtPosition(item, slot))
			{
				return;
			}

			// We make it not visible the time it is transfered to another slot, to avoid seeing the sprite flickering.
			display.MakeVisible(false);
			display.ShouldDrop = true;
            Inventory.ClientTransferItem(item, slot, AttachedContainer);
        }

		/// <summary>
		/// Move an item sprite at a given position on the grid.
		/// </summary>
        private void MoveToSlot(ItemGridItem gridItem, Vector2Int position)
        {
			Transform objectToMove = gridItem.transform;

			if (objectToMove.parent != transform)
            {
                objectToMove.SetParent(transform, false);
            }
            Vector2Int containerSize = AttachedContainer.Size;
            int slotIndex = position.y * containerSize.x + position.x;
            Transform slot = _gridLayout.transform.GetChild(slotIndex);
            objectToMove.localPosition = slot.localPosition;
			gridItem.MakeVisible(true);
        }

		/// <summary>
		/// Instantiate a sprite at the right location in the grid.
		/// </summary>
        private void CreateItemDisplay(Item item, Vector2Int position, bool ItemMovedInsideGrid = false)
        {
            // avoid creating the same item sprite multiple times. Except when it's moved around in the container.
            // In this case two instances need to exist on the same frame so we allow it.
            foreach (ItemGridItem itemSprite in _gridItems)
            {
                if (itemSprite.Item == item && !ItemMovedInsideGrid) return;
            }

            GameObject o = Instantiate(ItemDisplayPrefab, transform);
            ItemGridItem itemSpriteOnGrid = o.GetComponent<ItemGridItem>();

            Vector2 cellSize = _gridLayout.cellSize;
            o.GetComponent<RectTransform>().sizeDelta = new Vector2(cellSize.x, cellSize.y);

            itemSpriteOnGrid.Item = item;
            MoveToSlot(itemSpriteOnGrid, position);

            _gridItems.Add(itemSpriteOnGrid);
        }

        public GameObject GetCurrentGameObjectInSlot()
        {
            Vector2Int slotPosition = GetSlotPosition(Mouse.current.position.ReadValue());
            return AttachedContainer.ItemAt(slotPosition) == null ? null : AttachedContainer.ItemAt(slotPosition).gameObject;
        }

    }
}