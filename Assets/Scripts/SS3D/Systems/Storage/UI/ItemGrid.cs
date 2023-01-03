using System;
using System.Collections;
using System.Collections.Generic;
using Coimbra;
using SS3D.Systems.Storage.Containers;
using SS3D.Systems.Storage.Interfaces;
using SS3D.Systems.Storage.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Systems.Storage.UI
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
            Container container = AttachedContainer.Container;
            Vector2Int containerSize = container.Size;
            int count = containerSize.x * containerSize.y;
            for (int i = 0; i < count; i++)
            {
                Instantiate(ItemSlotPrefab, parent);
            }

            if (container.ItemCount > 0)
            {
                StartCoroutine(DisplayInitialItems());
            }

            container.OnContentsChanged += ContainerOnContentsChanged;
        }

        private void OnDestroy()
        {
            AttachedContainer.Container.OnContentsChanged -= ContainerOnContentsChanged;
        }

        private IEnumerator DisplayInitialItems()
        {
            // thanks Unity UI
            yield return new WaitForEndOfFrame();
            Container container = AttachedContainer.Container;
            foreach (Item item in container.Items)
            {
                Vector2Int position = container.PositionOf(item);
                CreateItemDisplay(item, position);
            }
        }

        private void ContainerOnContentsChanged(Container container, IEnumerable<Item> items, IEnumerable<Item> newItems, ContainerChangeType type)
        {
            switch (type)
            {
                case ContainerChangeType.Add:
                    foreach (Item item in items)
                    {
                        Vector2Int position = container.PositionOf(item);
                        CreateItemDisplay(item, position);
                    }
                    break;
                case ContainerChangeType.Remove:
                    foreach (Item item in items)
                    {
                        for (var i = 0; i < _gridItems.Count; i++)
                        {
                            ItemGridItem gridItem = _gridItems[i];
                            if (gridItem.Item != item)
                            {
                                continue;
                            }

                            _gridItems.RemoveAt(i);
                            gridItem.Destroy();
                            break;
                        }
                    }
                    break;
                case ContainerChangeType.Move:
                    foreach (Item item in items)
                    {
                        foreach (ItemGridItem gridItem in _gridItems)
                        {
                            if (gridItem.Item == item)
                            {
                                Vector2Int position = container.PositionOf(item);
                                MoveToSlot(gridItem.transform, position);
                                break;
                            }
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

        public Vector2 GetGridDimensions()
        {
            if (_gridLayout == null)
            {
                _gridLayout = GetComponentInChildren<GridLayoutGroup>();
            }
            
            Vector2Int size = AttachedContainer.Container.Size;
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
        
        public override void OnItemDisplayDrop(ItemDisplay display)
        {
            Item item = display.Item;
            Vector2Int size = item.Size;
            Vector3 dragPosition = display.transform.position;
            
            // Get item center position
            Rect rect = display.GetComponent<RectTransform>().rect;
            Vector2 rectCenter = new(rect.width / 2, rect.height / 2);
            Vector2 position = GetSlotPositionExact(new Vector2(dragPosition.x + rectCenter.x, dragPosition.y - rectCenter.y));
            
            // Offset slot by item dimensions
            Vector2Int slot = new( Mathf.RoundToInt(position.x - size.x / 2f), Mathf.RoundToInt(position.y - size.y / 2f));
            
            if (!AttachedContainer.Container.IsAreaFreeExcluding(new RectInt(slot, size), item))
            {
                return;
            }

            display.ShouldDrop = true;
            CreateItemDisplay(display.Item, slot);
            Inventory.ClientTransferItem(item, slot, AttachedContainer);
        }
        
        private void MoveToSlot(Transform objectToMove, Vector2Int position)
        {
            if (objectToMove.parent != transform)
            {
                objectToMove.SetParent(transform, false);
            }
            Vector2Int containerSize = AttachedContainer.Container.Size;
            int slotIndex = position.y * containerSize.x + position.x;
            Transform slot = _gridLayout.transform.GetChild(slotIndex);
            objectToMove.localPosition = slot.localPosition;
        }

        private void CreateItemDisplay(Item item, Vector2Int position)
        {
            GameObject o = Instantiate(ItemDisplayPrefab, transform);
            ItemGridItem gridItem = o.GetComponent<ItemGridItem>();

            Vector2Int itemSize = item.Size;
            Vector2 cellSize = _gridLayout.cellSize;
            o.GetComponent<RectTransform>().sizeDelta = new Vector2(itemSize.x * cellSize.x, itemSize.y * cellSize.y);
            
            gridItem.Item = item;
            MoveToSlot(o.transform, position);
            
            _gridItems.Add(gridItem);
        }

		public GameObject GetCurrentGameObjectInSlot()
		{
			Vector2Int slotPosition = GetSlotPosition(new Vector2(Input.mousePosition.x, Input.mousePosition.y));
			Container container = AttachedContainer.Container;
			return container.ItemAt(slotPosition) == null ? null : container.ItemAt(slotPosition).gameObject;
		}
		
    }
}