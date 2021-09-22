using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using SS3D.Content.Systems.Interactions;
using SS3D.Engine.Interactions;
using UnityEngine;
using SS3D.Engine.Inventory;

namespace SS3D.Content.Furniture.Storage
{
    public class ContainerInteractive : NetworkedOpenable
    {

        [HideInInspector] public ContainerDescriptor containerDescriptor;
        private Sprite viewContainerIcon;

        public override IInteraction[] GenerateInteractionsFromTarget(InteractionEvent interactionEvent)
        {
            if (containerDescriptor.hasCustomInteraction)
            {
                return new IInteraction[0];
            }

            List<IInteraction> interactions = new List<IInteraction>();

            StoreInteraction storeInteraction = new StoreInteraction(containerDescriptor);
            storeInteraction.icon = containerDescriptor.storeIcon;
            TakeInteraction takeInteraction = new TakeInteraction(containerDescriptor);
            takeInteraction.icon = containerDescriptor.takeIcon;
            ViewContainerInteraction view = new ViewContainerInteraction(containerDescriptor){MaxDistance = containerDescriptor.maxDistance, icon = viewContainerIcon};
            view.icon = containerDescriptor.viewIcon;

            // Pile or Normal the Store Interaction will always appear, but View only appears in Normal containers
            if (IsOpen() | !containerDescriptor.onlyStoreWhenOpen | !containerDescriptor.isOpenable)
            {
                if (containerDescriptor.hasUi)
                {
                    interactions.Add(storeInteraction);
                    interactions.Add(view);
                }
                else
                {
                    interactions.Add(storeInteraction);
                    interactions.Add(takeInteraction);
                }           
            }

            if (containerDescriptor.isOpenable)
            {
                OpenInteraction openInteraction = new OpenInteraction(containerDescriptor);
                openInteraction.icon = containerDescriptor.openIcon;
                openInteraction.OpenStateChange += OnOpenStateChange;
                interactions.Add(openInteraction);
            }

            return interactions.ToArray();
        }

        
        protected override void OnOpenStateChange(object sender, bool e)
        {  
            base.OnOpenStateChange(sender, e);
        }

        /// <summary>
        /// recursively closes all container UI when the root container is closed.
        /// This is potentially very slow when there's a lot of containers and items as it calls get component for every items in every container.
        /// A faster solution could be to use unity game tag and to tag every object with a container as such.
        /// Keeping track in Container of the list of objects that are containers would make it really fast.
        /// </summary>
        private void closeUis()
        {
            if (containerDescriptor.containerUi != null)
            {
                containerDescriptor.containerUi.Close();
            }
            
            // We check for each item if they are interactive containers
            foreach(Item item in containerDescriptor.attachedContainer.Container.Items)
            {
                ContainerInteractive[] containerInteractives = item.GameObject.GetComponents<ContainerInteractive>();
                // if the item is an interactive container, we call this method again on it.
                if (containerInteractives != null)
                {
                    foreach(ContainerInteractive c in containerInteractives)
                    {
                        c.closeUis();
                    }                   
                }
            }
        }

        protected override void OpenHook(bool oldVal, bool newVal)
        {
            base.OpenHook(oldVal, newVal);
            if (!newVal)
            {
                closeUis();
            }
        }
    }
}