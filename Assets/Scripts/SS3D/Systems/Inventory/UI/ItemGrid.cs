using Coimbra;
using JetBrains.Annotations;
using SS3D.Systems.Inventory.Containers;
using SS3D.Systems.Inventory.Interfaces;
using SS3D.Systems.Inventory.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace SS3D.Systems.Inventory.UI
{
    public class ItemGrid : InventoryDisplayElement, IPointerClickHandler, ISlotProvider
    {
        public event Action<Vector2Int> OnPointerClickSlot;

        public event Action<Item, Vector2Int> OnItemDrop;

        private readonly List<ItemGridItem> _gridItems = new();

        /// <summary>
        /// A prefab for each slot in this grid
        /// </summary>
        [FormerlySerializedAs("ItemSlotPrefab")]
        [SerializeField]
        private GameObject _itemSlotPrefab;

        /// <summary>
        /// A prefab for displaying items inside the container
        /// </summary>
        [FormerlySerializedAs("ItemDisplayPrefab")]
        [SerializeField]
        private GameObject _itemDisplayPrefab;

        private GridLayoutGroup _gridLayout;

        private Vector2Int _gridSize;

        private ItemDisplay _droppedDisplay;

        public void Init(Vector2Int gridSize, List<StoredItem> storedItems)
        {
            _gridSize = gridSize;

            // todo define serialized field and set in editor
            if (_gridLayout == null)
            {
                _gridLayout = GetComponentInChildren<GridLayoutGroup>();
            }

            Transform parent = _gridLayout.transform;
            int count = gridSize.x * gridSize.y;
            for (int i = 0; i < count; i++)
            {
                Instantiate(_itemSlotPrefab, parent);
            }

            StartCoroutine(DisplayInitialItems(storedItems));
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

            float x = (_gridSize.x * _gridLayout.cellSize.x) + (_gridSize.x * _gridLayout.spacing.x);
            float y = (_gridSize.y * _gridLayout.cellSize.y) + (_gridSize.y * _gridLayout.spacing.y);
            return new(x, y);
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
                OnPointerClickSlot?.Invoke(GetSlotPosition(eventData.position));
            }
        }

        [CanBeNull]
        public GameObject GetCurrentGameObjectInSlot()
        {
            Vector2Int slotPosition = GetSlotPosition(Mouse.current.position.ReadValue());
            return ItemAt(slotPosition) == null ? null : ItemAt(slotPosition).gameObject;
        }

        /// <summary>
        /// Instantiate a sprite at the right location in the grid.
        /// </summary>
        public void CreateItemDisplay(Item item, Vector2Int position, bool itemMovedInsideGrid = false)
        {
            // avoid creating the same item sprite multiple times. Except when it's moved around in the container.
            // In this case two instances need to exist on the same frame so we allow it.
            foreach (ItemGridItem itemSprite in _gridItems)
            {
                if (itemSprite.Item == item && !itemMovedInsideGrid)
                {
                    return;
                }
            }

            GameObject o = Instantiate(_itemDisplayPrefab, transform);
            ItemGridItem itemSpriteOnGrid = o.GetComponent<ItemGridItem>();
            itemSpriteOnGrid.OnDragOutOfUI += HandleDragOutOfUI;

            Vector2 cellSize = _gridLayout.cellSize;
            o.GetComponent<RectTransform>().sizeDelta = new Vector2(cellSize.x, cellSize.y);

            itemSpriteOnGrid.Item = item;
            MoveToSlot(itemSpriteOnGrid, position);

            _gridItems.Add(itemSpriteOnGrid);
        }

        public void RemoveItemDisplay(Item item)
        {
            foreach (ItemGridItem gridItem in _gridItems)
            {
                if (gridItem.Item != item)
                {
                    continue;
                }

                gridItem.OnDragOutOfUI -= HandleDragOutOfUI;
                _gridItems.Remove(gridItem);
                gridItem.gameObject.Dispose(true);
                return;
            }
        }

        public void MoveItemDisplay(Item item, Vector2Int slotPosition)
        {
            foreach (ItemGridItem gridItem in _gridItems)
            {
                if (gridItem.Item != item)
                {
                    continue;
                }

                MoveToSlot(gridItem, slotPosition);
                return;
            }
        }

        public void ResetDroppedItemDisplayPositionAndParent()
        {
            if (_droppedDisplay)
            {
                _droppedDisplay.ResetPositionAndParent();
            }
        }

        /// <summary>
        /// When an item display is dropped on this grid, this compute in which slot of the grid the sprite should be displayed.
        /// Does nothing if the area of drop is not free. Does nothing if the mouse is outside the slots.
        /// This use the mouse position to decide in which slot the sprite should go.
        /// </summary>
        /// <param name="display"></param>
        protected override void OnItemDisplayDrop(ItemDisplay display)
        {
            Item item = display.Item;
            _droppedDisplay = display;

            Vector3 mousePosition = Input.mousePosition;
            Vector2 position = GetSlotPositionExact(mousePosition);
            Vector2Int slot = new(Mathf.RoundToInt(position.x - (1 / 2f)), Mathf.RoundToInt(position.y - (1 / 2f)));

            OnItemDrop?.Invoke(item, slot);

            // We make it not visible the time it is transfered to another slot, to avoid seeing the sprite flickering.
            // display.MakeVisible(false);
            // display.ShouldDrop = true;
        }

        private void HandleDragOutOfUI(object sender, EventArgs e)
        {
            DropItemOutside(((ItemDisplay)sender).Item);
        }

        /// <summary>
        /// Finds an item at a position
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>The item at the position, or null if there is none</returns>
        private Item ItemAt(Vector2Int position)
        {
            foreach (ItemGridItem storedItem in _gridItems)
            {
                if (storedItem.GridPosition == position)
                {
                    return storedItem.Item;
                }
            }

            return null;
        }

        /// <summary>
        /// Create item displays for items already contained in the container when viewing it.
        /// </summary>
        private IEnumerator DisplayInitialItems(List<StoredItem> storedItems)
        {
            // For some reason, has to be delayed to end of frame to work.
            yield return new WaitForEndOfFrame();

            foreach (StoredItem storedItem in storedItems)
            {
                Vector2Int position = storedItem.Position;
                CreateItemDisplay(storedItem.Item, position);
            }
        }

        /// <summary>
        /// Get a slot position given a screen position, without rounding
        /// </summary>
        private Vector2 GetSlotPositionExact(Vector2 screenPosition)
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
        private Vector2Int GetSlotPosition(Vector2 screenPosition)
        {
            Vector2 exact = GetSlotPositionExact(screenPosition);

            return new Vector2Int(Mathf.FloorToInt(exact.x), Mathf.FloorToInt(exact.y));
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

            int slotIndex = (position.y * _gridSize.x) + position.x;
            Transform slot = _gridLayout.transform.GetChild(slotIndex);
            objectToMove.localPosition = slot.localPosition;
            gridItem.MakeVisible(true);
        }
    }
}
