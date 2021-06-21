using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content;
using UnityEngine;
using SS3D.Engine.Examine;


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
        /// The local position of attached items
        /// </summary>
        public Vector3 AttachmentOffset = Vector3.zero;
        /// <summary>
        /// The creatures looking at this container
        /// </summary>
        public HashSet<Entity> Observers = new HashSet<Entity>();

        private Container container;

        public delegate void ObserverHandler(AttachedContainer container, Entity observer);
        
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

        /// <summary>
        /// Return the name of the Object as defined by the Examine System. 
        /// <remarks> If multiple classes implement the Interface IExaminable
        /// and have an ExamineType equal to SIMPLE_TEXT, it returns the first name encountered. </remarks>
        /// </summary>
        public string GetName()
        {
            String Name;

            //Gets all components on this object implementing the interface IExaminable
            var iExaminables = GetComponents<IExaminable>();
            //Go through each components until one has a ExamineType equal to SIMPLE_TEXT and is not an empty string, then returns the name linked to it.
            foreach (IExaminable iExaminable in iExaminables)
            {
               if (iExaminable.GetData().GetExamineType() == ExamineType.SIMPLE_TEXT)
                {
                    DataNameDescription DataName = (DataNameDescription)(iExaminable.GetData());
                    Name = DataName.GetName();
                    if (Name != "")
                    {
                        return Name;
                    }
                }
            }
            return null;
        }

        public static AttachedContainer CreateEmpty(GameObject gameObject, Vector2Int size, IEnumerable<Filter> filters = null)
        {
            var attachedContainer = gameObject.AddComponent<AttachedContainer>();
            var container = new Container
            {
                Size = size
            };
            if (filters != null)
            {
                container.Filters.AddRange(filters);
            }
            attachedContainer.Container = container;
            
            return attachedContainer;
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
        public bool AddObserver(Entity observer)
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
        public void RemoveObserver(Entity observer)
        {
            Observers.Remove(observer);
        }

        public override string ToString()
        {
            return $"{name}({nameof(AttachedContainer)})[size: {container.Size}, items: {container.ItemCount}]";
        }

        public string NameToString()
        {
            return $"{name}";
        }

        /// <summary>
        /// Verify if this AttachedContainer is attached to a right or to a left hand
        /// </summary>
        public bool IsAttachedToHands()
        {
            return NameToString() == "hand_r" || NameToString() == "hand_l";
        }
        protected virtual void OnItemAttached(Item e)
        {
            ItemAttached?.Invoke(this, e);
        }

        protected virtual void OnItemDetached(Item e)
        {
            ItemDetached?.Invoke(this, e);
        }
        
        protected virtual void OnNewObserver(Entity e)
        {
            NewObserver?.Invoke(this, e);
        }

        private void UpdateContainer(Container newContainer)
        {
            if (container != null)
            {
                container.ContentsChanged -= ContainerContentsChanged;
                container.AttachedTo = null;
            }

            if (newContainer == null)
            {
                return;
            }
            
            newContainer.ContentsChanged += ContainerContentsChanged;
            newContainer.AttachedTo = this;
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
                            itemTransform.localPosition = AttachmentOffset;
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
                            item.transform.SetParent(null, true);
                        }
                        OnItemDetached(item);
                    }

                    break;
                }
            }
        }
    }
}