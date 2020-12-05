using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

namespace SS3D.Engine.Inventory
{
    /// <summary>
    /// A container attached to a gameobject
    /// </summary>
    public class AttachedContainer : MonoBehaviour
    {
        /// <summary>
        /// If items should be hidden
        /// </summary>
        public bool HideItems = true;
        /// <summary>
        /// If items should be attached as children
        /// </summary>
        public bool AttachItems = true;
        /// <summary>
        /// The creatures looking at this container
        /// </summary>
        public HashSet<Creature> Observers = new HashSet<Creature>();

        private Container container;

        public delegate void ObserverHandler(AttachedContainer container, Creature observer);
        
        public event EventHandler<Item> ItemAttached;
        public event EventHandler<Item> ItemDetached;
        public event ObserverHandler NewObserver; 
        
        /// <summary>
        /// The container that is attached
        /// <remarks>Only set this right after creation, as event listener will not update</remarks>
        /// </summary>
        public Container Container
        {
            get => container;
            set => UpdateContainer(value);
        }

        public void OnDestroy()
        {
            Container?.Destroy();
        }

        /// <summary>
        /// Adds an observer to this container
        /// </summary>
        /// <param name="observer">The creature which observes</param>
        /// <returns>True if the creature was not already observing this container</returns>
        public bool AddObserver(Creature observer)
        {
            bool newObserver = Observers.Add(observer);
            if (newObserver)
            {
                OnNewObserver(observer);
            }
            return newObserver;
        }

        /// <summary>
        /// Removes an observer
        /// </summary>
        /// <param name="observer">The observer to remove</param>
        public void RemoveObserver(Creature observer)
        {
            Observers.Remove(observer);
        }

        public override string ToString()
        {
            return $"{name}({nameof(AttachedContainer)})[size: {container.Size}, items: {container.ItemCount}]";
        }

        protected virtual void OnItemAttached(Item e)
        {
            ItemAttached?.Invoke(this, e);
        }

        protected virtual void OnItemDetached(Item e)
        {
            ItemDetached?.Invoke(this, e);
        }
        
        protected virtual void OnNewObserver(Creature e)
        {
            NewObserver?.Invoke(this, e);
        }

        private void UpdateContainer(Container newContainer)
        {
            if (container != null)
            {
                container.ContentsChanged -= ContainerContentsChanged;
            }

            if (newContainer == null)
            {
                return;
            }
            
            newContainer.ContentsChanged += ContainerContentsChanged;
            container = newContainer;
        }

        private void ContainerContentsChanged(Container container, IEnumerable<Item> items, Container.ContainerChangeType type)
        {
            switch (type)
            {
                case Container.ContainerChangeType.Add:
                {
                    foreach (Item item in items)
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
                            itemTransform.localPosition = Vector3.zero;
                            OnItemAttached(item);
                        }
                    }

                    break;
                }
                case Container.ContainerChangeType.Remove:
                {
                    foreach (Item item in items)
                    {
                        item.Unfreeze();
                        // Restore visibility
                        if (HideItems)
                        {
                            item.SetVisibility(true);
                        }
                        // Remove parent if child of this
                        if (item.transform.parent == transform)
                        {
                            item.transform.SetParent(null);
                        }
                        OnItemDetached(item);
                    }

                    break;
                }
            }
        }
    }
}