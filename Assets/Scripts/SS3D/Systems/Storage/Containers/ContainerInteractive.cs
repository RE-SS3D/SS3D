using System;
using System.Collections.Generic;
using SS3D.Interactions;
using SS3D.Interactions.Interfaces;
using SS3D.Systems.Storage.Interactions;
using SS3D.Systems.Storage.Items;
using UnityEngine;

namespace SS3D.Systems.Storage.Containers
{
    public class ContainerInteractive : NetworkedOpenable
    {

        [HideInInspector] public ContainerDescriptor containerDescriptor;
        private Sprite _viewContainerIcon;

        public override IInteraction[] CreateTargetInteractions(InteractionEvent interactionEvent)
        {
            if (containerDescriptor.HasCustomInteraction)
            {
                return Array.Empty<IInteraction>();
            }

            List<IInteraction> interactions = new();

            StoreInteraction storeInteraction = new(containerDescriptor)
            {
                Icon = containerDescriptor.StoreIcon
            };
            TakeFirstInteraction takeFirstInteraction = new(containerDescriptor)
            {
                Icon = containerDescriptor.TakeIcon
            };
            ViewContainerInteraction view = new(containerDescriptor)
            {
                MaxDistance = containerDescriptor.MaxDistance, Icon = _viewContainerIcon
            };

            view.Icon = containerDescriptor.ViewIcon;

            // Pile or Normal the Store Interaction will always appear, but View only appears in Normal containers
            if (IsOpen() | !containerDescriptor.OnlyStoreWhenOpen | !containerDescriptor.IsOpenable)
            {
                if (containerDescriptor.HasUi)
                {
                    interactions.Add(storeInteraction);
                    interactions.Add(view);
                }
                else
                {
                    interactions.Add(storeInteraction);
                    interactions.Add(takeFirstInteraction);
                }           
            }

            if (!containerDescriptor.IsOpenable)
            {
                return interactions.ToArray();
            }

            OpenInteraction openInteraction = new(containerDescriptor)
            {
                Icon = containerDescriptor.OpenIcon
            };
            openInteraction.OnOpenStateChanged += OpenStateChanged;
            interactions.Add(openInteraction);

            return interactions.ToArray();
        }

        
        protected override void OpenStateChanged(object sender, bool e)
        {  
            base.OpenStateChanged(sender, e);
        }

        /// <summary>
        /// recursively closes all container UI when the root container is closed.
        /// This is potentially very slow when there's a lot of containers and items as it calls get component for every items in every container.
        /// A faster solution could be to use unity game tag and to tag every object with a container as such.
        /// Keeping track in Container of the list of objects that are containers would make it really fast.
        /// </summary>
        private void CloseUis()
        {
            if (containerDescriptor.ContainerUi != null)
            {
                containerDescriptor.ContainerUi.Close();
            }
            
            // We check for each item if they are interactive containers
            foreach(Item item in containerDescriptor.AttachedContainer.Container.Items)
            {
                ContainerInteractive[] containerInteractives = item.GameObject.GetComponents<ContainerInteractive>();
                // if the item is an interactive container, we call this method again on it.
                if (containerInteractives == null)
                {
                    continue;
                }

                foreach(ContainerInteractive c in containerInteractives)
                {
                    c.CloseUis();
                }
            }
        }

        protected override void SyncOpenState(bool oldVal, bool newVal, bool asServer)
        {
            base.SyncOpenState(oldVal, newVal, asServer);
            if (!newVal)
            {
                CloseUis();
            }
        }
    }
}