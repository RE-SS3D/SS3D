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

        [Tooltip("Name of the container."), SerializeField]
        private string _containerName = "container";

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

        public string ContainerName => _containerName;
        public ContainerType Type => _type;
        public Vector2Int Size => _size;
        public bool HideItems => _hideItems;
        public Filter StartFilter => _startFilter;

        #endregion

        private Container _container;

        public event EventHandler<Item> OnItemAttached;
        public event EventHandler<Item> OnItemDetached;

        /// <summary>
        /// The items stored in this container, including information on how they are stored
        /// </summary>
        [SyncObject]
        private readonly SyncList<StoredItem> _storedItems = new();

        /// <summary>
        /// The items stored in this container
        /// </summary>
        public IEnumerable<Item> Items => StoredItems.Select(x => x.Item);

        public SyncList<StoredItem> StoredItems => _storedItems;

        /// <summary>
        /// The container that is attached
        /// <remarks>Only set this right after creation, as event listener will not update</remarks>
        /// </summary>
        public Container Container
        {
            get => _container;
            set => UpdateContainer(value);
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            _container = new Container(this);
            _storedItems.OnChange += HandleStoredItemsChanged;
            UpdateContainer(_container);
            _container.OnContentsChanged += HandleContainerContentsChanged;
        }

        protected override void OnDestroyed()
        {
            base.OnDestroyed();
            Container?.Purge();
        }

        public override string ToString()
        {
            return $"{name}({nameof(AttachedContainer)})[size: {_container.Size}, items: {_container.ItemCount}]";
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
        /// Replace the current container with a new one and set it up.
        /// </summary>
        /// <param name="newContainer"></param>
        [Server]
        private void UpdateContainer(Container newContainer)
        {
            if (_container != null)
            {
                _container.AttachedTo = null;
            }

            if (newContainer == null)
            {
                return;
            }

            newContainer.AttachedTo = this;
            _container = newContainer;
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
            ContainerChangeType changeType;

            switch (op)
            {
                case SyncListOperation.Add:
                    changeType = ContainerChangeType.Add;
                    break;
                case SyncListOperation.Insert:
                case SyncListOperation.Set:
                    changeType = ContainerChangeType.Move;
                    break;
                case SyncListOperation.RemoveAt:
                case SyncListOperation.Clear:
                    changeType = ContainerChangeType.Remove;
                    break;
                case SyncListOperation.Complete:
                    changeType = ContainerChangeType.Move;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(op), op, null);
            }

            _container.InvokeOnContentChanged(new[] { oldItem.Item }, new[] { newItem.Item }, changeType);
        }

        [Server]
        private void handleItemRemoved(Item item)
        {

            // Restore visibility
            if (HideItems)
            {
                item.SetVisibility(true);
            }

            // Remove parent if child of this
            if (item.transform.parent == transform)
            {
                item.transform.SetParent(null, true);
                ProcessItemDetached(item);
            }

            item.Unfreeze();
        }

        [Server]
        private void handleItemAdded(Item item)
        {
            item.Freeze();

            // Make invisible
            if (HideItems)
            {
                item.SetVisibility(false);
            }

            if (AttachItems)
            {
                Transform itemTransform = item.transform;
                itemTransform.SetParent(transform, false);
                itemTransform.localPosition = AttachmentOffset;
                ProcessItemAttached(item);
            }
        }

        [Server]
        private void HandleContainerContentsChanged(Container container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type)
        {
            switch (type)
            {
                case ContainerChangeType.Add:
                    foreach (Item item in newItems)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        handleItemAdded(item);
                    }

                    break;
                case ContainerChangeType.Move:
                    {
                        foreach (Item item in newItems)
                        {
                            if (item == null)
                            {
                                continue;
                            }

                            handleItemRemoved(item);
                            handleItemAdded(item);
                        }

                        break;
                    }
                case ContainerChangeType.Remove:
                    {
                        foreach (Item item in oldItems)
                        {
                            if (item == null)
                            {
                                continue;
                            }

                            handleItemRemoved(item);
                        }

                        break;
                    }
            }
        }
    }


}
