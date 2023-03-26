using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using FishNet.Object.Synchronizing;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.Items;
using UnityEngine;
using static SS3D.Substances.SubstanceContainer;
using static SS3D.Systems.Inventory.Containers.AttachedContainer;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// Stores items in a 2 dimensional container. 
    /// This class is handling the logic part of storing items. 
    /// It checks if items can be added, removed, replace, and does it when possible. 
    /// </summary>
    public sealed class Container
    {
        /// <summary>
        /// The size of this container
        /// </summary>
        private Vector2Int _size;

        private string _containerName = "container";

        private Filter _startFilter;
        /// <summary>
        /// An optional reference to an attached container
        /// </summary>
        public AttachedContainer AttachedTo { get; set; }
        /// <summary>
        /// The items stored in this container, including information on how they are stored
        /// </summary>
        private readonly List<StoredItem> _storedItems;
        /// <summary>
        /// Server sole purpose of locking code execution while an operation is outgoing
        /// </summary>
        private readonly object _modificationLock = new();
        /// <summary>
        /// The last time the contents of this container were changed
        /// </summary>
        public float LastModification { get; private set; }

        public string ContainerName => _containerName;

        public Filter StartFilter => _startFilter;

        public Vector2Int Size => _size;

        public bool HideItems => _hideItems;

        public ContainerType ContainerType => _type;

        public List<StoredItem> StoredItems => _storedItems;
        /// <summary>
        /// Is this container empty
        /// </summary>
        public bool Empty => ItemCount == 0;
        /// <summary>
        /// How many items are in this container
        /// </summary>
        public int ItemCount => StoredItems.Count;
        /// <summary>
        /// The items stored in this container
        /// </summary>
        public IEnumerable<ItemActor> Items => StoredItems.Select(x => x.Item);

        /// <summary>
        /// The creatures looking at this container
        /// </summary>
        public readonly List<Entity> ObservingPlayers = new();

        /// <summary>
        /// Set visibility of objects inside the container (not in the UI, in the actual game object).
        /// If the container is Hidden, the visibility of items is always off.
        /// </summary>
        private bool _hideItems = true;

        private ContainerType _type = ContainerType.None;

        public delegate void ContainerContentsHandler(Container container, IEnumerable<ItemActor> oldItems, IEnumerable<ItemActor> newItems, ContainerChangeType type);
        /// <summary>
        /// Called when the contents of the container change
        /// </summary>
        public event ContainerContentsHandler OnContentsChanged;

        public event ObserverHandler OnNewObserver;

        public delegate void ObserverHandler(Container container, Entity observer);


        public Container()
        {
            _storedItems = new List<StoredItem>();
            _size = new Vector2Int(1, 1);
        }

        public Container(Vector2Int size)
        {
            _storedItems = new List<StoredItem>();
            _size = size;
        }

        /// <summary>
        /// Set up the container with an attached container. 
        /// </summary>
        public Container(AttachedContainer attachedContainer)
        {
            AttachedTo = attachedContainer;
            _size = attachedContainer.Size;
            _type = attachedContainer.Type;
            _hideItems = attachedContainer.HideItems;
            _storedItems = (List<StoredItem>) (attachedContainer.StoredItems.Collection);
            _startFilter= attachedContainer.StartFilter;
        }


        /// <summary>
        /// Places an item into this container in the first available position
        /// </summary>
        /// <param name="item">The item to place</param>
        /// <returns>If the item was added</returns>
        public bool AddItem(ItemActor item)
        {
            if (ContainsItem(item))
            {
                return true;
            }

            if (!CanStoreItem(item))
            {
                return false;
            }

            Vector2Int itemSize = item.Size;
            int maxX = Size.x - itemSize.x;
            int maxY = Size.y - itemSize.y;

            // TODO: Use a more efficient algorithm
            for (int y = 0; y <= maxY; y++)
            {
                for (int x = 0; x <= maxX; x++)
                {
                    Vector2Int itemPosition = new Vector2Int(x, y);
                    if (AddItemPosition(item, itemPosition))
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
        public bool AddItemPosition(ItemActor item, Vector2Int position)
        {
            int itemIndex = FindItem(item);
            if (itemIndex != -1)
            {
                StoredItem existingItem = StoredItems[itemIndex];
                // Try to move existing item
                if (existingItem.Position == position)
                {
                    return true;
                }

                if (!IsAreaFreeExcluding(new RectInt(position, item.Size), item))
                {
                    return false;
                }

                StoredItem storedItem = new(item, position);
                ReplaceStoredItems(storedItem, itemIndex);
                return true;

                // Item at same position, nothing to do
            }

            if (!CanStoreItem(item))
            {
                return false;
            }

            bool wasAdded = false;
            lock (_modificationLock)
            {
                if (IsAreaFree(new RectInt(position, item.Size)))
                {
                    AddItemUnchecked(item, position);
                    wasAdded = true;
                }
            }

            if (!wasAdded)
            {
                return false;
            }

            item.SetContainer(this, true, false);

            return true;
        }

        /// <summary>
        /// Adds an item to the container without any checks (but ensuring there are no duplicates)
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <param name="position">Where the item should go, make sure this position is valid and free!</param>
        private void AddItemUnchecked(ItemActor item, Vector2Int position)
        {
            StoredItem newItem = new(item, position);

            // Move it if it is already in the container
            if (MoveItemUnchecked(newItem))
            {
                return;
            }

            AddToStoredItems(newItem);
            LastModification = Time.time;
        }

        /// <summary>
        /// Correctly add a storeItem to the container. All adding should use this method, never do it directly.
        /// If an AttachedContainer is set up, add to AttachedContainer's syncList, 
        /// this will update _storedItems automatically as _storedItems is set up as a reference to the list internal to AttachedContainer's syncList.
        /// If no AttachedContainer is set up, add to the StoredItems list directly.
        /// </summary>
        /// <param name="newItem"> the item to store.</param>
        private void AddToStoredItems(StoredItem newItem)
        {
            if(AttachedTo != null)
            {
                AttachedTo.StoredItems.Add(newItem);
            }
            else
            {
                StoredItems.Add(newItem);
            }
        }


        /// <summary>
        /// Correctly set a storeItem in the container at the given index. All replacing should use this method, never do it directly.
        /// If an AttachedContainer is set up, set to AttachedContainer's syncList, 
        /// this will update _storedItems automatically as _storedItems is set up as a reference to the list internal to AttachedContainer's syncList.
        /// If no AttachedContainer is set up, set the StoredItems list directly.
        /// </summary>
        /// <param name="item">the item to store.</param>
        /// <param name="index">the index in the list at which it should be stored.</param>
        private void ReplaceStoredItems(StoredItem item, int index)
        { 
            if (AttachedTo != null)
            {
                AttachedTo.StoredItems.Set(index, item);
            }
            else
            {
                StoredItems[index] = item;
            }
        }

        /// <summary>
        /// Correctly remove a storeItem in the container at the given index. All removing should use this method, never do it directly.
        /// If an AttachedContainer is set up, set to AttachedContainer's syncList, 
        /// this will update _storedItems automatically as _storedItems is set up as a reference to the list internal to AttachedContainer's syncList.
        /// If no AttachedContainer is set up, remove the item from the StoredItems list directly.
        /// </summary>
        /// <param name="index">the index in the list at which the storedItem should be removed.</param>
        private void RemoveStoredItem(int index)
        {
            
            if (AttachedTo != null)
            {
                AttachedTo.StoredItems.RemoveAt(index);
            }
            else
            {
                StoredItems.RemoveAt(index);
            }
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

            foreach (StoredItem storedItem in StoredItems)
            {
                if (storedItem.IsExcludedOfFreeAreaComputation) continue;
                RectInt storedItemPlacement = new(storedItem.Position, storedItem.Item.Size);
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
        public bool IsAreaFreeExcluding(RectInt area, ItemActor item)
        {
            int itemIndex = FindItem(item);
            StoredItem storedItem = default;
            if (itemIndex != -1)
            {
                storedItem = StoredItems[itemIndex];
                StoredItems[itemIndex] = new StoredItem(storedItem.Item, storedItem.Position, true);
            }

            bool areaFree = IsAreaFree(area);

            if (itemIndex != -1)
            {
                StoredItems[itemIndex] = new StoredItem(storedItem.Item, storedItem.Position, false);
                StoredItems[itemIndex] = storedItem;
            }

            return areaFree;
        }

        /// <summary>
        /// Removes an item from the container
        /// </summary>
        /// <param name="item">The item to remove</param>
        public void RemoveItem(ItemActor item)
        {
            for (int i = 0; i < StoredItems.Count; i++)
            {
                if (StoredItems[i].Item != item)
                {
                    continue;
                }

                RemoveItemAt(i);
                return;
            }
        }

        /// <summary>
        /// Moves an item without performing validation
        /// </summary>
        /// <param name="item">The item to move</param>
        /// <returns>If the item was moved</returns>
        private bool MoveItemUnchecked(StoredItem item)
        {
            for (int i = 0; i < StoredItems.Count; i++)
            {
                StoredItem x = StoredItems[i];
                if (x.Item != item.Item)
                {
                    continue;
                }

                if (x.Position == item.Position)
                {
                    return true;
                }

                ReplaceStoredItems(item, i);
                LastModification = Time.time;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Finds an item at a position
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>The item at the position, or null if there is none</returns>
        public ItemActor ItemAt(Vector2Int position)
        {
            foreach (StoredItem storedItem in StoredItems)
            {
                RectInt storedItemPlacement = new(storedItem.Position, storedItem.Item.Size);
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
        public Vector2Int PositionOf(ItemActor item)
        {
            foreach (StoredItem storedItem in StoredItems)
            {
                if (storedItem.Item == item)
                {
                    return storedItem.Position;
                }
            }
            
            return new Vector2Int(-1, -1);
        }

        private void RemoveItemAt(int index)
        {
            StoredItem storedItem = StoredItems[index];
            lock (_modificationLock)
            {
                RemoveStoredItem(index);   
            }

            LastModification = Time.time;
            storedItem.Item.SetContainerUnchecked(null);
        }

        /// <summary>
        /// Empties the container, removing all items
        /// </summary>
        public void Dump()
        {
            ItemActor[] oldItems = StoredItems.Select(x => x.Item).ToArray();
            for (int i = 0; i < oldItems.Length; i++)
            {
                oldItems[i].Container = null;
            }
            StoredItems.Clear();

            if(AttachedTo != null)
            {
                AttachedTo.StoredItems.Clear();
            }

            LastModification = Time.time;
        }

        /// <summary>
        /// Destroys all items in this container
        /// </summary>
        public void Purge()
        {
            for(int i =0; i < StoredItems.Count; i++)
            {
                StoredItems[i].Item.Delete();
            }
            StoredItems.Clear();

            if (AttachedTo != null)
            {
                AttachedTo.StoredItems.Clear();
            }

            LastModification = Time.time;
        }

        /// <summary>
        /// Checks if this container contains the item
        /// </summary>
        /// <param name="item">The item to search for</param>
        /// <returns>If it is in this container</returns>
        public bool ContainsItem(ItemActor item)
        {
            foreach (StoredItem storedItem in StoredItems)
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
        public bool CanStoreItem(ItemActor item)
        {
            // Do not store if the item is the container itself
            if (AttachedTo.GetComponent<ItemActor>() == item)
            {
                return false;
            }

            Filter filter = AttachedTo.StartFilter;
            if (filter != null)
            {
                return filter.CanStore(item);
            }

            return true;
        }

        /// <summary>
        /// Checks if this item fits inside the container
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CanHoldItem(ItemActor item)
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
        /// Checks if this item can be stored and fits inside the container
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool CanContainItem(ItemActor item)
        {
            return (CanStoreItem(item) && CanHoldItem(item));
        }

        /// <summary>
        /// Finds the index of an item
        /// </summary>
        /// <param name="item">The item to look for</param>
        /// <returns>The index of the item or -1 if not found</returns>
        public int FindItem(ItemActor item)
        {
            for (int i = 0; i < StoredItems.Count; i++)
            {
                StoredItem storedItem = StoredItems[i];
                if (storedItem.Item == item)
                {
                    return i;
                }
            }

            return -1;
        }

        public void InvokeOnContentChanged(ItemActor[] oldItems, ItemActor[] newItems, ContainerChangeType changeType)
        {
            OnContentsChanged?.Invoke(this, oldItems, newItems, changeType);
        }

        /// <summary>
        /// Adds an observer to this container
        /// </summary>
        /// <param name="observer">The creature which observes</param>
        /// <returns>True if the creature was not already observing this container</returns>
        public bool AddObserver(Entity observer)
        {
            if (ObservingPlayers.Contains(observer)) return false;

            ObservingPlayers.Add(observer);

            ProcessNewObserver(observer);
            return true;
        }

        /// <summary>
        /// Removes an observer
        /// </summary>
        /// <param name="observer">The observer to remove</param>
        public void RemoveObserver(Entity observer)
        {
            ObservingPlayers.Remove(observer);
        }

        private void ProcessNewObserver(Entity e)
        {
            OnNewObserver?.Invoke(this, e);
        }
    }
}