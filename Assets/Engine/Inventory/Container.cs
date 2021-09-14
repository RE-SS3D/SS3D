using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SS3D.Content;


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

        private readonly List<StoredIContainerizable> containerizables = new List<StoredIContainerizable>();
        private readonly object modificationLock = new object();

        public delegate void ContainerContentsHandler(Container container, IEnumerable<IContainerizable> items,
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
        public int ItemCount => containerizables.Count;

        /// <summary>
        /// The items stored in this container
        /// </summary>
        public IEnumerable<IContainerizable> Containerizables => containerizables.Select(x => x.Item);

        /// <summary>
        /// The items stored in this container, including information on how they are stored
        /// </summary>
        public List<StoredIContainerizable> StoredContainerizables => containerizables;

        /// <summary>
        /// The last time the contents of this container were changed
        /// </summary>
        public float LastModification { get; private set; }

        /// <summary>
        /// Places an item into this container in the first available position
        /// </summary>
        /// <param name="item">The item to place</param>
        /// <returns>If the item was added</returns>
        public bool AddItem(IContainerizable containerizable)
        {
            if (ContainsItem(containerizable))
            {
                return true;
            }

            if (!CouldStoreItem(containerizable))
            {
                return false;
            }

            Vector2Int containerizableSize = containerizable.Size;
            int maxX = Size.x - containerizableSize.x;
            int maxY = Size.y - containerizableSize.y;

            // TODO: Use a more efficient algorithm
            for (int y = 0; y <= maxY; y++)
            {
                for (int x = 0; x <= maxX; x++)
                {
                    Vector2Int containerizablePosition = new Vector2Int(x, y);
                    if (AddItem(containerizable, containerizablePosition))
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
        public bool AddItem(IContainerizable item, Vector2Int position)
        {
            int itemIndex = FindItem(item);
            if (itemIndex != -1)
            {
                StoredIContainerizable existingItem = StoredContainerizables[itemIndex];
                // Try to move existing item
                if (existingItem.Position != position)
                {
                    if (IsAreaFreeExcluding(new RectInt(position, item.Size), item))
                    {
                        StoredContainerizables[itemIndex] = new StoredIContainerizable(item, position);
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
        private void AddItemUnchecked(IContainerizable item, Vector2Int position)
        {
            var newItem = new StoredIContainerizable(item, position);

            // Move it if it is already in the container
            if (MoveItemUnchecked(newItem))
            {
                return;
            }

            containerizables.Add(newItem);
            LastModification = Time.time;
        }

        /// <summary>
        /// Adds a stored item without checking any validity
        /// <param name="storedItem">The item to store</param>
        /// </summary>
        public void AddItemUnchecked(StoredIContainerizable storedItem)
        {
            AddItemUnchecked(storedItem.Item, storedItem.Position);
        }

        /// <summary>
        /// Add an array of items without performing checks
        /// </summary>
        /// <param name="items"></param>
        public void AddItemsUnchecked(StoredIContainerizable[] containerizables)
        {
            foreach (StoredIContainerizable storedItem in containerizables)
            {
                AddItemUnchecked(storedItem);
            }
            
            OnContainerChanged(containerizables.Select(x => x.Item), ContainerChangeType.Add);
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

            foreach (StoredIContainerizable storedItem in containerizables)
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
        public bool IsAreaFreeExcluding(RectInt area, IContainerizable item)
        {
            int i = FindItem(item);
            StoredIContainerizable storedContainerizable = default;
            if (i != -1)
            {
                storedContainerizable = containerizables[i];
                containerizables[i] = new StoredIContainerizable(storedContainerizable.Item, new Vector2Int(100000, 100000));
            }

            bool areaFree = IsAreaFree(area);

            if (i != -1)
            {
                containerizables[i] = storedContainerizable;
            }

            return areaFree;
        }

        /// <summary>
        /// Removes an item from the container
        /// </summary>
        /// <param name="item">The item to remove</param>
        public void RemoveItem(IContainerizable item)
        {
            for (var i = 0; i < containerizables.Count; i++)
            {
                if (containerizables[i].Item == item)
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
        public void RemoveItems(IContainerizable[] itemsToRemove)
        {
            foreach (IContainerizable itemToRemove in itemsToRemove)
            {
                lock (modificationLock)
                {
                    for (var i = 0; i < containerizables.Count; i++)
                    {
                        StoredIContainerizable storedItem = containerizables[i];
                        if (storedItem.Item == itemToRemove)
                        {
                            StoredContainerizables.RemoveAt(i);
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
        public bool MoveItemUnchecked(StoredIContainerizable item)
        {
            for (var i = 0; i < containerizables.Count; i++)
            {
                StoredIContainerizable x = containerizables[i];
                if (x.Item == item.Item)
                {
                    if (x.Position != item.Position)
                    {
                        containerizables[i] = item;
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
        public void MoveItemsUnchecked(StoredIContainerizable[] items)
        {
            foreach (StoredIContainerizable storedItem in items)
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
        public IContainerizable ItemAt(Vector2Int position)
        {
            foreach (StoredIContainerizable storedItem in containerizables)
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
        public Vector2Int PositionOf(IContainerizable item)
        {
            foreach (StoredIContainerizable storedItem in containerizables)
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
                containerizables.AddRange(otherContainer.containerizables);
                OnContainerChanged(otherContainer.Containerizables, ContainerChangeType.Add);
                return;
            }
            
            if (otherContainer.Empty)
            {
                Dump();
                return;
            }
            
            // Loop through all items to find the first index of divergence
            // We can assume that all items after that point have been changed, as items are always inserted at the end
            List<IContainerizable> movedItems = new List<IContainerizable>();
            int changedIndex = -1;
            for (var i = 0; i < containerizables.Count; i++)
            {
                StoredIContainerizable storedItem = containerizables[i];
                StoredIContainerizable otherContainerItem = otherContainer.containerizables[i];
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
            Item[] removedItems = new Item[containerizables.Count - changedIndex];
            for (var i = changedIndex; i < containerizables.Count;)
            {
                containerizables.RemoveAt(i);
            }
            OnContainerChanged(removedItems.AsEnumerable(), ContainerChangeType.Remove);

            // Add all remaining items
            for (int i = changedIndex; i < otherContainer.ItemCount; i++)
            {
                containerizables.Add(otherContainer.containerizables[i]);
            }
            OnContainerChanged(otherContainer.Containerizables.Skip(changedIndex + 1), ContainerChangeType.Add);
                  
        }

        private void RemoveItemAt(int index)
        {
            StoredIContainerizable item = containerizables[index];
            lock (modificationLock)
            {
                containerizables.RemoveAt(index);
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
            IContainerizable[] oldItems = containerizables.Select(x => x.Item).ToArray();
            for (int i = 0; i < oldItems.Length; i++)
            {
                oldItems[i].Container = null;
            }
            containerizables.Clear();

            LastModification = Time.time;
            OnContainerChanged(oldItems, ContainerChangeType.Remove);
        }

        /// <summary>
        /// Destroys all items in this container
        /// </summary>
        public void Destroy()
        {
            IContainerizable[] oldItems = containerizables.Select(x => x.Item).ToArray();
            foreach (var item in containerizables)
            {
                item.Item.Destroy();
            }
            containerizables.Clear();

            LastModification = Time.time;
            OnContainerChanged(oldItems, ContainerChangeType.Remove);
        }

        /// <summary>
        /// Checks if this container contains the item
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>If it is in this container</returns>
        public bool ContainsItem(IContainerizable item)
        {
            foreach (StoredIContainerizable storedItem in containerizables)
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
        public bool CouldStoreItem(IContainerizable item)
        {
            // Do not store if the item is the container itself
            if (AttachedTo.GetComponent<IContainerizable>() == item)
            {
                return false;
            }

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
        /// Checks if this item fits inside the container
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CouldHoldItem(IContainerizable item)
        {
            Vector2Int itemSize = item.Size;
            int maxX = Size.x - itemSize.x;
            int maxY = Size.y - itemSize.y;

            // TODO: Use a more efficient algorithm
            for (int y = 0; y <= maxY; y++)
            {
                for (int x = 0; x <= maxX; x++)
                {
                    if (IsAreaFreeExcluding(new RectInt(new Vector2Int(x, y), item.Size), item))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the index of an item
        /// </summary>
        /// <param name="item">The item to look for</param>
        /// <returns>The index of the item or -1 if not found</returns>
        public int FindItem(IContainerizable item)
        {
            for (var i = 0; i < containerizables.Count; i++)
            {
                StoredIContainerizable storedItem = containerizables[i];
                if (storedItem.Item == item)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnItemAdded(IContainerizable item)
        {
            OnContainerChanged(new[] {item}, ContainerChangeType.Add);
        }

        private void OnItemRemoved(IContainerizable item)
        {
            OnContainerChanged(new[] {item}, ContainerChangeType.Remove);
        }

        protected virtual void OnContainerChanged(IEnumerable<IContainerizable> changedItems, ContainerChangeType type)
        {
            ContentsChanged?.Invoke(this, changedItems, type);
        }

        public struct StoredIContainerizable : IEquatable<StoredIContainerizable>
        {
            public readonly IContainerizable Item;
            public readonly Vector2Int Position;

            public StoredIContainerizable(IContainerizable item, Vector2Int position)
            {
                Item = item;
                Position = position;
            }

            public bool Equals(StoredIContainerizable other)
            {
                return Equals(Item, other.Item) && Position.Equals(other.Position);
            }

            public override bool Equals(object obj)
            {
                return obj is StoredIContainerizable other && Equals(other);
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