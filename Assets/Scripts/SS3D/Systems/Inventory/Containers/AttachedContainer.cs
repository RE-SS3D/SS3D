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
using static SS3D.Substances.SubstanceContainer;
using static SS3D.Systems.Inventory.Containers.Container;

namespace SS3D.Systems.Inventory.Containers
{
    /// <summary>
    /// AttachedContainer manages every aspect of a container attached to a gameObject.
    /// It's purpose is to centralize all relevant aspect of a container, it should be the only component one has to deal with when
    /// adding containers to a game object.
    ///
    /// Warning: Many attributes should be private instead of public. They are currently public because AttachedContainerEditor
    /// needs to access them directly, not through accessors or properties.
    ///
    /// AttachedContainerEditor should be declared as friend of AttachedContainer and most attributes should be private.
    /// </summary>
    public class AttachedContainer : NetworkActor
    {
        public bool AutomaticContainerSetUp = false;
        // References toward all container related scripts.

        public delegate void ContainerContentsHandler(AttachedContainer container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type);
        /// <summary>
        /// Called when the contents of the container change
        /// </summary>
        public event ContainerContentsHandler OnContentsChanged;


        public ContainerInteractive ContainerInteractive;
        public ContainerItemDisplay ContainerItemDisplay;

        // reference towards the container UI linked to this container.
        [Tooltip("Reference towards the container UI linked to this container. Leave empty before run ! ")]
        public ContainerUi ContainerUi;

        [Tooltip("Open interaction icon, visible when opening a container.")]
        public Sprite OpenIcon;
        [Tooltip("Take interaction icon, visible when taking something from a container.")]
        public Sprite TakeIcon;
        [Tooltip("Store interaction icon, visible when storing something in a container.")]
        public Sprite StoreIcon;
        [Tooltip("View interaction icon, visible when viewing a container.")]
        public Sprite ViewIcon;

        [Tooltip("The local position of attached items.")]
        public Vector3 AttachmentOffset = Vector3.zero;
        [Tooltip("Name of the container.")]
        public string ContainerName = "container";
        [Tooltip("If the container is openable, this defines if things can be stored in the container without opening it.")]
        public bool OnlyStoreWhenOpen;
        [Tooltip("When the container UI is opened, if set true, the animation on the object is triggered.")]
        public bool OpenWhenContainerViewed;
        [Tooltip("Defines the size of the container, every item takes a defined place inside a container.")]
        public Vector2Int Size = new(0, 0);

        /// <summary>
        /// Set visibility of objects inside the container (not in the UI, in the actual game object).
        /// If the container is Hidden, the visibility of items is always off.
        /// </summary>
        [Tooltip("Set visibility of items in container.")]
        public bool HideItems = true;
        [Tooltip("If items should be attached as children of the container's game object.")]
        public bool AttachItems = true;

        // Initialized should not be displayed, it's only useful for setting up the container in editor.
        [HideInInspector]
        public bool Initialized;
        [Tooltip("Max distance at which the container is visible if not hidden.")]
        public float MaxDistance = 5f;
        [Tooltip("If the container can be opened/closed, in the sense of having a close/open animation.")]
        public bool IsOpenable;
        [Tooltip("If the container should have the container's default interactions setting script.")]
        public bool IsInteractive;
        [Tooltip("If stuff inside the container can be seen using an UI.")]
        public bool HasUi;
        [Tooltip("If true, interactions in containerInteractive are ignored, instead, a script on the container's game object should implement IInteractionTarget.")]
        public bool HasCustomInteraction;
        [Tooltip("If the container renders items in custom position on the container.")]
        public bool HasCustomDisplay;
        [Tooltip(" The list of transforms defining where the items are displayed.")]
        public Transform[] Displays;
        [Tooltip(" The number of custom displays.")]
        public int NumberDisplay;
        [Tooltip("The filter on the container.")]
        public Filter StartFilter;
        [FormerlySerializedAs("ContainerType")]
        [Tooltip("Container type mostly allow to discriminate between diffent containers on a single prefab.")]
        public ContainerType Type;

        /// <summary>
        /// The creatures looking at this container
        /// </summary>
        public readonly HashSet<Entity> ObservingPlayers = new();

        [SerializeField] [NotNull] private Container _container;

        public delegate void ObserverHandler(AttachedContainer container, Entity observer);

        public event EventHandler<Item> OnItemAttached;
        public event EventHandler<Item> OnItemDetached;
        public event ObserverHandler OnNewObserver;

        /// <summary>
        /// The items stored in this container, including information on how they are stored
        /// </summary>
        [SyncObject]
        private readonly SyncList<StoredItem> _storedItems = new();

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
            // If container interactions icon are not defined at start, load default icons.
            OpenIcon = Assets.Get(InteractionIcons.Open);
            TakeIcon = Assets.Get(InteractionIcons.Take);
            StoreIcon = Assets.Get(InteractionIcons.Take);
            ViewIcon = Assets.Get(InteractionIcons.Open);

            _container = new Container(this);
            _storedItems.OnChange += HandleStoredItemsChanged;
            UpdateContainer(_container);
        }
        /// <summary>
        /// Return the name of the Object as defined by the Examine System. 
        /// <remarks> If multiple classes implement the Interface IExaminable
        /// and have an ExamineType equal to SIMPLE_TEXT, it returns the first name encountered. </remarks>
        /// </summary>
        public string GetName()
        {
            // TODO: Add back examine system

            return string.Empty;
        }

        public void OnDestroy()
        {
            Container?.Purge();
        }

        /// <summary>
        /// Adds an observer to this container
        /// </summary>
        /// <param name="observer">The creature which observes</param>
        /// <returns>True if the creature was not already observing this container</returns>
        public bool AddObserver(Entity observer)
        {
            bool newObserver = ObservingPlayers.Add(observer);
            if (newObserver)
            {
                ProcessNewObserver(observer);
            }
            return newObserver;
        }

        /// <summary>
        /// Removes an observer
        /// </summary>
        /// <param name="observer">The observer to remove</param>
        public void RemoveObserver(Entity observer)
        {
            ObservingPlayers.Remove(observer);
        }

        public override string ToString()
        {
            return $"{name}({nameof(AttachedContainer)})[size: {_container.Size}, items: {_container.ItemCount}]";
        }

        private void ProcessItemAttached(Item e)
        {
            OnItemAttached?.Invoke(this, e);
        }

        private void ProcessItemDetached(Item e)
        {
            OnItemDetached?.Invoke(this, e);
        }

        private void ProcessNewObserver(Entity e)
        {
            OnNewObserver?.Invoke(this, e);
        }

        /// <summary>
        /// Replace the current container with a new one and set it up.
        /// </summary>
        /// <param name="newContainer"></param>
        private void UpdateContainer(Container newContainer)
        {
            if (_container != null)
            {
                OnContentsChanged -= HandleContainerContentsChanged;
                _container.AttachedTo = null;
            }

            if (newContainer == null)
            {
                return;
            }

            newContainer.Size = Size;
            OnContentsChanged += HandleContainerContentsChanged;
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

            OnContentsChanged?.Invoke(this, new[] { oldItem.Item }, new[] { newItem.Item }, changeType);
        }

        private void handleItemRemoved(Item item)
        {
            // Only unfreeze the item if it was not just placed into another container
            if (item.Container == null)
            {
                item.Unfreeze();
            }

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
        }

        private void handleItemAdded(Item item)
        {
            item.Freeze();

            // Make invisible
            if (HideItems)
            {
                item.SetVisibility(false);
            }

            // Attach to container
            if (AttachItems)
            {
                Transform itemTransform = item.transform;
                itemTransform.SetParent(transform, false);
                itemTransform.localPosition = AttachmentOffset;
                ProcessItemAttached(item);
            }
        }

        private void HandleContainerContentsChanged(AttachedContainer container, IEnumerable<Item> oldItems, IEnumerable<Item> newItems, ContainerChangeType type)
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
