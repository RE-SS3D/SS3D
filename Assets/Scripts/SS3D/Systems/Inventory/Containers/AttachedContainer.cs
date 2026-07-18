using FishNet.Component.Transforming;
using SS3D.Core.Behaviours;
using SS3D.Core;
using System.Collections.Generic;
using System;
using UnityEngine;
using SS3D.Systems.Inventory.Items;
using FishNet.Object.Synchronizing;
using System.Linq;
using FishNet.Object;
using SS3D.Logging;
using SS3D.Systems.Tile;
using UnityEditor;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// AttachedContainer manages the networking  aspect of a container attached to a gameObject, and allows the user to set up a container,
    /// including it's size, interaction with it, what it can store and other options.
    /// </summary>
    public class AttachedContainer : NetworkActor
    {
        #region AttachedContainerOnlyFieldsAndProperties

        [SerializeField]
        private bool _automaticContainerSetUp = false;
        // References toward all container related scripts.

        public ContainerInteractive ContainerInteractive;
        public ContainerItemDisplay ContainerItemDisplay;

        /// <summary>
        /// The panel currently displaying this container's contents, if any. Set/cleared by
        /// StoragePanelHost — never authored on a prefab, always empty until runtime.
        /// </summary>
        [NonSerialized]
        public IContainerPanel OpenPanel;

        [Tooltip("The local position of attached items."), SerializeField]
        private Vector3 _attachmentOffset = Vector3.zero;

        [Tooltip("If the container is openable, this defines if things can be stored in the container without opening it."), SerializeField]
        private bool _onlyStoreWhenOpen;

        [Tooltip("When the container UI is opened, if set true, the animation on the object is triggered."), SerializeField]
        private bool _openWhenContainerViewed;

        [Tooltip("If items should be attached as children of the container's game object."), SerializeField]
        private bool _attachItems = true;

        // Initialized should not be displayed, it's only useful for setting up the container in editor.
        [HideInInspector, SerializeField]
        private bool _initialized;

        [Tooltip("Max distance at which the container is visible if not hidden."), SerializeField]
        private float _maxDistance = 5f;

        [Tooltip("If the container can be opened/closed, in the sense of having a close/open animation."), SerializeField]
        private bool _isOpenable;

        [Tooltip("If the container should have the container's default interactions setting script."), SerializeField]
        private bool _isInteractive;

        [Tooltip("If stuff inside the container can be seen using an UI."), SerializeField]
        private bool _hasUi;

        [Tooltip("If true, interactions in containerInteractive are ignored, instead, a script on the container's game object should implement IInteractionTarget."), SerializeField]
        private bool _hasCustomInteraction;

        [Tooltip("If the container renders items in custom position on the container."), SerializeField]
        private bool _hasCustomDisplay;

        [Tooltip(" The list of transforms defining where the items are displayed."), SerializeField]
        private Transform[] _displays;

        [Tooltip(" The number of custom displays."), SerializeField]
        private int _numberDisplay;

        [Tooltip(" if should display as slot in UI."), SerializeField]
        private bool _displayAsSlotInUI;


        public Vector3 AttachmentOffset => _attachmentOffset;

        public bool OnlyStoreWhenOpen => _onlyStoreWhenOpen;

        public bool OpenWhenContainerViewed => _openWhenContainerViewed;

        public bool AttachItems => _attachItems;

        public float MaxDistance => _maxDistance;

        public bool IsOpenable => _isOpenable;

        public bool IsInteractive => _isInteractive;

        public bool HasUi => _hasUi;

        public bool HasCustomInteraction => _hasCustomInteraction;

        public bool HasCustomDisplay => _hasCustomDisplay;

        public Transform[] Displays => _displays;

        public int NumberDisplay => _numberDisplay;

        public bool DisplayAsSlotInUI => _displayAsSlotInUI;

        #endregion

        #region ContainerAndAttachedContainerFieldsAndProperties

        public string ContainerName
        {
            get
            {
                // Prefer the owning item's display name (bags, PDAs, lockboxes) over the GameObject
                // name, which is usually the prefab id plus "(Clone)".
                Item owner = GetComponentInParent<Item>();
                if (owner != null && !string.IsNullOrEmpty(owner.Name))
                {
                    return owner.Name;
                }

                string objectName = gameObject.name;
                const string cloneSuffix = "(Clone)";
                if (objectName.EndsWith(cloneSuffix))
                {
                    objectName = objectName[..^cloneSuffix.Length].TrimEnd();
                }

                return objectName;
            }
        }

        [Tooltip("Defines the size of the container, every item takes a defined place inside a container."), SerializeField]
        private Vector2Int _size = new(0, 0);

        /// <summary>
        /// Set visibility of objects inside the container (not in the UI, in the actual game object).
        /// If the container is Hidden, the visibility of items is always off.
        /// </summary>
        [Tooltip("Set visibility of items in container."), SerializeField]
        private bool _hideItems = true;

        [Tooltip("Container type mostly allow to discriminate between different containers on a single prefab."), SerializeField]
        private ContainerType _type;

        [Tooltip("The filter on the container."), SerializeField]
        private Filter _startFilter;

        [Tooltip("The largest item size class this container accepts (Documents/design/inventory-storage.md §4)."), SerializeField]
        private SizeClass _maxSizeClass = SizeClass.Huge;

        [Tooltip("Weight ceiling shown in the storage panel readout. Exact value is a balancing pass, not enforced as a hard block (Documents/design/inventory-storage.md §13)."), SerializeField]
        private float _maxWeight = 20f;

        public ContainerType Type => _type;
        public Vector2Int Size => _size;
        public bool HideItems => _hideItems;
        public Filter StartFilter => _startFilter;
        public SizeClass MaxSizeClass => _maxSizeClass;
        public float MaxWeight => _maxWeight;

		/// <summary>
		/// Is this container empty
		/// </summary>
		public bool Empty => ItemCount == 0;
		/// <summary>
		/// How many items are in this container
		/// </summary>
		public int ItemCount => Items.Count();

        /// <summary>
        /// Total carried weight: every stored item's own weight (times its stack count), plus,
        /// recursively, the weight of anything stored inside a nested container on a stored item.
        /// Always computed, never cached (Documents/design/inventory-storage.md §2, §7).
        /// </summary>
        public float Weight
        {
            get
            {
                float total = 0f;
                foreach (StoredItem storedItem in _storedItems)
                {
                    Item item = storedItem.Item;
                    if (item == null)
                    {
                        continue;
                    }

                    total += item.Weight * Mathf.Max(1, item.StackCount);

                    foreach (AttachedContainer nested in item.GetComponentsInChildren<AttachedContainer>())
                    {
                        total += nested.Weight;
                    }
                }

                return total;
            }
        }

		#endregion

		public event EventHandler<Item> OnItemAttached;
        public event EventHandler<Item> OnItemDetached;

        public delegate void AttachedContainerHandler(AttachedContainer attachedContainer);

        public event AttachedContainerHandler OnAttachedContainerDisabled;

        /// <summary>
        /// The items stored in this container, including information on how they are stored
        /// </summary>
        [SyncObject]
        private readonly SyncList<StoredItem> _storedItems = new();

        /// <summary>
        /// The items stored in this container
        /// </summary>
        public IEnumerable<Item> Items => _storedItems.Select(x => x.Item);

		public delegate void ContainerContentsHandler(AttachedContainer container, Item oldItem, Item newItem, ContainerChangeType type);
		/// <summary>
		/// Called when the contents of the container change
		/// </summary>
		public event ContainerContentsHandler OnContentsChanged;

		private readonly object _modificationLock = new();

		public ContainerType ContainerType => _type;

		protected override void OnAwake()
        {
            base.OnAwake();
            _storedItems.OnChange += HandleStoredItemsChanged;
        }

        protected override void OnDisabled()
        {
            // Mostly used to allow inventory to update accessible containers.
            base.OnDisabled();
            if (!IsServer)
            {
                return;
            }
            OnAttachedContainerDisabled?.Invoke(this);
        }

        protected override void OnEnabled()
        {
            // Mostly used to allow inventory to update accessible containers.
            base.OnEnabled();
            if (!IsServer)
            {
                return;
            }
            var inventory = GetComponentInParent<HumanInventory>();

            if (inventory != null)
            {
                inventory.TryAddContainer(this);
            }
        }

        [Server]
        public void InvokeContainerDisabled()
        {
            OnAttachedContainerDisabled?.Invoke(this);
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            if(!IsServer) { return; }
            Purge();
        }

		public void Init(Vector2Int size, Filter filter)
		{
			_size = size;
			_startFilter= filter;
		}

        public override string ToString()
        {
            return $"{name}({nameof(AttachedContainer)})[size: {Size}, items: ]";
        }

        public void ProcessItemAttached(Item e)
        {
            OnItemAttached?.Invoke(this, e);
        }

        public void ProcessItemDetached(Item e)
        {
            OnItemDetached?.Invoke(this, e);
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
                    changeType = ContainerChangeType.Add;
					handleItemAdded(newItem.Item);
					break;
                case SyncListOperation.Insert:
					break;
				case SyncListOperation.Set:
                    changeType = ContainerChangeType.Move;
					break;
                case SyncListOperation.RemoveAt:
					changeType = ContainerChangeType.Remove;
					handleItemRemoved(oldItem.Item);
					break;
				case SyncListOperation.Clear:
                    changeType = ContainerChangeType.Remove;
					handleItemRemoved(oldItem.Item);
					break;
                case SyncListOperation.Complete:
					break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }

            if (changeType == ContainerChangeType.Add && newItem.Item.TryGetComponent(out NetworkTransform networkTransform))
            {
                 networkTransform.SetSynchronizePosition(false);
                 networkTransform.SetSynchronizeRotation(false);
            }

            if (changeType == ContainerChangeType.Remove && oldItem.Item.TryGetComponent(out NetworkTransform networkTransform2))
            {
                networkTransform2.SetSynchronizePosition(true);
                networkTransform2.SetSynchronizeRotation(true);
                //Punpun.Information(this, "from container " + this.gameObject + ", removing item" + oldItem.Item?.name);
            }

			if (changeType == ContainerChangeType.None) {
				return;
			}

            InvokeOnContentChanged(oldItem.Item, newItem.Item, changeType);
        }

        [ServerOrClient]
        private void handleItemRemoved(Item item)
        {

			if (item == null) return;

            // Restore visibility
            if (HideItems)
            {
                item.SetVisibility(true);
            }

            // Remove parent if child of this
            if (item.transform.parent == transform)
            {
                item.transform.SetParent(null, true);
            }

            ProcessItemDetached(item);
            item.Unfreeze();
        }

        [ServerOrClient]
        private void handleItemAdded(Item item)
        {
			if (item == null) return;

			item.Freeze();

            // Remove PlacedItemObject component if it exists to prevent saving items in containers
            PlacedItemObject placedItemObject = item.GetComponent<PlacedItemObject>();
            if (placedItemObject != null)
            {
                // Remove from TileMap tracking
                TileMap tileMap = SubSystems.Get<TileSubSystem>()?.CurrentMap;
                if (tileMap != null)
                {
                    tileMap.RemovePlacedItemFromTracking(placedItemObject);
                }
                
                // Destroy the PlacedItemObject component
                DestroyImmediate(placedItemObject);
            }

            // Make invisible
            if (HideItems)
            {
                item.SetVisibility(false);
            }

            if (AttachItems)
            {
                if (!HasCustomDisplay)
                {
                    Transform itemTransform = item.transform;
                    itemTransform.SetParent(transform, false);
                    itemTransform.localPosition = AttachmentOffset;
                }
                ProcessItemAttached(item);
            }
        }

		/// <summary>
		/// Places an item into this container in the first available position
		/// </summary>
		/// <param name="item">The item to place</param>
		/// <returns>If the item was added</returns>
		public bool AddItem(Item item)
		{
            if (!TryFindPositionFor(item, out Vector2Int itemPosition))
            {
                return false;
            }

            return AddItemPosition(item, itemPosition);
		}

        /// <summary>
        /// First grid cell where <paramref name="item"/> would store (free slot or mergeable stack).
        /// Used by HUD wells that map to multi-slot containers (pockets) without a picked cell.
        /// </summary>
        public bool TryFindPositionFor(Item item, out Vector2Int position)
        {
            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    Vector2Int candidate = new(x, y);
                    if (CanContainItemAtPosition(item, candidate))
                    {
                        position = candidate;
                        return true;
                    }
                }
            }

            position = default;
            return false;
        }

        /// <summary>
        /// transfer an item from this container to another container at a given position.
        /// </summary>
        public bool TransferItemToOther(Item item, Vector2Int position, AttachedContainer other)
        {
            if (other == null || !FindItem(item, out int index))
            {
                return false;
            }

            // Reject before remove — otherwise a failed AddStoredItem orphans the item (null container)
            // and clients can appear to "drop into hand" when HUD hit-tests fall through.
            if (!other.CanContainItemAtPosition(item, position))
            {
                return false;
            }

            StoredItem stored = _storedItems[index];
            if (!RemoveStoredItem(index))
            {
                return false;
            }

            if (other.AddStoredItem(new StoredItem(item, position)))
            {
                return true;
            }

            // Restore to the original slot if the add still failed (race / edge conditions).
            AddStoredItem(stored);
            return false;
        }

		/// <summary>
		/// Tries to add an item at the specified position
		/// </summary>
		/// <param name="storedItem">The item to add</param>
		/// <param name="position">The target position in the container</param>
		/// <returns>If the item was added</returns>
		public bool AddItemPosition(Item item, Vector2Int position)
		{
            return AddStoredItem(new StoredItem(item, position));
		}

		/// <summary>
		/// Correctly add a storeItem to the container. All adding should use this method, never do it directly.
		/// </summary>
		/// <param name="newItem"> the item to store.</param>
		private bool AddStoredItem(StoredItem newItem)
		{
            // Fail if attempted storage position is out of bounds
            if (newItem.Position.x >= Size.x || newItem.Position.y >= Size.y)
            {
                return false;
            }

            if (newItem.Position.x < 0 || newItem.Position.y < 0)
            {
                return false;
            }

            if (!CanContainItem(newItem.Item))
            {
                return false;
            }

            // Fail if it is the same container
            if (ReferenceEquals(newItem.Item.Container, this))
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

            // Stacking: merge into an existing compatible stack instead of taking a new slot, even if
            // the caller targeted a different (possibly occupied-by-something-else) position — checked
            // before the "position occupied" bailout below, since a drag-drop explicitly targets a
            // slot position and the natural drop target for a stackable item IS an occupied stack
            // (Documents/design/inventory-storage.md §5).
            if (newItem.Item.IsStackable && TryFindMergeableStack(newItem.Item, out int mergeIndex))
            {
                Item existingStackItem = _storedItems[mergeIndex].Item;
                existingStackItem.SetStackCount(existingStackItem.StackCount + newItem.Item.StackCount);
                DespawnMergedItem(newItem.Item);
                return true;
            }

            // Fail if item is at this postion
            if (ItemAt(newItem.Position))
            {
                return false;
            }

            _storedItems.Add(newItem);
            newItem.Item.SetContainer(this);
            return true;
		}

        /// <summary>
        /// Finds an existing stored item this stackable item could merge into: same stack definition,
        /// with enough remaining room for the incoming item's full count. Does not partially merge/split.
        /// </summary>
        private bool TryFindMergeableStack(Item item, out int index)
        {
            for (int i = 0; i < _storedItems.Count; i++)
            {
                Item existing = _storedItems[i].Item;
                if (existing != null
                    && existing.CanMergeWith(item)
                    && existing.StackCount + item.StackCount <= existing.MaxStackSize)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        /// <summary>
        /// Despawns an item that was merged into an existing stack rather than stored in its own slot.
        /// Does not go through Item.Delete()/Container.RemoveItem — the item was never added to this
        /// container's storage, and may already have been removed from its source (see TransferItemToOther).
        /// </summary>
        [Server]
        private void DespawnMergedItem(Item item)
        {
            if (item.GameObject != null)
            {
                ServerManager.Despawn(item.GameObject);
            }
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
		private bool RemoveStoredItem(int index)
		{
            StoredItem storedItem = _storedItems[index];

            if(!CanRemoveItem(storedItem.Item)) return false;

            storedItem.Item.SetContainer(null);
            lock (_modificationLock)
            {
                _storedItems.RemoveAt(index);
            }
            return true;
           
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
            if(FindItem(item, out int index))
            {
                RemoveStoredItem(index);
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

            for(int i= _storedItems.Count-1; i>=0; i--)
            {
                RemoveStoredItem(i);
            }
		}

		/// <summary>
		/// Destroys all items in this container
		/// </summary>
		public void Purge()
		{
			for (int i = 0; i < _storedItems.Count; i++)
			{
                if (_storedItems[i].Item == null) continue;
				_storedItems[i].Item.Delete();
			}
			_storedItems.Clear();
		}

		/// <summary>
		/// Checks if this container contains the item
		/// </summary>
		/// <param name="item">The item to search for</param>
		/// <returns>If it is in this container</returns>
		public bool ContainsItem(Item item)
		{
			foreach (StoredItem storedItem in _storedItems)
			{
				if (storedItem.Item.Equals(item))
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
		private bool CanStoreItem(Item item)
		{
			if (_startFilter != null)
			{
				return _startFilter.CanStore(item);
			}
			return true;
		}

		/// <summary>
		/// Checks if this item fits inside the container. A stackable item that can merge into an
		/// existing compatible stack doesn't need a free slot (Documents/design/inventory-storage.md §5).
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		private bool CanHoldItem(Item item)
		{
			if (item.IsStackable && TryFindMergeableStack(item, out _))
			{
				return true;
			}

			return Items.Count() < Size.x * Size.y;
		}

        /// <summary>
        /// Checks if this item can be stored and fits inside the container. It will also check for
        /// custom storage conditions if they exists, which are scripts put on the same game object as this container and
        /// implementing IStorageCondition.
        /// </summary>
        public bool CanContainItem(Item item)
		{
            return CanStoreItem(item)
                    && CanHoldItem(item)
                    && item.SizeClass <= _maxSizeClass // Flat size-class fit check (Documents/design/inventory-storage.md §4)
                    && !item.GetComponentsInChildren<AttachedContainer>().AsEnumerable().Contains(this) // Can't put an item in its own container
                    && !(bool)GetComponents<IStorageCondition>()?.Any(x => !x.CanStore(this, item));
        }

        /// <summary>
        /// Whether dropping this item at this exact position would succeed — used to drive the storage
        /// panel's valid/invalid drop highlighting. An occupied slot still counts as valid if it holds a
        /// stack this item could merge into (see AddStoredItem's stacking path).
        /// </summary>
        public bool CanContainItemAtPosition(Item item, Vector2Int position)
        {
            if (!CanContainItem(item) || !AreSlotCoordinatesInGrid(position))
            {
                return false;
            }

            if (IsAreaFree(position))
            {
                return true;
            }

            Item occupant = ItemAt(position);
            return item.IsStackable
                && occupant != null
                && occupant.CanMergeWith(item)
                && occupant.StackCount + item.StackCount <= occupant.MaxStackSize;
        }

        /// <summary>
        /// Whether the given inventory is allowed to open/store/take from this container right now.
        /// Always true unless an AttachedContainerLock component is present and locked
        /// (Documents/design/inventory-storage.md §8).
        /// </summary>
        public bool IsAccessibleBy(HumanInventory inventory)
        {
            AttachedContainerLock containerLock = GetComponent<AttachedContainerLock>();
            return containerLock == null || containerLock.IsAccessGranted(inventory);
        }

        /// <summary>
        /// Finds the index of an item
        /// </summary>
        /// <param name="item">The item to look for</param>
        /// <returns>The index of the item or -1 if not found</returns>
        public bool FindItem(Item item, out int index)
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
			foreach (StoredItem storedItem in _storedItems)
			{
				if (storedItem.Position == slotPosition)
				{
					return false;
				}
			}

			return true;
		}

		public void InvokeOnContentChanged(Item oldItem, Item newItem, ContainerChangeType changeType)
		{
			OnContentsChanged?.Invoke(this, oldItem, newItem, changeType);
		}

		private bool AreSlotCoordinatesInGrid(Vector2Int slotCoordinates)
		{
			return slotCoordinates.x < Size.x && slotCoordinates.y < Size.y && slotCoordinates.x >= 0 && slotCoordinates.y >= 0;
		}
	}

}