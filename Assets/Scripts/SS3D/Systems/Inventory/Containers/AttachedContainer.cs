using FishNet.Component.Transforming;
using FishNet.Object.Synchronizing;
using JetBrains.Annotations;
using SS3D.Core.Behaviours;
using SS3D.Logging;
using SS3D.Systems.Inventory.Items;
using SS3D.Systems.Inventory.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// AttachedContainer manages the networking  aspect of a container attached to a gameObject, and allows the user to set up a container,
    /// including it's size, interaction with it, what it can store and other options.
    /// </summary>
    public class AttachedContainer : NetworkActor
    {
        public delegate void ContainerContentsHandler(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type);

        public event ContainerContentsHandler OnClientContentsChanged;

        public event ContainerContentsHandler OnServerContentsChanged;

        [SyncObject]
        private readonly SyncList<StoredItem> _storedItems = new();

        private readonly object _modificationLock = new();

        [SerializeField]
        private Vector3 _attachmentOffset = Vector3.zero;

        [Tooltip("If the container renders items in custom position on the container.")]
        [SerializeField]
        private bool _hasCustomDisplay;

        [Tooltip("Defines the size of the container, every item takes a defined place inside a container.")]
        [SerializeField]
        private Vector2Int _size = Vector2Int.one;

        [Tooltip("Container type mostly allow to discriminate between different containers on a single prefab.")]
        [SerializeField]
        private ContainerType _type;

        public ContainerType Type => _type;

        public Vector2Int Size => _size;

        public string ContainerName => gameObject.name;

        public bool Empty => ItemCount == 0;

        public int ItemCount => Items.Count();

        /// <summary>
        /// The items stored in this container
        /// </summary>
        public IEnumerable<Item> Items => _storedItems.Select(x => x.Item);

        public List<StoredItem> StoredItems => _storedItems.ToList();

        public ContainerType ContainerType => _type;

        public override void OnStartServer()
        {
            base.OnStartServer();
            _storedItems.OnChange += HandleStoredItemsChanged;
        }

        public void Init(Vector2Int size)
        {
            _size = size;
        }

        [NotNull]
        public override string ToString()
        {
            return $"{name}({nameof(AttachedContainer)})[size: {Size}, items: ]";
        }

        /// <summary>
        /// Places an item into this container in the first available position
        /// </summary>
        /// <param name="item">The item to place</param>
        /// <returns>If the item was added</returns>
        public bool AddItem(Item item)
        {
            // TODO: Use a more efficient algorithm
            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    Vector2Int itemPosition = new Vector2Int(x, y);

                    if (TryAddStoredItem(new(item, itemPosition)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// transfer an item from this container to another container at a given position.
        /// </summary>
        public bool TransferItemToOther(Item item, AttachedContainer other)
        {
            if (!FindItem(item, out int index))
            {
                return false;
            }

            if (!TryRemoveStoredItem(index))
            {
                return false;
            }

            return other.AddItem(item);
        }

        /// <summary>
        /// transfer an item from this container to another container at a given position.
        /// </summary>
        public bool TransferItemToOther(Item item, Vector2Int position, AttachedContainer other)
        {
            if (!FindItem(item, out int index))
            {
                return false;
            }

            if (!TryRemoveStoredItem(index))
            {
                return false;
            }

            return other.TryAddStoredItem(new StoredItem(item, position));
        }

        public bool CanRemoveItem(Item item)
        {
            return !(bool)GetComponents<IStorageCondition>()?.Any(x => !x.CanRemove(this, item));
        }

        /// <summary>
        /// Removes an item from the container
        /// </summary>
        /// <param name="item">The item to remove</param>
        public void RemoveItem(Item item)
        {
            if (FindItem(item, out int index))
            {
                TryRemoveStoredItem(index);
            }
        }

        /// <summary>
        /// Finds an item at a position
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>The item at the position, or null if there is none</returns>
        public Item ItemAt(Vector2Int position)
        {
            foreach (StoredItem storedItem in _storedItems)
            {
                if (storedItem.Position == position)
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
            foreach (StoredItem storedItem in _storedItems)
            {
                if (storedItem.Item.Equals(item))
                {
                    return storedItem.Position;
                }
            }

            return new Vector2Int(-1, -1);
        }

        /// <summary>
        /// Empties the container, removing all items
        /// </summary>
        public void Dump()
        {
            Log.Information(this, "dumping the content of container on" + gameObject);
            Item[] oldItems = _storedItems.Select(x => x.Item).ToArray();

            for (int i = _storedItems.Count - 1; i >= 0; i--)
            {
                TryRemoveStoredItem(i);
            }
        }

        /// <summary>
        /// Destroys all items in this container
        /// </summary>
        public void Purge()
        {
            for (int i = 0; i < _storedItems.Count; i++)
            {
                if (_storedItems[i].Item == null)
                {
                    continue;
                }

                _storedItems[i].Item.Delete();
            }

            _storedItems.Clear();
        }

        /// <summary>
        /// Checks if this item can be stored and fits inside the container. It will also check for
        /// custom storage conditions if they exists, which are scripts put on the same game object as this container and
        /// implementing IStorageCondition.
        /// </summary>
        public bool CanContainItem(Item item)
        {
            return CanHoldItemNumber(item)
                && !item.GetComponentsInChildren<AttachedContainer>().AsEnumerable().Contains(this) // Can't put an item in its own container
                && !(bool)GetComponents<IStorageCondition>()?.Any(x => !x.CanStore(this, item));
        }

        public bool CanContainItemAtPosition(Item item, Vector2Int position)
        {
            return CanContainItem(item) && IsAreaFree(position) && AreSlotCoordinatesInGrid(position);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();

            if (!IsServer)
            {
                return;
            }

            Purge();
        }

        /// <summary>
        /// Runs when the container was changed, networked
        /// </summary>
        /// <param name="op">Type of change</param>
        /// <param name="index">Which element was changed</param>
        /// <param name="oldItem">Element before the change</param>
        /// <param name="newItem">Element after the change</param>
        private void HandleStoredItemsChanged(SyncListOperation op, int index, StoredItem oldItem, StoredItem newItem, bool asServer)
        {
            ContainerChangeType changeType = ContainerChangeType.None;

            switch (op)
            {
                case SyncListOperation.Add:
                {
                    changeType = ContainerChangeType.Add;
                    HandleItemAdded(newItem.Item);
                    break;
                }

                case SyncListOperation.Set:
                {
                    changeType = ContainerChangeType.Move;
                    break;
                }

                case SyncListOperation.RemoveAt:
                {
                    changeType = ContainerChangeType.Remove;
                    HandleItemRemoved(oldItem.Item);
                    break;
                }

                case SyncListOperation.Clear:
                case SyncListOperation.Insert:
                case SyncListOperation.Complete:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }

            if (changeType == ContainerChangeType.None)
            {
                return;
            }

            if (asServer)
            {
                OnServerContentsChanged?.Invoke(this, oldItem.Item, newItem.Item, changeType);
            }
            else
            {
                OnClientContentsChanged?.Invoke(this, oldItem.Item, newItem.Item, changeType);
            }
        }

        [ServerOrClient]
        private void HandleItemRemoved(Item item)
        {
            if (!item)
            {
                return;
            }

            if (item.TryGetComponent(out NetworkTransform networkTransform2))
            {
                networkTransform2.SetSynchronizePosition(true);
                networkTransform2.SetSynchronizeRotation(true);
            }

            item.SetFreeze(false);

            // Restore visibility
            if (!_hasCustomDisplay)
            {
                item.SetVisibility(true);
                item.transform.SetParent(null, true);
            }
        }

        [ServerOrClient]
        private void HandleItemAdded(Item item)
        {
            if (item == null)
            {
                return;
            }

            if (item.TryGetComponent(out NetworkTransform networkTransform))
            {
                networkTransform.SetSynchronizePosition(false);
                networkTransform.SetSynchronizeRotation(false);
            }

            item.SetFreeze(true);

            if (!_hasCustomDisplay)
            {
                item.SetVisibility(false);
                Transform itemTransform = item.transform;
                itemTransform.SetParent(transform, false);
                itemTransform.localPosition = _attachmentOffset;
            }
        }

        /// <summary>
        /// Correctly add a storeItem to the container. All adding should use this method, never do it directly.
        /// </summary>
        /// <param name="newItem"> the item to store.</param>
        private bool TryAddStoredItem(StoredItem newItem)
        {
            if (!CanContainItemAtPosition(newItem.Item, newItem.Position))
            {
                return false;
            }

            if (FindItem(newItem.Item, out int itemIndex))
            {
                StoredItem existingItem = _storedItems[itemIndex];

                // do nothing if the item is at the exact same location.
                if (existingItem.Position == newItem.Position)
                {
                    return true;
                }

                ReplaceStoredItem(newItem, itemIndex);

                return true;
            }

            _storedItems.Add(newItem);
            newItem.Item.SetContainer(this);

            return true;
        }

        /// <summary>
        /// Correctly set a storeItem in the container at the given index. All replacing should use this method, never do it directly.
        /// </summary>
        /// <param name="item">the item to store.</param>
        /// <param name="index">the index in the list at which it should be stored.</param>
        private void ReplaceStoredItem(StoredItem item, int index)
        {
            _storedItems[index] = item;
        }

        /// <summary>
        /// Correctly remove a storeItem in the container at the given index. All removing should use this method, never do it directly.
        /// </summary>
        /// <param name="index">the index in the list at which the storedItem should be removed.</param>
        private bool TryRemoveStoredItem(int index)
        {
            StoredItem storedItem = _storedItems[index];

            if (!CanRemoveItem(storedItem.Item))
            {
                return false;
            }

            storedItem.Item.SetContainer(null);

            lock (_modificationLock)
            {
                _storedItems.RemoveAt(index);
            }

            return true;
        }

        /// <summary>
        /// Checks if this item fits inside the container in terms of size
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool CanHoldItemNumber(Item item) => Items.Count() < Size.x * Size.y;

        /// <summary>
        /// Finds the index of an item
        /// </summary>
        /// <param name="item">The item to look for</param>
        /// <param name = "index">The index of the item in the list or -1 if not found</param>
        /// <returns>The index of the item or -1 if not found</returns>
        private bool FindItem(Item item, out int index)
        {
            index = -1;

            for (int i = 0; i < _storedItems.Count; i++)
            {
                StoredItem storedItem = _storedItems[i];

                if (storedItem.Item == item)
                {
                    index = i;

                    return true;
                }
            }

            return false;
        }

        private bool IsAreaFree(Vector2Int slotPosition)
        {
            return _storedItems.All(storedItem => storedItem.Position != slotPosition);
        }

        private bool AreSlotCoordinatesInGrid(Vector2Int slotCoordinates)
        {
            return slotCoordinates.x < Size.x && slotCoordinates.y < Size.y && slotCoordinates.x >= 0 && slotCoordinates.y >= 0;
        }
    }
}
