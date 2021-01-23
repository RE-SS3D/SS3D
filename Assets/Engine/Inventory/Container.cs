using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// Stores items in a 2 dimensional container
    /// </summary>
    public class Container
    {
        /// <summary>
        /// The size of this container
        /// </summary>
        public Vector2Int Size;

        /// <summary>
        /// Filters for this container
        /// </summary>
        public readonly List<Filter> Filters = new List<Filter>();
        
        /// <summary>
        /// An optional reference to an attached container
        /// </summary>
        public AttachedContainer AttachedTo { get; set; }

        private readonly List<StoredItem> items = new List<StoredItem>();
        private readonly object modificationLock = new object();

        public delegate void ContainerContentsHandler(Container container, IEnumerable<Item> items,
            ContainerChangeType type);

        /// <summary>
        /// Called when the contents of the container change
        /// </summary>
        public event ContainerContentsHandler ContentsChanged;

        /// <summary>
        /// Is this container empty
        /// </summary>
        public bool Empty => ItemCount == 0;
        
        /// <summary>
        /// How many items are in this container
        /// </summary>
        public int ItemCount => items.Count;
        
        /// <summary>
        /// The items stored in this container
        /// </summary>
        public IEnumerable<Item> Items => items.Select(x => x.Item);

        /// <summary>
        /// The items stored in this container, including information on how they are stored
        /// </summary>
        public List<StoredItem> StoredItems => items;

        /// <summary>
        /// The last time the contents of this container were changed
        /// </summary>
        public float LastModification { get; private set; }

        /// <summary>
        /// Places an item into this container in the first available position
        /// </summary>
        /// <param name="item">The item to place</param>
        /// <returns>If the item was added</returns>
        public bool AddItem(Item item)
        {
            if (ContainsItem(item))
            {
                return true;
            }

            if (!CouldStoreItem(item))
            {
                return false;
            }

            Vector2Int itemSize = item.Size;
            int maxX = Size.x - itemSize.x;
            int maxY = Size.y - itemSize.y;

            // TODO: Use a more efficient algorithm
            for (int y = 0; y < maxY; y++)
            {
                for (int x = 0; x < maxX; x++)
                {
                    Vector2Int itemPosition = new Vector2Int(x, y);
                    if (AddItem(item, itemPosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to add an item at the specified position
        /// </summary>
        /// <param name="storedItem">The item to add</param>
        /// <param name="position">The target position in the container</param>
        /// <returns>If the item was added</returns>
        public bool AddItem(Item item, Vector2Int position)
        {
            int itemIndex = FindItem(item);
            if (itemIndex != -1)
            {
                StoredItem existingItem = StoredItems[itemIndex];
                // Try to move existing item
                if (existingItem.Position != position)
                {
                    if (IsAreaFreeExcluding(new RectInt(position, item.Size), item))
                    {
                        StoredItems[itemIndex] = new StoredItem(item, position);
                        OnContainerChanged(new[] {item}, ContainerChangeType.Move);
                        return true;
                    }

                    return false;
                }

                // Item at same position, nothing to do
                return true;
            }

            if (!CouldStoreItem(item))
            {
                return false;
            }

            bool wasAdded = false;
            lock (modificationLock)
            {
                if (IsAreaFree(new RectInt(position, item.Size)))
                {
                    AddItemUnchecked(item, position);
                    wasAdded = true;
                }
            }

            if (wasAdded)
            {
                item.SetContainer(this, true, false);
                OnItemAdded(item);
            }

            return wasAdded;
        }

        /// <summary>
        /// Adds an item to the container without any checks (but ensuring there are no duplicates)
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="position">Where the item should go, make sure this position is valid and free!</param>
        private void AddItemUnchecked(Item item, Vector2Int position)
        {
            var newItem = new StoredItem(item, position);

            // Move it if it is already in the container
            if (MoveItemUnchecked(newItem))
            {
                return;
            }

            items.Add(newItem);
            LastModification = Time.time;
        }

        /// <summary>
        /// Adds a stored item without checking any validity
        /// <param name="storedItem">The item to store</param>
        /// </summary>
        public void AddItemUnchecked(StoredItem storedItem)
        {
            AddItemUnchecked(storedItem.Item, storedItem.Position);
        }

        /// <summary>
        /// Add an array of items without performing checks
        /// </summary>
        /// <param name="items"></param>
        public void AddItemsUnchecked(StoredItem[] items)
        {
            foreach (StoredItem storedItem in items)
            {
                AddItemUnchecked(storedItem);
            }
            
            OnContainerChanged(items.Select(x => x.Item), ContainerChangeType.Add);
        }

        /// <summary>
        /// Checks if a given area in the container is free
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <returns>If the given area is free</returns>
        public bool IsAreaFree(RectInt area)
        {
            if (area.xMin < 0 || area.xMax < 0)
            {
                return false;
            }

            if (area.xMax > Size.x || area.yMax > Size.y)
            {
                return false;
            }

            foreach (StoredItem storedItem in items)
            {
                var storedItemPlacement = new RectInt(storedItem.Position, storedItem.Item.Size);
                if (area.Overlaps(storedItemPlacement))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if a given area in the container is free, while excluding an item
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <param name="item">The item to exclude from the check</param>
        /// <returns>If the given area is free</returns>
        public bool IsAreaFreeExcluding(RectInt area, Item item)
        {
            int i = FindItem(item);
            StoredItem storedItem = default;
            if (i != -1)
            {
                storedItem = items[i];
                items[i] = new StoredItem(storedItem.Item, new Vector2Int(100000, 100000));
            }

            bool areaFree = IsAreaFree(area);

            if (i != -1)
            {
                items[i] = storedItem;
            }

            return areaFree;
        }

        /// <summary>
        /// Removes an item from the container
        /// </summary>
        /// <param name="item">The item to remove</param>
        public void RemoveItem(Item item)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i].Item == item)
                {
                    RemoveItemAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Removes multiple items from the container
        /// </summary>
        /// <param name="itemsToRemove">An array of items to remove</param>
        public void RemoveItems(Item[] itemsToRemove)
        {
            foreach (Item itemToRemove in itemsToRemove)
            {
                lock (modificationLock)
                {
                    for (var i = 0; i < items.Count; i++)
                    {
                        StoredItem storedItem = items[i];
                        if (storedItem.Item == itemToRemove)
                        {
                            StoredItems.RemoveAt(i);
                            itemToRemove.SetContainer(null, true, true);
                            break;
                        }
                    }
                }
            }
            
            LastModification = Time.time;
            
            OnContainerChanged(itemsToRemove, ContainerChangeType.Remove);
        }

        /// <summary>
        /// Moves an item without performing validation
        /// </summary>
        /// <param name="item">The item to move</param>
        /// <returns>If the item was moved</returns>
        public bool MoveItemUnchecked(StoredItem item)
        {
            for (var i = 0; i < items.Count; i++)
            {
                StoredItem x = items[i];
                if (x.Item == item.Item)
                {
                    if (x.Position != item.Position)
                    {
                        items[i] = item;
                        LastModification = Time.time;
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Moves multiple items without performing validation
        /// </summary>
        /// <param name="items">The items to move</param>
        public void MoveItemsUnchecked(StoredItem[] items)
        {
            foreach (StoredItem storedItem in items)
            {
                MoveItemUnchecked(storedItem);
            }
            
            OnContainerChanged(items.Select(x => x.Item), ContainerChangeType.Move);
        }

        /// <summary>
        /// Finds an item at a position
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>The item at the position, or null if there is none</returns>
        public Item ItemAt(Vector2Int position)
        {
            foreach (StoredItem storedItem in items)
            {
                var storedItemPlacement = new RectInt(storedItem.Position, storedItem.Item.Size);
                if (storedItemPlacement.Contains(position))
                {
                    return storedItem.Item;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds the position of an item in the container
        /// </summary>
        /// <param name="item">The item to look for</param>
        /// <returns>The item's position or (-1, -1)</returns>
        public Vector2Int PositionOf(Item item)
        {
            foreach (StoredItem storedItem in items)
            {
                if (storedItem.Item == item)
                {
                    return storedItem.Position;
                }
            }
            
            return new Vector2Int(-1, -1);
        }

        /// <summary>
        /// Ensures this container has the same state as the one given, using the least amount of operations
        /// </summary>
        /// <param name="otherContainer">The container to match</param>
        public void Reconcile(Container otherContainer)
        {
            Size = otherContainer.Size;
            
            if (Empty)
            {
                items.AddRange(otherContainer.items);
                OnContainerChanged(otherContainer.Items, ContainerChangeType.Add);
                return;
            }
            
            if (otherContainer.Empty)
            {
                Dump();
                return;
            }
            
            // Loop through all items to find the first index of divergence
            // We can assume that all items after that point have been changed, as items are always inserted at the end
            List<Item> movedItems = new List<Item>();
            int changedIndex = -1;
            for (var i = 0; i < items.Count; i++)
            {
                StoredItem storedItem = items[i];
                StoredItem otherContainerItem = otherContainer.items[i];
                if (storedItem.Item != otherContainerItem.Item)
                {
                    changedIndex = i;
                    break;
                }

                if (storedItem.Position != otherContainerItem.Position)
                {
                    movedItems.Add(storedItem.Item);
                }
            }

            // Invoke move logic if any element has moved
            if (movedItems.Count > 0)
            {
                OnContainerChanged(movedItems, ContainerChangeType.Move);
            }

            // Nothing actually changed
            if (changedIndex == -1)
            {
                return;
            }

            // Remove all items after first divergence
            Item[] removedItems = new Item[items.Count - changedIndex];
            for (var i = changedIndex; i < items.Count;)
            {
                items.RemoveAt(i);
            }
            OnContainerChanged(removedItems.AsEnumerable(), ContainerChangeType.Remove);

            // Add all remaining items
            for (int i = changedIndex; i < otherContainer.ItemCount; i++)
            {
                items.Add(otherContainer.items[i]);
            }
            OnContainerChanged(otherContainer.Items.Skip(changedIndex + 1), ContainerChangeType.Add);
            
            
        }

        private void RemoveItemAt(int index)
        {
            StoredItem item = items[index];
            lock (modificationLock)
            {
                items.RemoveAt(index);
            }

            LastModification = Time.time;
            item.Item.SetContainerUnchecked(null);
            OnItemRemoved(item.Item);
        }

        /// <summary>
        /// Empties the container, removing all items
        /// </summary>
        public void Dump()
        {
            Item[] oldItems = items.Select(x => x.Item).ToArray();
            while (items.Count > 0)
            {
                items[0].Item.Container = null;
            }
            LastModification = Time.time;
            OnContainerChanged(oldItems, ContainerChangeType.Remove);
        }

        /// <summary>
        /// Destroys all items in this container
        /// </summary>
        public void Destroy()
        {
            Item[] oldItems = items.Select(x => x.Item).ToArray();
            while (items.Count > 0)
            {
                items[0].Item.Destroy();
            }

            LastModification = Time.time;
            OnContainerChanged(oldItems, ContainerChangeType.Remove);
        }

        /// <summary>
        /// Checks if this container contains the item
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>If it is in this container</returns>
        public bool ContainsItem(Item item)
        {
            foreach (StoredItem storedItem in items)
            {
                if (storedItem.Item == item)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this item could be stored (traits etc.) without considering size
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CouldStoreItem(Item item)
        {
            foreach (Filter filter in Filters)
            {
                if (!filter.CanStore(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Finds the index of an item
        /// </summary>
        /// <param name="item">The item to look for</param>
        /// <returns>The index of the item or -1 if not found</returns>
        public int FindItem(Item item)
        {
            for (var i = 0; i < items.Count; i++)
            {
                StoredItem storedItem = items[i];
                if (storedItem.Item == item)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnItemAdded(Item item)
        {
            OnContainerChanged(new[] {item}, ContainerChangeType.Add);
        }

        private void OnItemRemoved(Item item)
        {
            OnContainerChanged(new[] {item}, ContainerChangeType.Remove);
        }

        protected virtual void OnContainerChanged(IEnumerable<Item> changedItems, ContainerChangeType type)
        {
            ContentsChanged?.Invoke(this, changedItems, type);
        }

        public struct StoredItem : IEquatable<StoredItem>
        {
            public readonly Item Item;
            public readonly Vector2Int Position;

            public StoredItem(Item item, Vector2Int position)
            {
                Item = item;
                Position = position;
            }

            public bool Equals(StoredItem other)
            {
                return Equals(Item, other.Item) && Position.Equals(other.Position);
            }

            public override bool Equals(object obj)
            {
                return obj is StoredItem other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Item != null ? Item.GetHashCode() : 0) * 397) ^ Position.GetHashCode();
                }
            }
        }

        public enum ContainerChangeType
        {
            Add,
            Remove,
            Move
        }
    }
}