using FishNet;
using FishNet.Connection;
using SS3D.Core.Behaviours;
using SS3D.Data;
using SS3D.Data.AssetDatabases;
using SS3D.Data.Enums;
using SS3D.Systems.Entities;
using SS3D.Systems.Inventory.UI;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using SS3D.Systems.Inventory.Items;
using JetBrains.Annotations;
using FishNet.Object.Synchronizing;
using System.Linq;
using FishNet.Object;
using SS3D.Logging;
using System.Drawing;

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

        // reference towards the container UI linked to this container.
        [Tooltip("Reference towards the container UI linked to this container. Leave empty before run ! ")]
        public ContainerUi ContainerUi;

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

        // If you define setters for the properties of this region in the future, be careful and make sure they modify the related container fields as well in Container.cs.
        #region ContainerAndAttachedContainerFieldsAndProperties

        public string ContainerName => gameObject.name;

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
        public ContainerType Type => _type;
        public Vector2Int Size => _size;
        public bool HideItems => _hideItems;
        public Filter StartFilter => _startFilter;

		/// <summary>
		/// Is this container empty
		/// </summary>
		public bool Empty => ItemCount == 0;
		/// <summary>
		/// How many items are in this container
		/// </summary>
		public int ItemCount => Items.Count();

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
		/// <summary>
		/// The last time the contents of this container were changed
		/// </summary>
		public float LastModification { get; private set; }

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

			// This seem to be called correctly 2 move and none when moving inside, 2 remove and none when removing, 2 add and none when adding, 2 add 2 remove and none when transferring
			Punpun.Information(this, "container change type is" + changeType.ToString());

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
			if (ContainsItem(item))
			{
				return true;
			}

			if (!CanContainItem(item))
			{
				return false;
			}

			// TODO: Use a more efficient algorithm
			for (int y = 0; y <= Size.y; y++)
			{
				for (int x = 0; x <= Size.x; x++)
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
		public bool AddItemPosition(Item item, Vector2Int position)
		{
			int itemIndex = FindItem(item);
			if (itemIndex != -1)
			{
				StoredItem existingItem = _storedItems[itemIndex];
				// Try to move existing item
				if (existingItem.Position == position)
				{
					return true;
				}

				StoredItem storedItem = new(item, position);
				ReplaceStoredItem(storedItem, itemIndex);
				return true;

				// Item at same position, nothing to do
			}

			if (!CanContainItem(item))
			{
				return false;
			}

			bool wasAdded = false;
			lock (_modificationLock)
			{
				AddItemUnchecked(item, position);
				wasAdded = true;
			}

			if (!wasAdded)
			{
				return false;
			}

			item.SetContainer(this);

			return true;
		}

		/// <summary>
		/// Adds an item to the container without any checks (but ensuring there are no duplicates)
		/// </summary>
		/// <param name="item">The item to add</param>
		/// <param name="position">Where the item should go, make sure this position is valid and free!</param>
		private void AddItemUnchecked(Item item, Vector2Int position)
		{
			StoredItem newItem = new(item, position);

			// Move it if it is already in the container
			if (MoveItemUnchecked(newItem))
			{
				return;
			}

			AddStoredItem(newItem);
			LastModification = Time.time;
		}

		/// <summary>
		/// Correctly add a storeItem to the container. All adding should use this method, never do it directly.
		/// </summary>
		/// <param name="newItem"> the item to store.</param>
		private void AddStoredItem(StoredItem newItem)
		{
			_storedItems.Add(newItem);
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
		private void RemoveStoredItem(int index)
		{
			_storedItems.RemoveAt(index);
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
		/// Removes an item from the container
		/// </summary>
		/// <param name="item">The item to remove</param>
		public void RemoveItem(Item item)
		{
			for (int i = 0; i < _storedItems.Count; i++)
			{
				if (_storedItems[i].Item != item)
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
			for (int i = 0; i < _storedItems.Count; i++)
			{
				StoredItem x = _storedItems[i];
				if (x.Item != item.Item)
				{
					continue;
				}

				if (x.Position == item.Position)
				{
					return true;
				}

				ReplaceStoredItem(item, i);
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

		private void RemoveItemAt(int index)
		{
			StoredItem storedItem = _storedItems[index];
			lock (_modificationLock)
			{
				RemoveStoredItem(index);
			}

			LastModification = Time.time;
			storedItem.Item.SetContainer(null);
		}

		/// <summary>
		/// Empties the container, removing all items
		/// </summary>
		public void Dump()
		{
			Item[] oldItems = _storedItems.Select(x => x.Item).ToArray();
			for (int i = 0; i < oldItems.Length; i++)
			{
				oldItems[i].SetContainer(null);
			}
			_storedItems.Clear();

			LastModification = Time.time;
		}

		/// <summary>
		/// Destroys all items in this container
		/// </summary>
		public void Purge()
		{
			for (int i = 0; i < _storedItems.Count; i++)
			{
				_storedItems[i].Item.Delete();
			}
			_storedItems.Clear();

			LastModification = Time.time;
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
		/// Checks if this item fits inside the container
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		private bool CanHoldItem(Item item)
		{
			return Items.Count() < Size.x * Size.y;
		}

		/// <summary>
		/// Checks if this item can be stored and fits inside the container
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public bool CanContainItem(Item item)
		{
			return CanStoreItem(item) && CanHoldItem(item);
		}
		
		/// <summary>
		/// Finds the index of an item
		/// </summary>
		/// <param name="item">The item to look for</param>
		/// <returns>The index of the item or -1 if not found</returns>
		public int FindItem(Item item)
		{
			for (int i = 0; i < _storedItems.Count; i++)
			{
				StoredItem storedItem = _storedItems[i];
				if (storedItem.Item == item)
				{
					return i;
				}
			}

			return -1;
		}

		public bool IsAreaFree(Vector2Int slotPosition)
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

		public bool AreSlotCoordinatesInGrid(Vector2Int slotCoordinates)
		{
			return slotCoordinates.x < Size.x && slotCoordinates.y < Size.y && slotCoordinates.x >= 0 && slotCoordinates.y >= 0;
		}
	}

}
