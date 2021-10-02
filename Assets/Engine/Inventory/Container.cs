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
        public bool Empty => ContainableCount == 0;
        
        /// <summary>
        /// How many items are in this container
        /// </summary>
        public int ContainableCount => containables.Count;

        /// <summary>
        /// The items stored in this container
        /// </summary>
        public IEnumerable<IContainable> Containerizables => containables.Select(x => x.Containable);

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
        public bool AddContainable(IContainable containable)
        {
            if (ContainsContainable(containable))
            {
                return true;
            }

            if (!CouldStoreContainable(containable))
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
                    if (AddContainable(containable, containerizablePosition))
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
        /// <param name="containable">The containable to add</param>
        /// <param name="position">The target position in the container</param>
        /// <returns>If the containable was added</returns>
        public bool AddContainable(IContainable containable, Vector2Int position)
        {
            int itemIndex = FindContainable(containable);
            if (itemIndex != -1)
            {
                StoredIContainable existingContainable = StoredContainables[itemIndex];
                // Try to move existing containable
                if (existingContainable.Position != position)
                {
                    if (IsAreaFreeExcluding(new RectInt(position, containable.Size), containable))
                    {
                        StoredContainables[itemIndex] = new StoredIContainable(containable, position);
                        OnContainerChanged(new[] {containable}, ContainerChangeType.Move);
                        return true;
                    }

                    return false;
                }

                // Containable at same position, nothing to do
                return true;
            }

            if (!CouldStoreContainable(containable) || !canHoldVolume(containable))
            {
                return false;
            }

            bool wasAdded = false;
            lock (modificationLock)
            {
                if (IsAreaFree(new RectInt(position, containable.Size)))
                {
                    AddContainableUnchecked(containable, position);
                    wasAdded = true;
                }
            }

            if (wasAdded)
            {
                containable.SetContainer(this, true, false);
                OnContainableAdded(containable);
            }

            return wasAdded;
        }

        /// <summary>
        /// Adds an containable to the container without any checks (but ensuring there are no duplicates)
        /// </summary>
        /// <param name="containable">The containable to add</param>
        /// <param name="position">Where the containable should go, make sure this position is valid and free!</param>
        private void AddContainableUnchecked(IContainable containable, Vector2Int position)
        {
            var newContainable = new StoredIContainable(containable, position);

            // Move it if it is already in the container
            if (MoveContainableUnchecked(newContainable))
            {
                return;
            }

            containables.Add(newContainable);
            LastModification = Time.time;
        }

        /// <summary>
        /// Adds a stored containable without checking any validity
        /// <param name="storedContainable">The containable to store</param>
        /// </summary>
        public void AddContainableUnchecked(StoredIContainable storedContainable)
        {
            AddContainableUnchecked(storedContainable.Containable, storedContainable.Position);
        }

        /// <summary>
        /// Add an array of items without performing checks
        /// </summary>
        /// <param name="items"></param>
        public void AddContainablesUnchecked(StoredIContainable[] containables)
        {
            foreach (StoredIContainable storedContainable in containables)
            {
                AddContainableUnchecked(storedContainable);
            }
            
            OnContainerChanged(containables.Select(x => x.Containable), ContainerChangeType.Add);
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

            foreach (StoredIContainable storedContainable in containables)
            {
                var storedContainablePlacement = new RectInt(storedContainable.Position, storedContainable.Containable.Size);
                if (area.Overlaps(storedContainablePlacement))
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
            int i = FindContainable(containable);
            StoredIContainable storedContainerizable = default;
            if (i != -1)
            {
                storedContainerizable = containables[i];
                containables[i] = new StoredIContainable(storedContainerizable.Containable, new Vector2Int(100000, 100000));
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
        public void RemoveContainable(IContainable containable)
        {
            for (var i = 0; i < containables.Count; i++)
            {
                if (containables[i].Containable == containable)
                {
                    RemoveContainableAt(i);
                    return;
                }
            }
        }

        /// <summary>
        /// Removes multiple items from the container
        /// </summary>
        /// <param name="itemsToRemove">An array of items to remove</param>
        public void RemoveContainables(IContainable[] itemsToRemove)
        {
            foreach (IContainable itemToRemove in itemsToRemove)
            {
                lock (modificationLock)
                {
                    for (var i = 0; i < containables.Count; i++)
                    {
                        StoredIContainable storedContainable = containables[i];
                        if (storedContainable.Containable == itemToRemove)
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
        public bool MoveContainableUnchecked(StoredIContainable containable)
        {
            for (var i = 0; i < containables.Count; i++)
            {
                StoredIContainable x = containables[i];
                if (x.Containable == containable.Containable)
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
        public void MoveContainablesUnchecked(StoredIContainable[] items)
        {
            foreach (StoredIContainable storedContainable in items)
            {
                MoveContainableUnchecked(storedContainable);
            }
            
            OnContainerChanged(items.Select(x => x.Containable), ContainerChangeType.Move);
        }

        /// <summary>
        /// Finds an containable at a position
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>The containable at the position, or null if there is none</returns>
        public IContainable ContainableAt(Vector2Int position)
        {
            foreach (StoredIContainable storedContainable in containables)
            {
                var storedContainablePlacement = new RectInt(storedContainable.Position, storedContainable.Containable.Size);
                if (storedContainablePlacement.Contains(position))
                {
                    return storedContainable.Containable;
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
            foreach (StoredIContainable storedContainable in containables)
            {
                if (storedContainable.Containable == containable)
                {
                    return storedContainable.Position;
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
            List<IContainable> movedContainables = new List<IContainable>();
            int changedIndex = -1;
            for (var i = 0; i < containables.Count; i++)
            {
                StoredIContainable storedContainable = containables[i];
                StoredIContainable otherContainerContainable = otherContainer.containables[i];
                if (storedContainable.Containable != otherContainerContainable.Containable)
                {
                    changedIndex = i;
                    break;
                }

                if (storedContainable.Position != otherContainerContainable.Position)
                {
                    movedContainables.Add(storedContainable.Containable);
                }
            }

            // Invoke move logic if any element has moved
            if (movedContainables.Count > 0)
            {
                OnContainerChanged(movedContainables, ContainerChangeType.Move);
            }

            // Nothing actually changed
            if (changedIndex == -1)
            {
                return;
            }

            // Remove all items after first divergence
            IContainable[] removedContainables = new IContainable[containables.Count - changedIndex];
            for (var i = changedIndex; i < containables.Count;)
            {
                containables.RemoveAt(i);
            }
            OnContainerChanged(removedContainables.AsEnumerable(), ContainerChangeType.Remove);

            // Add all remaining items
            for (int i = changedIndex; i < otherContainer.ContainableCount; i++)
            {
                containables.Add(otherContainer.containables[i]);
            }
            OnContainerChanged(otherContainer.Containerizables.Skip(changedIndex + 1), ContainerChangeType.Add);
                  
        }

        private void RemoveContainableAt(int index)
        {
            StoredIContainable containable = containables[index];
            lock (modificationLock)
            {
                containables.RemoveAt(index);
            }

            LastModification = Time.time;
            containable.Containable.SetContainerUnchecked(null);
            OnContainableRemoved(containable.Containable);
        }

        /// <summary>
        /// Empties the container, removing all items
        /// </summary>
        public void Dump()
        {
            IContainable[] oldContainables = containables.Select(x => x.Containable).ToArray();
            for (int i = 0; i < oldContainables.Length; i++)
            {
                oldContainables[i].Container = null;
            }
            containables.Clear();

            LastModification = Time.time;
            OnContainerChanged(oldContainables, ContainerChangeType.Remove);
        }

        /// <summary>
        /// Destroys all items in this container
        /// </summary>
        public void Destroy()
        {
            IContainable[] oldContainables = containables.Select(x => x.Containable).ToArray();
            foreach (var containable in containables)
            {
                containable.Containable.Destroy();
            }
            containables.Clear();

            LastModification = Time.time;
            OnContainerChanged(oldContainables, ContainerChangeType.Remove);
        }

        /// <summary>
        /// Checks if this container contains the containable
        /// </summary>
        /// <param name="containable">The containable to search for</param>
        /// <returns>If it is in this container</returns>
        public bool ContainsContainable(IContainable containable)
        {
            foreach (StoredIContainable storedContainable in containables)
            {
                if (storedContainable.Containable == containable)
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
        public bool CouldStoreContainable(IContainable containable)
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
        public bool CouldHoldContainable(IContainable containable)
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
        public int FindContainable(IContainable containable)
        {
            for (var i = 0; i < containables.Count; i++)
            {
                StoredIContainable storedContainable = containables[i];
                if (storedContainable.Containable == containable)
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnContainableAdded(IContainable containable)
        {
            OnContainerChanged(new[] {containable}, ContainerChangeType.Add);
        }

        private void OnContainableRemoved(IContainable containable)
        {
            OnContainerChanged(new[] {containable}, ContainerChangeType.Remove);
        }

        protected virtual void OnContainerChanged(IEnumerable<IContainable> changedContainables, ContainerChangeType type)
        {
            ContentsChanged?.Invoke(this, changedContainables, type);
        }

        public struct StoredIContainable : IEquatable<StoredIContainable>
        {
            public readonly IContainable Containable;
            public readonly Vector2Int Position;

            public StoredIContainable(IContainable containable, Vector2Int position)
            {
                Containable = containable;
                Position = position;
            }

            public bool Equals(StoredIContainable other)
            {
                return Equals(Containable, other.Containable) && Position.Equals(other.Position);
            }

            public override bool Equals(object obj)
            {
                return obj is StoredIContainable other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((Containable != null ? Containable.GetHashCode() : 0) * 397) ^ Position.GetHashCode();
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