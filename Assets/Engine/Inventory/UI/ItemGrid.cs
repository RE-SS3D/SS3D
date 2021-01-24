using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SS3D.Engine.Inventory.UI
{
    public class ItemGrid : InventoryDisplayElement, IPointerClickHandler, IDropHandler
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

        private GridLayoutGroup gridLayout;
        private List<ItemGridItem> gridItems = new List<ItemGridItem>();

        public void RemoveGridItem(ItemGridItem item)
        {
            gridItems.Remove(item);
        }
        
        private void Start()
        {
            if (gridLayout == null)
            {
                gridLayout = GetComponentInChildren<GridLayoutGroup>();
            }

            Transform parent = gridLayout.transform;
            Container container = AttachedContainer.Container;
            Vector2Int containerSize = container.Size;
            int count = containerSize.x * containerSize.y;
            for (var i = 0; i < count; i++)
            {
                Instantiate(ItemSlotPrefab, parent);
            }

            if (container.ItemCount > 0)
            {
                StartCoroutine(DisplayInitialItems());
            }

            container.ContentsChanged += ContainerOnContentsChanged;
        }

        private void OnDestroy()
        {
            AttachedContainer.Container.ContentsChanged -= ContainerOnContentsChanged;
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

        private void ContainerOnContentsChanged(Container container, IEnumerable<Item> items, Container.ContainerChangeType type)
        {
            switch (type)
            {
                case Container.ContainerChangeType.Add:
                    foreach (Item item in items)
                    {
                        Vector2Int position = container.PositionOf(item);
                        CreateItemDisplay(item, position);
                    }
                    break;
                case Container.ContainerChangeType.Remove:
                    foreach (Item item in items)
                    {
                        for (var i = 0; i < gridItems.Count; i++)
                        {
                            ItemGridItem gridItem = gridItems[i];
                            if (gridItem.Item == item)
                            {
                                gridItems.RemoveAt(i);
                                Destroy(gridItem.gameObject);
                                break;
                            }
                        }
                    }
                    break;
                case Container.ContainerChangeType.Move:
                    foreach (Item item in items)
                    {
                        foreach (ItemGridItem gridItem in gridItems)
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
            gridLayout.GetComponent<RectTransform>().GetWorldCorners(corners);
            Vector2 localPoint = new Vector2(screenPosition.x - corners[1].x, corners[1].y - screenPosition.y);
            Vector3 scale = gridLayout.transform.localToWorldMatrix.lossyScale;
            var cellSize = gridLayout.cellSize;
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
            if (gridLayout == null)
            {
                gridLayout = GetComponentInChildren<GridLayoutGroup>();
            }
            
            Vector2Int size = AttachedContainer.Container.Size;
            float x = size.x * gridLayout.cellSize.x + size.x * gridLayout.spacing.x;
            float y = size.y * gridLayout.cellSize.y + size.y * gridLayout.spacing.y;
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
            if (parent == gridLayout.transform || parent == transform)
            {
                Vector2Int slotPosition = GetSlotPosition(eventData.position);
                Inventory.ClientInteractWithContainerSlot(AttachedContainer, slotPosition);
            }
        }
        
        public override void OnItemDrop(ItemDisplay display)
        {
            Item item = display.Item;
            Vector2Int size = item.Size;
            Vector3 dragPosition = display.transform.position;
            
            // Get item center position
            var rect = display.GetComponent<RectTransform>().rect;
            Vector2 rectCenter = new Vector2(rect.width / 2, rect.height / 2);
            var position = GetSlotPositionExact(new Vector2(dragPosition.x + rectCenter.x, dragPosition.y - rectCenter.y));
            
            // Offset slot by item dimensions
            var slot = new Vector2Int( Mathf.RoundToInt(position.x - size.x / 2f), Mathf.RoundToInt(position.y - size.y / 2f));
            
            if (!AttachedContainer.Container.IsAreaFreeExcluding(new RectInt(slot, size), item))
            {
                return;
            }

            display.DropAccepted = true;
            CreateItemDisplay(display.Item, slot);
            Inventory.ClientTransferItem(item, slot, AttachedContainer);
        }
        
        private void MoveToSlot(Transform transform, Vector2Int position)
        {
            if (transform.parent != this.transform)
            {
                transform.SetParent(this.transform, false);
            }
            Vector2Int containerSize = AttachedContainer.Container.Size;
            int slotIndex = position.y * containerSize.x + position.x;
            Transform slot = gridLayout.transform.GetChild(slotIndex);
            transform.localPosition = slot.localPosition;
        }

        private void CreateItemDisplay(Item item, Vector2Int position)
        {
            GameObject o = Instantiate(ItemDisplayPrefab, transform);
            var gridItem = o.GetComponent<ItemGridItem>();

            Vector2Int itemSize = item.Size;
            Vector2 cellSize = gridLayout.cellSize;
            o.GetComponent<RectTransform>().sizeDelta = new Vector2(itemSize.x * cellSize.x, itemSize.y * cellSize.y);
            
            gridItem.Item = item;
            MoveToSlot(o.transform, position);
            
            gridItems.Add(gridItem);
        }
    }
}