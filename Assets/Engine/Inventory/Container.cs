using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SS3D.Content;
using SS3D.Engine.Substances;

namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// Stores items in a 2 dimensional container
    /// </summary>
    public class Container
    {
        private SubstanceContainer substanceContainer;

        public SubstanceContainer SubstanceContainer
        {
            get => substanceContainer;
        }

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

        private readonly List<StoredIContainable> containables = new List<StoredIContainable>();
        private readonly object modificationLock = new object();

        public delegate void ContainerContentsHandler(Container container, IEnumerable<IContainable> items,
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
        public int ItemCount => containables.Count;

        /// <summary>
        /// The items stored in this container
        /// </summary>
        public IEnumerable<IContainable> Containerizables => containables.Select(x => x.Item);

        /// <summary>
        /// The items stored in this container, including information on how they are stored
        /// </summary>
        public List<StoredIContainable> StoredContainables => containables;

        /// <summary>
        /// The last time the contents of this container were changed
        /// </summary>
        public float LastModification { get; private set; }

        /// <summary>
        /// Places an containable into this container in the first available position
        /// </summary>
        /// <param name="containable">The containable to place</param>
        /// <returns>If the containable was added</returns>
        public bool AddItem(IContainable containable)
        {
            if (ContainsItem(containable))
            {
                return true;
            }

            if (!CouldStoreItem(containable))
            {
                return false;
            }

            Vector2Int containerizableSize = containable.Size;
            int maxX = Size.x - containerizableSize.x;
            int maxY = Size.y - containerizableSize.y;

            // TODO: Use a more efficient algorithm
            for (int y = 0; y <= maxY; y++)
            {
                for (int x = 0; x <= maxX; x++)
                {
                    Vector2Int containerizablePosition = new Vector2Int(x, y);
                    if (AddItem(containable, containerizablePosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public float TotalStoredVolume()
        {
            float volume = 0f;
            foreach(IContainable stored in Containerizables)
            {
                volume += stored.Volume;
            }
            return volume;
        }

        /// <summary>
        /// Tries to add an containable at the specified position
        /// </summary>
        /// <param name="storedItem">The containable to add</param>
        /// <param name="position">The target position in the container</param>
        /// <returns>If the containable was added</returns>
        public bool AddItem(IContainable containable, Vector2Int position)
        {
            int itemIndex = FindItem(containable);
            if (itemIndex != -1)
            {
                StoredIContainable existingItem = StoredContainables[itemIndex];
                // Try to move existing containable
                if (existingItem.Position != position)
                {
                    if (IsAreaFreeExcluding(new RectInt(position, containable.Size), containable))
                    {
                        StoredContainables[itemIndex] = new StoredIContainable(containable, position);
                        OnContainerChanged(new[] {containable}, ContainerChangeType.Move);
                        return true;
                    }

                    return false;
                }

                // Item at same position, nothing to do
                return true;
            }

            if (!CouldStoreItem(containable) || !canHoldVolume(containable))
            {
                return false;
            }

            bool wasAdded = false;
            lock (modificationLock)
            {
                if (IsAreaFree(new RectInt(position, containable.Size)))
                {
                    AddItemUnchecked(containable, position);
                    wasAdded = true;
                }
            }

            if (wasAdded)
            {
                containable.SetContainer(this, true, false);
                OnItemAdded(containable);
            }

            return wasAdded;
        }

        /// <summary>
        /// Adds an containable to the container without any checks (but ensuring there are no duplicates)
        /// </summary>
        /// <param name="containable">The containable to add</param>
        /// <param name="position">Where the containable should go, make sure this position is valid and free!</param>
        private void AddItemUnchecked(IContainable containable, Vector2Int position)
        {
            var newItem = new StoredIContainable(containable, position);

            // Move it if it is already in the container
            if (MoveItemUnchecked(newItem))
            {
                return;
            }

            containables.Add(newItem);
            LastModification = Time.time;
        }

        /// <summary>
        /// Adds a stored containable without checking any validity
        /// <param name="storedItem">The containable to store</param>
        /// </summary>
        public void AddItemUnchecked(StoredIContainable storedItem)
        {
            AddItemUnchecked(storedItem.Item, storedItem.Position);
        }

        /// <summary>
        /// Add an array of items without performing checks
        /// </summary>
        /// <param name="items"></param>
        public void AddItemsUnchecked(StoredIContainable[] containables)
        {
            foreach (StoredIContainable storedItem in containables)
            {
                AddItemUnchecked(storedItem);
            }
            
            OnContainerChanged(containables.Select(x => x.Item), ContainerChangeType.Add);
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

            foreach (StoredIContainable storedItem in containables)
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
        /// Checks if a given area in the container is free, while excluding an containable
        /// </summary>
        /// <param name="area">The area to check</param>
        /// <param name="containable">The containable to exclude from the check</param>
        /// <returns>If the given area is free</returns>
        public bool IsAreaFreeExcluding(RectInt area, IContainable containable)
        {
            int i = FindItem(containable);
            StoredIContainable storedContainerizable = default;
            if (i != -1)
            {
                storedContainerizable = containables[i];
                containables[i] = new StoredIContainable(storedContainerizable.Item, new Vector2Int(100000, 100000));
            }

            bool areaFree = IsAreaFree(area);

            if (i != -1)
            {
                containables[i] = storedContainerizable;
            }

            return areaFree;
        }

        /// <summary>
        /// Removes an containable from the container
        /// </summary>
        /// <param name="containable">The containable to remove</param>
        public void RemoveItem(IContainable containable)
        {
            for (var i = 0; i < containables.Count; i++)
            {
                if (containables[i].Item == containable)
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
        public void RemoveItems(IContainable[] itemsToRemove)
        {
            foreach (IContainable itemToRemove in itemsToRemove)
            {
                lock (modificationLock)
                {
                    for (var i = 0; i < containables.Count; i++)
                    {
                        StoredIContainable storedItem = containables[i];
                        if (storedItem.Item == itemToRemove)
                        {
                            StoredContainables.RemoveAt(i);
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
        /// Moves an containable without performing validation
        /// </summary>
        /// <param name="containable">The containable to move</param>
        /// <returns>If the containable was moved</returns>
        public bool MoveItemUnchecked(StoredIContainable containable)
        {
            for (var i = 0; i < containables.Count; i++)
            {
                StoredIContainable x = containables[i];
                if (x.Item == containable.Item)
                {
                    if (x.Position != containable.Position)
                    {
                        containables[i] = containable;
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
        public void MoveItemsUnchecked(StoredIContainable[] items)
        {
            foreach (StoredIContainable storedItem in items)
            {
                MoveItemUnchecked(storedItem);
            }
            
            OnContainerChanged(items.Select(x => x.Item), ContainerChangeType.Move);
        }

        /// <summary>
        /// Finds an containable at a position
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>The containable at the position, or null if there is none</returns>
        public IContainable ItemAt(Vector2Int position)
        {
            foreach (StoredIContainable storedItem in containables)
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
        /// Finds the position of an containable in the container
        /// </summary>
        /// <param name="containable">The containable to look for</param>
        /// <returns>The containable's position or (-1, -1)</returns>
        public Vector2Int PositionOf(IContainable containable)
        {
            foreach (StoredIContainable storedItem in containables)
            {
                if (storedItem.Item == containable)
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
                containables.AddRange(otherContainer.containables);
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
            List<IContainable> movedItems = new List<IContainable>();
            int changedIndex = -1;
            for (var i = 0; i < containables.Count; i++)
            {
                StoredIContainable storedItem = containables[i];
                StoredIContainable otherContainerItem = otherContainer.containables[i];
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
            Item[] removedItems = new Item[containables.Count - changedIndex];
            for (var i = changedIndex; i < containables.Count;)
            {
                containables.RemoveAt(i);
            }
            OnContainerChanged(removedItems.AsEnumerable(), ContainerChangeType.Remove);

            // Add all remaining items
            for (int i = changedIndex; i < otherContainer.ItemCount; i++)
            {
                containables.Add(otherContainer.containables[i]);
            }
            OnContainerChanged(otherContainer.Containerizables.Skip(changedIndex + 1), ContainerChangeType.Add);
                  
        }

        private void RemoveItemAt(int index)
        {
            StoredIContainable containable = containables[index];
            lock (modificationLock)
            {
                containables.RemoveAt(index);
            }

            LastModification = Time.time;
            containable.Item.SetContainerUnchecked(null);
            OnItemRemoved(containable.Item);
        }

        /// <summary>
        /// Empties the container, removing all items
        /// </summary>
        public void Dump()
        {
            IContainable[] oldItems = containables.Select(x => x.Item).ToArray();
            for (int i = 0; i < oldItems.Length; i++)
            {
                oldItems[i].Container = null;
            }
            containables.Clear();

            LastModification = Time.time;
            OnContainerChanged(oldItems, ContainerChangeType.Remove);
        }

        /// <summary>
        /// Destroys all items in this container
        /// </summary>
        public void Destroy()
        {
            IContainable[] oldItems = containables.Select(x => x.Item).ToArray();
            foreach (var containable in containables)
            {
                containable.Item.Destroy();
            }
            containables.Clear();

            LastModification = Time.time;
            OnContainerChanged(oldItems, ContainerChangeType.Remove);
        }

        /// <summary>
        /// Checks if this container contains the containable
        /// </summary>
        /// <param name="containable">The containable to search for</param>
        /// <returns>If it is in this container</returns>
        public bool ContainsItem(IContainable containable)
        {
            foreach (StoredIContainable storedItem in containables)
            {
                if (storedItem.Item == containable)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if this containable could be stored (traits etc.) without considering size
        /// </summary>
        /// <param name="containable"></param>
        /// <returns></returns>
        public bool CouldStoreItem(IContainable containable)
        {
            // Do not store if the containable is the container itself
            if (AttachedTo.GetComponent<IContainable>() == containable)
            {
                return false;
            }

            foreach (Filter filter in Filters)
            {
                if (!filter.CanStore(containable))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the containable volume fits the remaining volume in the container
        /// </summary>
        /// <param name="containable"> The IContainable whose volume is being tested</param>
        /// <returns></returns>
        public bool canHoldVolume(IContainable containable)
        {
            return AttachedTo.containerDescriptor.volume > AttachedTo.containerDescriptor.VolumeOccupied + containable.Volume;  
        }

        /// <summary>
        /// Checks if this containable fits inside the container
        /// </summary>
        /// <param name="containable"></param>
        /// <returns></returns>
        public bool CouldHoldItem(IContainable containable)
        { 
            if (!canHoldVolume(containable))
            {
                return false;
            }

            Vector2Int itemSize = containable.Size;
            int maxX = Size.x - itemSize.x;
            int maxY = Size.y - itemSize.y;

            // TODO: Use a more efficient algorithm
            for (int y = 0; y <= maxY; y++)
            {
                for (int x = 0; x <= maxX; x++)
                {
                    if (IsAreaFreeExcluding(new RectInt(new Vector2Int(x, y), containable.Size), containable))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the index of an containable
        /// </summary>
        /// <param name="containable">The containable to look for</param>
        /// <returns>The index of the containable or -1 if not found</returns>
        public int FindItem(IContainable containable)
        {
            for (var i = 0; i < containables.Count; i++)
            {
                StoredIContainable storedItem = containables[i];
                if (storedItem.Item == containable)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnItemAdded(IContainable containable)
        {
            OnContainerChanged(new[] {containable}, ContainerChangeType.Add);
        }

        private void OnItemRemoved(IContainable containable)
        {
            OnContainerChanged(new[] {containable}, ContainerChangeType.Remove);
        }

        protected virtual void OnContainerChanged(IEnumerable<IContainable> changedItems, ContainerChangeType type)
        {
            ContentsChanged?.Invoke(this, changedItems, type);
        }

        public struct StoredIContainable : IEquatable<StoredIContainable>
        {
            public readonly IContainable Item;
            public readonly Vector2Int Position;

            public StoredIContainable(IContainable containable, Vector2Int position)
            {
                Item = containable;
                Position = position;
            }

            public bool Equals(StoredIContainable other)
            {
                return Equals(Item, other.Item) && Position.Equals(other.Position);
            }

            public override bool Equals(object obj)
            {
                return obj is StoredIContainable other && Equals(other);
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