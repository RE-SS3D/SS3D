using System;
using System.Collections.Generic;
using SS3D.Core.Behaviours;
using SS3D.Storage.Containers;
using SS3D.Systems.Entities;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Storage.Containers
{
    [RequireComponent(typeof(Container))]
    /// <summary>
    /// A container attached to a gameobject
    /// </summary>
    public class AttachedContainer : NetworkActor
    {
        /// <summary>
        /// The creatures looking at this container
        /// </summary>
        public readonly HashSet<PlayerControllable> ObservingPlayers = new();

        public ContainerDescriptor ContainerDescriptor;

        [SerializeField] private Container _container;

        public delegate void ObserverHandler(AttachedContainer container, PlayerControllable observer);
        
        public event EventHandler<Item> OnItemAttached;
        public event EventHandler<Item> OnItemDetached;
        public event ObserverHandler OnNewObserver;
        
        /// <summary>
        /// The container that is attached
        /// <remarks>Only set this right after creation, as event listener will not update</remarks>
        /// </summary>
        public Container Container
        {
            get => _container;
            set => UpdateContainer(value);
        }

        protected override void OnStart()
        {
            base.OnStart();

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
        public bool AddObserver(PlayerControllable observer)
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
        public void RemoveObserver(PlayerControllable observer)
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

        private void ProcessNewObserver(PlayerControllable e)
        {
            OnNewObserver?.Invoke(this, e);
        }

        private void UpdateContainer(Container newContainer)
        {
            if (_container != null)
            {
                _container.OnContentsChanged -= HandleContainerContentsChanged;
                _container.AttachedTo = null;
            }

            if (newContainer == null)
            {
                return;
            }

            newContainer.Size = ContainerDescriptor.Size;
            newContainer.OnContentsChanged += HandleContainerContentsChanged;
            newContainer.AttachedTo = this;
            _container = newContainer;
        }

        private void HandleContainerContentsChanged(Container container, IEnumerable<Item> oldItems,IEnumerable<Item> newItems, ContainerChangeType type)
        {
            void handleItemAdded(Item item)
            {
                item.Freeze();
                        
                // Make invisible
                if (ContainerDescriptor.HideItems)                                                                  
                {
                    item.SetVisibility(false);
                }

                // Attach to container
                if (ContainerDescriptor.AttachItems)
                {
                    Transform itemTransform = item.transform;
                    itemTransform.SetParent(transform, false);
                    itemTransform.localPosition = ContainerDescriptor.AttachmentOffset;
                    ProcessItemAttached(item);
                }
            }

            void handleItemRemoved(Item item)
            {
                // Only unfreeze the item if it was not just placed into another container
                if(item.Container == null)
                    item.Unfreeze();
                // Restore visibility
                if (ContainerDescriptor.HideItems)
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